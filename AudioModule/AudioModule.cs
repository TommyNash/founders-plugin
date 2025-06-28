using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using FoundersKit.Logging;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Модуль для работы с аудио в Unity/WebGL
    /// </summary>
    public class AudioModule : MonoBehaviour
    {
        #region Private Variables
        public const string CONTEXT_NAME = "AudioModule";

        private AudioDatabase database;
        private SourceAudio mainTrackSource;                                                    // Ссылка на текущий основной трек
        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private List<SourceAudio> registeredSources = new List<SourceAudio>();

        private static bool isInitialized = false;
        private float globalVolume = 1f;
        private float globalPitch = 1f;
        private bool isMuted = false;
        #endregion

        #region Singleton
        private static AudioModule instance;
        public static AudioModule Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioModule");
                    instance = go.AddComponent<AudioModule>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Event Delegates
        public event Action<string> OnMainTrackStarted;
        public event Action<string> OnMainTrackStopped;
        public event Action<string> OnOneShotPlayed;
        public event Action<float> OnVolumeChanged;
        public event Action<string> OnSoundLoaded;
        public event Action<string, string> OnSoundFailed;
        public event Action<string> OnSoundPlayed;
        public event Action<string> OnSoundStopped;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Инициализируем WebGL Audio System
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.InitializeAudioSystem(globalVolume);
#endif

            if (!isInitialized)
            {
                Initialize();
            }

            LoadDatabase();
            PreloadClips();
        }

        private void Initialize()
        {
            try
            {
                isInitialized = true;
                Log.Success("Module initialized", CONTEXT_NAME);
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing {CONTEXT_NAME}: {ex.Message}", CONTEXT_NAME);
            }
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AudioDatabase>(AudioSettings.DATABASE_PATH);
            if (database == null)
            {
                Log.Error($"Не удалось загрузить базу данных аудио из {AudioSettings.DATABASE_PATH}", CONTEXT_NAME);
            }
        }

        private void PreloadClips()
        {
            if (database == null) return;
            StartCoroutine(PreloadClipsCoroutine());
        }

        private IEnumerator PreloadClipsCoroutine()
        {
            if (database == null) yield break;

            foreach (var clipData in database.GetPreloadClips())
            {
                AudioClip clip = null;

                #if UNITY_EDITOR
                // В редакторе всегда используем клип из базы данных
                clip = clipData.Clip;
                #else
                // В WebGL билде всегда используем клип из базы данных
                // Resources.Load не работает для файлов вне папки Resources
                clip = clipData.Clip;
                #endif

                try
                {
                    if (clip != null)
                    {
                        clipCache[clipData.Key] = clip;
                        OnSoundLoaded?.Invoke(clipData.Key);
                    }
                    else
                    {
                        OnSoundFailed?.Invoke(clipData.Key, "Не удалось загрузить аудио клип");
                    }
                }
                catch (Exception ex)
                {
                    OnSoundFailed?.Invoke(clipData.Key, ex.Message);
                }
            }
        }

        public AudioClip LoadAudioClipAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                var error = "Ключ звука не может быть пустым";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            // Проверяем кэш
            if (clipCache.TryGetValue(key, out var cachedClip))
            {
                OnSoundLoaded?.Invoke(key);
                return cachedClip;
            }

            // Загружаем из базы данных
            var clipData = database?.GetClipData(key);
            if (clipData == null)
            {
                var error = $"Не удалось найти данные аудио клипа для ключа {key}";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            if (clipData.Clip == null)
            {
                var error = $"Аудио клип для ключа {key} не загружен в базу данных";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            AudioClip clip = null;

            #if UNITY_EDITOR
            // В редакторе всегда используем клип из базы данных
            clip = clipData.Clip;
            #else
            // В WebGL билде всегда используем клип из базы данных
            // Resources.Load не работает для файлов вне папки Resources
            clip = clipData.Clip;
            #endif

            // Добавляем в кэш
            clipCache[key] = clip;
            OnSoundLoaded?.Invoke(key);
            return clip;
        }


        public AudioClip LoadAudioClip(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                var error = "Ключ звука не может быть пустым";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            // Проверяем кэш
            if (clipCache.TryGetValue(key, out var cachedClip))
            {
                OnSoundLoaded?.Invoke(key);
                return cachedClip;
            }

            // Загружаем из базы данных
            var clipData = database?.GetClipData(key);
            if (clipData == null)
            {
                var error = $"Не удалось найти данные аудио клипа для ключа {key}";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            if (clipData.Clip == null)
            {
                var error = $"Аудио клип для ключа {key} не загружен в базу данных";
                Log.Error($"{error}", CONTEXT_NAME);
                OnSoundFailed?.Invoke(key, error);
                return null;
            }

            AudioClip clip = null;

            #if UNITY_EDITOR
            // В редакторе всегда используем клип из базы данных
            clip = clipData.Clip;
            #else
            // В WebGL билде всегда используем клип из базы данных
            // Resources.Load не работает для файлов вне папки Resources
            clip = clipData.Clip;
            #endif

            // Добавляем в кэш
            clipCache[key] = clip;
            OnSoundLoaded?.Invoke(key);
            return clip;
        }

        public void RegisterSource(SourceAudio source)
        {
            if (source != null && !registeredSources.Contains(source))
            {
                registeredSources.Add(source);
            }
        }

        public void UnregisterSource(SourceAudio source)
        {
            if (source != null)
            {
                registeredSources.Remove(source);
                
                // Очищаем ссылку на основной трек, если он уничтожается
                if (source == mainTrackSource)
                {
                    mainTrackSource = null;
                }
            }
        }

        #endregion

        #region Public API
        /// <summary>
        /// Получает текущее значение глобальной громкости
        /// </summary>
        /// <returns>Значение громкости от 0 до 1</returns>
        public float GetVolume()
        {
            return globalVolume;
        }

        /// <summary>
        /// Устанавливает глобальную громкость для всех источников звука
        /// </summary>
        /// <param name="volume">Значение громкости от 0 до 1</param>
        public void SetVolume(float volume)
        {
            globalVolume = Mathf.Clamp(volume, 0f, 1f);
            if (!isMuted)
            {
                foreach (var source in registeredSources)
                {
                    if (source != null)
                    {
                        source.Volume = globalVolume;
                        source.Pitch = globalPitch;
                    }
                }
                NotifyVolumeChanged(globalVolume);
                
                // Обновляем WebGL Audio System
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (WebGLAudioBridge.IsInitialized)
                {
                    WebGLAudioBridge.SetGlobalVolume(globalVolume);
                }
                #endif
            }
        }

        /// <summary>
        /// Отключает все звуки
        /// </summary>
        /// <remarks>
        /// Сохраняет текущее значение громкости для последующего включения
        /// </remarks>
        public void Mute()
        {
            if (!isMuted)
            {
                isMuted = true;
                foreach (var source in registeredSources)
                {
                    if (source != null)
                    {
                        source.Mute = true;
                    }
                }
                NotifyVolumeChanged(0f);
                
                // Обновляем WebGL Audio System
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (WebGLAudioBridge.IsInitialized)
                {
                    WebGLAudioBridge.SetGlobalVolume(0f);
                }
                #endif
            }
        }

        /// <summary>
        /// Включает все звуки с сохраненной громкостью
        /// </summary>
        public void Unmute()
        {
            if (isMuted)
            {
                isMuted = false;
                foreach (var source in registeredSources)
                {
                    if (source != null)
                    {
                        source.Mute = false;
                    }
                }
                NotifyVolumeChanged(globalVolume);
                
                // Обновляем WebGL Audio System
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (WebGLAudioBridge.IsInitialized)
                {
                    WebGLAudioBridge.SetGlobalVolume(globalVolume);
                }
                #endif
            }
        }

        public void NotifyVolumeChanged(float volume)
        {
            OnVolumeChanged?.Invoke(volume);
        }

        /// <summary>
        /// Приостанавливает воспроизведение всех звуков
        /// </summary>
        /// <remarks>
        /// Сохраняет позицию воспроизведения для каждого источника
        /// </remarks>
        public void PauseAll()
        {
            foreach (var source in registeredSources)
            {
                if (source != null)
                {
                    source.Pause();
                }
            }
        }

        /// <summary>
        /// Возобновляет воспроизведение всех приостановленных звуков
        /// </summary>
        public void UnpauseAll()
        {
            foreach (var source in registeredSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }
        }

        /// <summary>
        /// Останавливает воспроизведение всех звуков
        /// </summary>
        /// <remarks>
        /// Сбрасывает позицию воспроизведения для всех источников
        /// </remarks>
        public void StopAll()
        {
            foreach (var source in registeredSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        /// <summary>
        /// Очищает кэш загруженных аудио клипов
        /// </summary>
        /// <remarks>
        /// Освобождает память, занятую неиспользуемыми аудио клипами
        /// </remarks>
        public void ClearCache()
        {
            clipCache.Clear();
            // Resources.UnloadUnusedAssets() - дорогая операция, вызывается только по явному запросу
        }

        /// <summary>
        /// Очищает кэш и освобождает неиспользуемые ресурсы
        /// </summary>
        /// <remarks>
        /// ВНИМАНИЕ: Это дорогая операция, которая может вызвать задержки!
        /// Используйте только при необходимости (например, при смене уровня или при нехватке памяти).
        /// Для обычной очистки кэша используйте ClearCache().
        /// </remarks>
        public void ClearCacheAndUnloadAssets()
        {
            clipCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Получает информацию о текущем состоянии кэша
        /// </summary>
        /// <returns>Словарь с информацией о количестве клипов и использованной памяти</returns>
        public Dictionary<string, object> GetCacheInfo()
        {
            return new Dictionary<string, object>
            {
                ["count"] = clipCache.Count,
                ["memory"] = CalculateMemoryUsage() / (1024f * 1024f) // Конвертируем в МБ
            };
        }

        private long CalculateMemoryUsage()
        {
            long totalMemory = 0;
            foreach (var clip in clipCache.Values)
            {
                if (clip != null)
                {
                    // Примерный расчет памяти для аудио клипа
                    // Частота дискретизации * количество каналов * битность * длительность
                    totalMemory += (long)(clip.frequency * clip.channels * 2 * clip.length);
                }
            }
            return totalMemory;
        }

        private void OnDestroy()
        {
            StopAll();
            ClearCache();
            registeredSources.Clear();
        }

        /// <summary>
        /// Воспроизводит звук как основной трек (с автоматической остановкой предыдущего)
        /// </summary>
        /// <param name="key">Ключ звука из базы данных</param>
        /// <remarks>
        /// Используйте для воспроизведения основных треков (музыка, зацикленные звуки).
        /// Предыдущий основной трек будет автоматически остановлен.
        /// </remarks>
        public void PlayMainTrack(string key)
        {
            // Останавливаем предыдущий основной трек
            if (mainTrackSource != null)
            {
                var previousKey = mainTrackSource.CurrentKey;
                mainTrackSource.Stop();
                OnMainTrackStopped?.Invoke(previousKey);
                OnSoundStopped?.Invoke(previousKey);
            }

            var source = GetOrCreateSource(key); // Создаем GameObject с именем ключа трека
            if (source != null)
            {
                mainTrackSource = source; // Сохраняем ссылку на основной трек
                source.Pitch = globalPitch;
                source.Volume = globalVolume;
                source.Play(key);
                OnMainTrackStarted?.Invoke(key);
                OnSoundPlayed?.Invoke(key);
            }
        }

        /// <summary>
        /// Останавливает текущий основной трек
        /// </summary>
        public void StopMainTrack()
        {
            if (mainTrackSource != null)
            {
                var key = mainTrackSource.CurrentKey;
                mainTrackSource.Stop();
                OnMainTrackStopped?.Invoke(key);
                OnSoundStopped?.Invoke(key);
            }
        }

        /// <summary>
        /// Воспроизводит звук как одиночный эффект (без остановки других звуков)
        /// </summary>
        /// <param name="key">Ключ звука из базы данных</param>
        /// <remarks>
        /// Используйте для воспроизведения коротких звуковых эффектов (клики, взрывы и т.д.).
        /// Звук воспроизводится поверх других звуков.
        /// </remarks>
        public void PlayOneShot(string key)
        {
            var source = GetOrCreateSource($"OneShot_{key}");
            if (source != null)
            {
                source.Pitch = globalPitch;
                source.Volume = globalVolume;
                source.Play(key);
                OnOneShotPlayed?.Invoke(key);
                OnSoundPlayed?.Invoke(key);
            }
        }

        /// <summary>
        /// Воспроизводит звук как фоновый трек (без остановки других звуков)
        /// </summary>
        /// <param name="key">Ключ звука из базы данных</param>
        /// <remarks>
        /// Используйте для воспроизведения фоновых звуков (амбиент, ветер и т.д.).
        /// Звук воспроизводится параллельно с другими звуками.
        /// </remarks>
        public void PlayBackground(string key)
        {
            var source = GetOrCreateSource($"Background_{key}");
            if (source != null)
            {
                source.Pitch = globalPitch;
                source.Volume = globalVolume;
                source.Play(key);
                OnSoundPlayed?.Invoke(key);
            }
        }

        /// <summary>
        /// Воспроизводит звук как временный трек (с автоматической остановкой после завершения)
        /// </summary>
        /// <param name="key">Ключ звука из базы данных</param>
        /// <remarks>
        /// Используйте для воспроизведения временных звуков (диалоги, объявления и т.д.).
        /// После завершения воспроизведения источник звука будет автоматически удален.
        /// </remarks>
        public void PlayTemporary(string key)
        {
            var source = GetOrCreateSource($"Temporary_{key}");
            if (source != null)
            {
                source.Pitch = globalPitch;
                source.Volume = globalVolume;
                source.Play(key);
                OnSoundPlayed?.Invoke(key);
                
                // Удаляем источник после завершения воспроизведения
                if (source.gameObject != null)
                {
                    UnregisterSource(source); // Убираем из списка перед уничтожением
                    Destroy(source.gameObject);
                }
            }
        }

        /// <summary>
        /// Устанавливает глобальный pitch для всех источников звука
        /// </summary>
        /// <param name="pitch">Значение pitch от 0.5 до 2.0</param>
        /// <remarks>
        /// Pitch влияет на скорость воспроизведения звука. Значение 1.0 - нормальная скорость,
        /// меньше 1.0 - замедление, больше 1.0 - ускорение
        /// </remarks>
        public void SetPitch(float pitch)
        {
            globalPitch = Mathf.Clamp(pitch, SourceAudio.MIN_PITCH, SourceAudio.MAX_PITCH);
            foreach (var source in registeredSources)
            {
                if (source != null)
                {
                    source.Pitch = globalPitch;
                }
            }
        }

        /// <summary>
        /// Получает текущее значение глобального pitch
        /// </summary>
        /// <returns>Значение pitch от 0.5 до 2.0</returns>
        public float GetPitch()
        {
            return globalPitch;
        }

        /// <summary>
        /// Устанавливает pitch для конкретного источника звука
        /// </summary>
        /// <param name="sourceKey">Ключ источника звука</param>
        /// <param name="pitch">Значение pitch от 0.5 до 2.0</param>
        public void SetSourcePitch(string sourceKey, float pitch)
        {
            var source = GetSourceByClipKey(sourceKey);
            if (source != null)
            {
                source.Pitch = Mathf.Clamp(pitch, SourceAudio.MIN_PITCH, SourceAudio.MAX_PITCH);
            }
        }

        /// <summary>
        /// Получает текущее значение pitch для конкретного источника звука
        /// </summary>
        /// <param name="sourceKey">Ключ источника звука</param>
        /// <returns>Значение pitch от 0.5 до 2.0</returns>
        public float GetSourcePitch(string sourceKey)
        {
            var source = GetSourceByClipKey(sourceKey);
            return source != null ? source.Pitch : globalPitch;
        }

        /// <summary>
        /// Устанавливает громкость для конкретного источника звука
        /// </summary>
        /// <param name="sourceKey">Ключ источника звука</param>
        /// <param name="volume">Значение громкости от 0 до 1</param>
        public void SetSourceVolume(string sourceKey, float volume)
        {
            var source = GetSourceByClipKey(sourceKey);
            if (source != null)
            {
                source.Volume = Mathf.Clamp(volume, 0f, 1f);
            }
        }

        /// <summary>
        /// Получает текущее значение громкости для конкретного источника звука
        /// </summary>
        /// <param name="sourceKey">Ключ источника звука</param>
        /// <returns>Значение громкости от 0 до 1</returns>
        public float GetSourceVolume(string sourceKey)
        {
            var source = GetSourceByClipKey(sourceKey);
            return source != null ? source.Volume : globalVolume;
        }

        /// <summary>
        /// Получает ключ текущего основного трека
        /// </summary>
        /// <returns>Ключ текущего основного трека или null если трек не воспроизводится</returns>
        public string GetCurrentMainTrackKey()
        {
            return mainTrackSource?.CurrentKey;
        }

        /// <summary>
        /// Проверяет, воспроизводится ли основной трек
        /// </summary>
        /// <returns>true если основной трек воспроизводится</returns>
        public bool IsMainTrackPlaying()
        {
            return mainTrackSource != null && mainTrackSource.IsPlaying;
        }

        private SourceAudio GetOrCreateSource(string name)
        {
            var source = GetSourceByKey(name);
            if (source == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(null);
                DontDestroyOnLoad(go);
                source = go.AddComponent<SourceAudio>();
                RegisterSource(source);
            }
            return source;
        }

        private SourceAudio GetSourceByKey(string key)
        {
            return registeredSources.FirstOrDefault(s => s.gameObject.name == key);
        }

        private SourceAudio GetSourceByClipKey(string clipKey)
        {
            return registeredSources.FirstOrDefault(s => s.CurrentKey == clipKey);
        }

        #endregion
    }
} 