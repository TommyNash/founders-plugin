using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Менеджер аудио системы. Управляет всеми источниками звука и их состоянием.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
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

        // Настройки
        private const float MIN_VOLUME = 0f;
        private const float MAX_VOLUME = 1f;
        private const int MAX_CONCURRENT_MUSIC = 3;
        private const int MAX_CONCURRENT_EFFECTS = 5;

        private Dictionary<string, SourceAudio> musicSources = new Dictionary<string, SourceAudio>();
        private Queue<SourceAudio> effectSourcesPool = new Queue<SourceAudio>();
        private HashSet<SourceAudio> activeEffectSources = new HashSet<SourceAudio>();
        
        private float globalVolume = 1f;
        private bool isMuted = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEffectPool();
                
                #if UNITY_ANDROID
                InitializeAndroidAudio();
                #endif
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeEffectPool()
        {
            for (int i = 0; i < MAX_CONCURRENT_EFFECTS; i++)
            {
                var effectObject = new GameObject($"EffectSource_{i}");
                effectObject.transform.SetParent(transform);
                var source = effectObject.AddComponent<SourceAudio>();
                effectSourcesPool.Enqueue(source);
            }
        }

        #if UNITY_ANDROID
        private void InitializeAndroidAudio()
        {
            // Создаем тихий AudioSource для инициализации аудиосистемы
            var initSource = gameObject.AddComponent<AudioSource>();
            initSource.volume = 0f;
            initSource.clip = AudioClip.Create("Init", 1, 1, 44100, false);
            initSource.Play();
            Destroy(initSource, 0.1f);
            
            // Добавляем обработчик первого касания
            StartCoroutine(WaitForFirstTouch());
        }

        private IEnumerator WaitForFirstTouch()
        {
            while (!Input.GetMouseButtonDown(0) && !Input.touchCount > 0)
            {
                yield return null;
            }
            
            // Переинициализируем все источники звука
            foreach (var source in musicSources.Values)
            {
                if (source != null)
                {
                    var unitySource = source.GetComponent<AudioSource>();
                    if (unitySource != null)
                    {
                        unitySource.Stop();
                        unitySource.Play();
                    }
                }
            }
        }
        #endif

        #region Музыка

        public async void PlayMusic(string key, bool loop = true, bool dontDestroyOnLoad = true)
        {
            try
            {
                // Проверяем лимит одновременно играющей музыки
                if (musicSources.Count >= MAX_CONCURRENT_MUSIC)
                {
                    // Останавливаем самую старую музыку
                    var oldestMusic = musicSources.First();
                    StopMusic(oldestMusic.Key);
                }

                // Если этот ключ уже играет, ничего не делаем
                if (IsMusicPlaying(key)) return;

                // Создаем новый источник для этого ключа
                var musicObject = new GameObject($"Music_{key}");
                musicObject.transform.SetParent(transform);
                var source = musicObject.AddComponent<SourceAudio>();
                
                // Настраиваем источник
                source.Volume = globalVolume;
                source.Mute = isMuted;

                // Подписываемся на события
                source.OnFinished += () => HandleMusicFinished(key);
                source.OnError += (k, error) => Debug.LogError($"Ошибка воспроизведения музыки {k}: {error}");
                
                if (loop)
                {
                    await source.PlayLooped(key, dontDestroyOnLoad);
                }
                else
                {
                    await source.Play(key);
                }

                // Сохраняем источник
                musicSources[key] = source;
                OnMusicStarted?.Invoke(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при воспроизведении музыки {key}: {ex}");
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

        public void StopAllMusic()
        {
            var keys = musicSources.Keys.ToList();
            foreach (var key in keys)
            {
                StopMusic(key);
            }
        }

        public void PauseAllMusic()
        {
            foreach (var source in musicSources.Values)
            {
                if (source != null) source.Pause();
            }
        }

        public void UnpauseAllMusic()
        {
            foreach (var source in musicSources.Values)
            {
                if (source != null) source.UnPause();
            }
        }

        private void HandleMusicFinished(string key)
        {
            if (musicSources.ContainsKey(key))
            {
                StopMusic(key);
            }
        }

        #endregion

        #region Эффекты

        public async void PlayEffect(string key)
        {
            try
            {
                // Очищаем неактивные источники
                CleanupEffectSources();

                // Получаем свободный источник из пула
                var source = GetEffectSource();
                if (source == null)
                {
                    Debug.LogWarning("Достигнут лимит одновременных звуковых эффектов");
                    return;
                }

                // Настраиваем источник
                source.Volume = globalVolume;
                source.Mute = isMuted;

                // Воспроизводим эффект
                await source.Play(key);
                OnEffectPlayed?.Invoke(key);

                // Возвращаем источник в пул
                ReturnEffectSource(source);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при воспроизведении эффекта {key}: {ex}");
            }
        }

        private SourceAudio GetEffectSource()
        {
            if (effectSourcesPool.Count > 0)
            {
                var source = effectSourcesPool.Dequeue();
                activeEffectSources.Add(source);
                return source;
            }
            return null;
        }

        private void ReturnEffectSource(SourceAudio source)
        {
            if (source != null)
            {
                activeEffectSources.Remove(source);
                effectSourcesPool.Enqueue(source);
            }
        }

        private void CleanupEffectSources()
        {
            var inactiveSources = activeEffectSources.Where(s => !s.IsPlaying).ToList();
            foreach (var source in inactiveSources)
            {
                ReturnEffectSource(source);
            }
        }

        #endregion

        #region Управление громкостью

        public void SetGlobalVolume(float volume)
        {
            globalVolume = Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME);
            
            // Обновляем громкость всех источников
            foreach (var source in musicSources.Values)
            {
                if (source != null) source.Volume = globalVolume;
            }
            
            foreach (var source in activeEffectSources)
            {
                if (source != null) source.Volume = globalVolume;
            }

            OnVolumeChanged?.Invoke(globalVolume);
        }

        public void SetMusicVolume(string key, float volume)
        {
            if (musicSources.TryGetValue(key, out var source))
            {
                source.Volume = Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME);
            }
        }

        public void Mute()
        {
            isMuted = true;
            foreach (var source in musicSources.Values.Concat(activeEffectSources))
            {
                if (source != null) source.Mute = true;
            }
        }

        public void Unmute()
        {
            isMuted = false;
            foreach (var source in musicSources.Values.Concat(activeEffectSources))
            {
                if (source != null) source.Mute = false;
            }
        }

        #endregion

        #region Управление временем

        public float GetMusicLength(string key)
        {
            if (musicSources.TryGetValue(key, out var source))
            {
                var unitySource = source.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    return unitySource.clip.length;
                }
            }
            return 0f;
        }

        public float GetMusicTime(string key)
        {
            if (musicSources.TryGetValue(key, out var source))
            {
                return source.Time;
            }
            return 0f;
        }

        public void SetMusicTime(string key, float normalizedTime)
        {
            if (musicSources.TryGetValue(key, out var source))
            {
                var unitySource = source.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    float clipTime = Mathf.Clamp01(normalizedTime) * unitySource.clip.length;
                    source.Time = clipTime;
                }
            }
        }

        #endregion

        #region Состояние

        public bool IsMusicPlaying(string key)
        {
            return musicSources.TryGetValue(key, out var source) && 
                   source != null && 
                   source.IsPlayingKey(key);
        }

        public bool IsAnyMusicPlaying()
        {
            return musicSources.Any(pair => pair.Value != null && pair.Value.IsPlaying);
        }

        public string[] GetPlayingMusicKeys()
        {
            return musicSources.Where(pair => pair.Value != null && pair.Value.IsPlaying)
                              .Select(pair => pair.Key)
                              .ToArray();
        }

        public float GetGlobalVolume()
        {
            return globalVolume;
        }

        public bool IsMuted()
        {
            return isMuted;
        }

        #endregion

        private void OnDestroy()
        {
            StopAllMusic();
            instance = null;
        }
    }
} 