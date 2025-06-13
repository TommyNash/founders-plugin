using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Менеджер аудио системы. Управляет всеми источниками звука, их состоянием и глобальными настройками.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        private static bool isQuitting = false;
        
        public static AudioManager Instance
        {
            get
            {
                if (isQuitting) return null;
                
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // События
        public event Action<string> OnMusicStarted;
        public event Action<string> OnMusicStopped;
        public event Action<string> OnEffectPlayed;
        public event Action<float> OnVolumeChanged;
        public event Action<string> OnSoundLoaded;
        public event Action<string> OnSoundFailed;
        public event Action<string> OnSoundComplete;

        // Настройки
        private const float MIN_VOLUME = 0f;
        private const float MAX_VOLUME = 1f;
        private const int MAX_CONCURRENT_MUSIC = 3;
        private const int MAX_CONCURRENT_EFFECTS = 5;

        // Коллекции для управления звуками
        private Dictionary<string, SourceAudio> musicSources = new Dictionary<string, SourceAudio>();
        private Queue<SourceAudio> effectSourcesPool = new Queue<SourceAudio>();
        private HashSet<SourceAudio> activeEffectSources = new HashSet<SourceAudio>();
        private readonly List<SourceAudio> activeSources = new List<SourceAudio>();
        
        // Состояние
        private float globalVolume = 1f;
        private bool isMuted = false;
        private AudioDatabase database;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadDatabase();
            PreloadSounds();
            InitializeWebGL();
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null)
            {
                Debug.LogWarning("AudioDatabase не найден в Resources. Создайте его через меню Assets/Create/FoundersPlugin/Audio/Database");
            }
        }

        private void PreloadSounds()
        {
            if (database == null) return;

            foreach (var clipData in database.GetPreloadClips())
            {
                try
                {
                    AudioModule.Instance.LoadAudioClip(clipData.Key);
                    Debug.Log($"Предзагружен звук: {clipData.Key}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка при предзагрузке звука {clipData.Key}: {ex.Message}");
                }
            }
        }

        private void InitializeWebGL()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                WebGLAudioBridge.InitializeAudioSystem(globalVolume);
                WebGLAudioBridge.OnSoundLoaded += HandleSoundLoaded;
                WebGLAudioBridge.OnSoundError += HandleSoundError;
                WebGLAudioBridge.OnSoundComplete += HandleSoundComplete;
                WebGLAudioBridge.OnMuteChanged += HandleMuteChanged;
                Debug.Log("WebGL аудио мост инициализирован");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при инициализации WebGL аудио моста: {ex.Message}");
            }
            #endif
        }

        #region Управление источниками звука

        public void RegisterSource(SourceAudio source)
        {
            if (!activeSources.Contains(source))
            {
                activeSources.Add(source);
            }
        }

        public void UnregisterSource(SourceAudio source)
        {
            activeSources.Remove(source);
        }

        public bool IsAudioPlaying(string key)
        {
            return activeSources.Any(s => s.IsPlayingKey(key));
        }

        public SourceAudio GetSourcePlayingKey(string key)
        {
            return activeSources.FirstOrDefault(s => s.IsPlayingKey(key));
        }

        #endregion

        #region Управление музыкой

        public async Task PlayMusic(string key, float volume = 1f, bool loop = true)
        {
            if (!musicSources.ContainsKey(key))
            {
                if (musicSources.Count >= MAX_CONCURRENT_MUSIC)
                {
                    var oldestMusic = musicSources.First();
                    StopMusic(oldestMusic.Key);
                }

                var source = CreateMusicSource();
                musicSources[key] = source;
                await source.PlayWithParameters(key, new PlaySoundParameters 
                { 
                    volume = volume * globalVolume,
                    loop = loop,
                    dontDestroyOnLoad = true 
                });
                OnMusicStarted?.Invoke(key);
            }
        }

        public void StopMusic(string key)
        {
            if (musicSources.TryGetValue(key, out var source))
            {
                source.Stop();
                Destroy(source.gameObject);
                musicSources.Remove(key);
                OnMusicStopped?.Invoke(key);
            }
        }

        private SourceAudio CreateMusicSource()
        {
            var go = new GameObject("MusicSource");
            go.transform.parent = transform;
            return go.AddComponent<SourceAudio>();
        }

        #endregion

        #region Управление звуковыми эффектами

        public async Task PlayEffect(string key, float volume = 1f)
        {
            if (activeEffectSources.Count >= MAX_CONCURRENT_EFFECTS)
            {
                return;
            }

            SourceAudio source = GetEffectSource();
            await source.PlayWithParameters(key, new PlaySoundParameters 
            { 
                volume = volume * globalVolume,
                loop = false 
            });
            activeEffectSources.Add(source);
            OnEffectPlayed?.Invoke(key);
        }

        private SourceAudio GetEffectSource()
        {
            if (effectSourcesPool.Count > 0)
            {
                return effectSourcesPool.Dequeue();
            }

            var go = new GameObject("EffectSource");
            go.transform.parent = transform;
            return go.AddComponent<SourceAudio>();
        }

        public void ReturnEffectSourceToPool(SourceAudio source)
        {
            if (activeEffectSources.Remove(source))
            {
                source.Stop();
                effectSourcesPool.Enqueue(source);
            }
        }

        #endregion

        #region Управление громкостью

        public void SetVolume(float value)
        {
            globalVolume = Mathf.Clamp01(value);
            
            // Обновляем громкость для всех активных источников
            foreach (var source in activeSources.ToArray())
            {
                if (source != null)
                {
                    float currentVolume = source.Volume;
                    source.Volume = currentVolume;
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.SetGlobalVolume(globalVolume);
            #endif

            OnVolumeChanged?.Invoke(globalVolume);
        }

        public float GetVolume()
        {
            return globalVolume;
        }

        public void Mute()
        {
            if (!isMuted)
            {
                isMuted = true;
                SetVolume(0f);
            }
        }

        public void Unmute()
        {
            if (isMuted)
            {
                isMuted = false;
                SetVolume(1f);
            }
        }

        /// <summary>
        /// Ставит на паузу все активные звуки
        /// </summary>
        public void PauseAll()
        {
            // Ставим на паузу все музыкальные источники
            foreach (var source in musicSources.Values)
            {
                if (source != null)
                {
                    source.Pause();
                }
            }

            // Ставим на паузу все источники эффектов
            foreach (var source in activeEffectSources)
            {
                if (source != null)
                {
                    source.Pause();
                }
            }

            // Ставим на паузу все остальные активные источники
            foreach (var source in activeSources)
            {
                if (source != null && !musicSources.ContainsValue(source) && !activeEffectSources.Contains(source))
                {
                    source.Pause();
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.PauseAll();
            #endif
        }

        /// <summary>
        /// Снимает с паузы все звуки
        /// </summary>
        public void UnpauseAll()
        {
            // Снимаем с паузы все музыкальные источники
            foreach (var source in musicSources.Values)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }

            // Снимаем с паузы все источники эффектов
            foreach (var source in activeEffectSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }

            // Снимаем с паузы все остальные активные источники
            foreach (var source in activeSources)
            {
                if (source != null && !musicSources.ContainsValue(source) && !activeEffectSources.Contains(source))
                {
                    source.UnPause();
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.UnpauseAll();
            #endif
        }

        #endregion

        #region WebGL колбэки

        private void HandleSoundLoaded(string key)
        {
            OnSoundLoaded?.Invoke(key);
        }

        private void HandleSoundError(string key, string error)
        {
            OnSoundFailed?.Invoke(key);
            Debug.LogError($"Ошибка звука {key}: {error}");
        }

        private void HandleSoundComplete(string key)
        {
            OnSoundComplete?.Invoke(key);
        }

        private void HandleMuteChanged(bool muted)
        {
            if (muted)
                Mute();
            else
                Unmute();
        }

        #endregion

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                StopAllSounds();
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebGLAudioBridge.OnSoundLoaded -= HandleSoundLoaded;
                WebGLAudioBridge.OnSoundError -= HandleSoundError;
                WebGLAudioBridge.OnSoundComplete -= HandleSoundComplete;
                WebGLAudioBridge.OnMuteChanged -= HandleMuteChanged;
                #endif
                instance = null;
            }
        }

        private void StopAllSounds()
        {
            foreach (var source in musicSources.Values)
            {
                if (source != null)
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }
            musicSources.Clear();

            foreach (var source in activeEffectSources)
            {
                if (source != null)
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }
            activeEffectSources.Clear();
            effectSourcesPool.Clear();
        }
    }
} 

