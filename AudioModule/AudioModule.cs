using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Основной класс модуля для работы с аудио в WebGL играх.
    /// Реализует функционал проигрывания звуков через Unity и WebGL API
    /// </summary>
    public class AudioModule : MonoBehaviour
    {
        #region Variables
        private static AudioModule instance;
        private static bool isInitialized = false;
        private string moduleAttribute = "AudioModule";


        // Кэш для звуковых клипов
        private readonly ConcurrentDictionary<string, AudioClip> audioCache = new ConcurrentDictionary<string, AudioClip>();

        // Настройки
#if UNITY_WEBGL && !UNITY_EDITOR
        private float defaultVolume = AudioSettings.DEFAULT_VOLUME;
#endif
        private int maxCacheItems = 100;
        #endregion

        #region Singleton
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
        public event Action<string> OnSoundLoaded;
        public event Action<string> OnSoundFailed;
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
            
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            try
            {
                LoadSettings();
                InitializeWebGL();
                PreloadSounds();
                isInitialized = true;
                YandexGameManager.Log("AudioModule инициализирован", "success", moduleAttribute);
            }
            catch (Exception ex)
            {
                YandexGameManager.Log($"Ошибка при инициализации AudioModule: {ex.Message}", "error", moduleAttribute);
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Загрузка настроек из PlayerPrefs или другого источника
                maxCacheItems = PlayerPrefs.GetInt("AudioModule_MaxCacheItems", 100);
            }
            catch (Exception ex)
            {
                YandexGameManager.Log($"Ошибка при загрузке настроек AudioModule: {ex.Message}", "error", moduleAttribute);
            }
        }

        private void InitializeWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // WebGL инициализация теперь происходит через AudioManager
                YandexGameManager.Log("WebGL аудио мост инициализирован", "success", moduleAttribute);
            }
            catch (Exception ex)
            {
                YandexGameManager.Log($"Ошибка при инициализации WebGL аудио моста: {ex.Message}", "error", moduleAttribute);
            }
#endif
        }

        private void PreloadSounds()
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return;

            // Получаем все звуки для предзагрузки и сортируем по приоритету
            var clipsToPreload = database.GetAllKeys()
                .Select(k => database.GetClipData(k))
                .Where(c => c.PreloadOnStart)
                .OrderByDescending(c => c.LoadPriority)
                .ToList();

            foreach (var clipData in clipsToPreload)
            {
                LoadAudioClipAsync(clipData.Key).ContinueWith(t => {
                    if (t.IsCompleted && !t.IsFaulted)
                    {
                        YandexGameManager.UnityLog($"Предзагружен звук {clipData.Key} с приоритетом {clipData.LoadPriority}", "info", moduleAttribute);
                    }
                });
            }
        }
        #endregion

        #region API

        /// <summary>
        /// Загружает аудио клип синхронно
        /// </summary>
        public AudioClip LoadAudioClip(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            try
            {
                // Проверяем кэш
                if (audioCache.TryGetValue(key, out var cachedClip))
                {
                    OnSoundPlayed?.Invoke(key);
                    return cachedClip;
                }

                // Загружаем базу данных
                var database = Resources.Load<AudioDatabase>("AudioDatabase");
                if (database == null)
                {
                    YandexGameManager.Log("AudioDatabase не найден в Resources", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }

                // Получаем данные о клипе
                var clipData = database.GetClipData(key);
                if (clipData == null)
                {
                    YandexGameManager.Log($"Звук с ключом {key} не найден в базе данных", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }

                // Если клип уже загружен в базе данных
                if (clipData.Clip != null)
                {
                    audioCache.TryAdd(key, clipData.Clip);
                    OnSoundLoaded?.Invoke(key);
                    OnSoundPlayed?.Invoke(key);
                    return clipData.Clip;
                }

                // Загружаем из Resources
                string resourcePath = clipData.Path.Replace("Assets/Resources/", "").Replace(".mp3", "").Replace(".wav", "").Replace(".ogg", "");
                var clip = Resources.Load<AudioClip>(resourcePath);
                
                if (clip != null)
                {
                    audioCache.TryAdd(key, clip);
                    clipData.Clip = clip; // Сохраняем в базе данных
                    OnSoundLoaded?.Invoke(key);
                    OnSoundPlayed?.Invoke(key);
                    return clip;
                }
                else
                {
                    YandexGameManager.Log($"Не удалось загрузить звук {key} из {resourcePath}", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }
            }
            catch (Exception ex)
            {
                YandexGameManager.Log($"Ошибка при загрузке звука {key}: {ex.Message}", "error", moduleAttribute);
                OnSoundFailed?.Invoke(key);
                return null;
            }
        }

        /// <summary>
        /// Загружает аудио клип асинхронно
        /// </summary>
        public async Task<AudioClip> LoadAudioClipAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            try
            {
                // Проверяем кэш
                if (audioCache.TryGetValue(key, out var cachedClip))
                {
                    OnSoundPlayed?.Invoke(key);
                    return cachedClip;
                }

                // Загружаем базу данных
                var database = Resources.Load<AudioDatabase>("AudioDatabase");
                if (database == null)
                {
                    YandexGameManager.Log("AudioDatabase не найден в Resources", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }

                // Получаем данные о клипе
                var clipData = database.GetClipData(key);
                if (clipData == null)
                {
                    YandexGameManager.Log($"Звук с ключом {key} не найден в базе данных", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }

                // Если клип уже загружен в базе данных
                if (clipData.Clip != null)
                {
                    audioCache.TryAdd(key, clipData.Clip);
                    OnSoundLoaded?.Invoke(key);
                    OnSoundPlayed?.Invoke(key);
                    return clipData.Clip;
                }

                // Загружаем асинхронно из Resources
                string resourcePath = clipData.Path.Replace("Assets/Resources/", "").Replace(".mp3", "").Replace(".wav", "").Replace(".ogg", "");
                var request = Resources.LoadAsync<AudioClip>(resourcePath);
                await request;

                var clip = request.asset as AudioClip;
                if (clip != null)
                {
                    audioCache.TryAdd(key, clip);
                    clipData.Clip = clip; // Сохраняем в базе данных
                    OnSoundLoaded?.Invoke(key);
                    OnSoundPlayed?.Invoke(key);
                return clip;
                }
                else
                {
                    YandexGameManager.Log($"Не удалось загрузить звук {key} из {resourcePath}", "error", moduleAttribute);
                    OnSoundFailed?.Invoke(key);
                    return null;
                }
            }
            catch (Exception ex)
            {
                YandexGameManager.Log($"Ошибка при загрузке звука {key}: {ex.Message}", "error", moduleAttribute);
                OnSoundFailed?.Invoke(key);
                return null;
            }
        }

        

        /// <summary>
        /// Загружает группу звуков с учетом приоритета
        /// </summary>
        public async Task LoadSoundGroupAsync(IEnumerable<string> keys)
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return;

            // Сортируем звуки по приоритету
            var sortedClips = keys
                .Select(k => database.GetClipData(k))
                .Where(c => c != null)
                .OrderByDescending(c => c.LoadPriority)
                .ToList();

            foreach (var clipData in sortedClips)
            {
                await LoadAudioClipAsync(clipData.Key);
            }
        }

        private void ManageCache()
        {
            // Очистка старых элементов кэша если превышен лимит
            if (audioCache.Count >= maxCacheItems)
            {
                var database = Resources.Load<AudioDatabase>("AudioDatabase");
                if (database == null) return;

                // Получаем список звуков, отсортированный по приоритету (сначала удаляем с низким приоритетом)
                var cachesToRemove = audioCache.Keys
                    .Select(k => new { Key = k, Priority = database.GetClipData(k)?.LoadPriority ?? 0 })
                    .OrderBy(x => x.Priority)
                    .Take(audioCache.Count - maxCacheItems + 1)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in cachesToRemove)
                {
                    audioCache.TryRemove(key, out _);
                    Debug.Log($"Удален из кэша звук с низким приоритетом: {key}");
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WebGLAudioBridge.OnSoundLoaded -= HandleSoundLoaded;
                WebGLAudioBridge.OnSoundError -= HandleSoundError;
                WebGLAudioBridge.OnSoundComplete -= HandleSoundComplete;
                WebGLAudioBridge.OnMuteChanged -= HandleMuteChanged;
#endif

                // Очистка ресурсов
                audioCache.Clear();
                instance = null;
                isInitialized = false;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void HandleSoundLoaded(string key)
        {
            OnSoundLoaded?.Invoke(key);
        }

        private void HandleSoundError(string key, string error)
        {
            YandexGameManager.Log($"Ошибка загрузки звука {key}: {error}", "error", moduleAttribute);
            OnSoundFailed?.Invoke(key);
        }

        private void HandleSoundComplete(string key)
        {
            OnSoundStopped?.Invoke(key);
        }

        private void HandleMuteChanged(bool isMuted)
        {
            // Обработка изменения состояния мута в WebGL
            var sources = FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
            foreach (var source in sources)
            {
                source.Mute = isMuted;
            }
        }
#endif

        public void OnSoundFinished(string key)
        {
            OnSoundStopped?.Invoke(key);
        }

        /// <summary>
        /// Предзагружает группу звуков по тегу
        /// </summary>
        public async Task PreloadSoundsByTag(string tag)
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return;

            var clipsToLoad = database.GetClipsByTag(tag);
            foreach (var clipData in clipsToLoad)
            {
                await LoadAudioClipAsync(clipData.Key);
            }
        }

        /// <summary>
        /// Очищает кэш для определенной группы звуков
        /// </summary>
        public void ClearCacheByTag(string tag)
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return;

            var clipsToRemove = database.GetClipsByTag(tag);
            foreach (var clipData in clipsToRemove)
            {
                audioCache.TryRemove(clipData.Key, out _);
            }
        }

        /// <summary>
        /// Проверяет, загружен ли звук в кэш
        /// </summary>
        public bool IsSoundCached(string key)
        {
            return audioCache.ContainsKey(key);
        }

        /// <summary>
        /// Получает информацию о состоянии кэша
        /// </summary>
        public (int count, float memoryUsageMB) GetCacheInfo()
        {
            float totalMemory = 0f;
            foreach (var clip in audioCache.Values)
            {
                if (clip != null)
                {
                    // Приблизительный расчет памяти (частота * каналы * длительность * 2 байта на сэмпл)
                    totalMemory += clip.frequency * clip.channels * clip.length * 2f / (1024f * 1024f);
                }
            }
            return (audioCache.Count, totalMemory);
        }

        /// <summary>
        /// Анализирует звуковой клип и возвращает его характеристики
        /// </summary>
        public AudioClipInfo GetClipInfo(string key)
        {
            if (!audioCache.TryGetValue(key, out var clip) || clip == null)
                return null;

            return new AudioClipInfo
            {
                Name = clip.name,
                Length = clip.length,
                Channels = clip.channels,
                Frequency = clip.frequency,
                SampleCount = clip.samples,
                MemoryUsageMB = (clip.frequency * clip.channels * clip.length * 2f) / (1024f * 1024f)
            };
        }

        /// <summary>
        /// Информация о звуковом клипе
        /// </summary>
        public class AudioClipInfo
        {
            public string Name { get; set; }
            public float Length { get; set; }
            public int Channels { get; set; }
            public int Frequency { get; set; }
            public int SampleCount { get; set; }
            public float MemoryUsageMB { get; set; }
        }

        /// <summary>
        /// Проверяет, есть ли звук в базе данных
        /// </summary>
        public bool DoesSoundExist(string key)
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            return database != null && database.ContainsKey(key);
        }

        /// <summary>
        /// Получает длительность звука
        /// </summary>
        public float GetSoundDuration(string key)
        {
            if (audioCache.TryGetValue(key, out var clip) && clip != null)
                return clip.length;

            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return 0f;

            var clipData = database.GetClipData(key);
            return clipData?.Clip != null ? clipData.Clip.length : 0f;
        }

        /// <summary>
        /// Очищает неиспользуемые звуки из кэша
        /// </summary>
        public void CleanupUnusedSounds(float unusedThresholdSeconds = 300f)
        {
            var keysToRemove = new List<string>();
            var currentTime = Time.realtimeSinceStartup;

            foreach (var key in audioCache.Keys)
            {
                if (!IsAudioPlaying(key) && (currentTime - GetLastPlayTime(key)) > unusedThresholdSeconds)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                audioCache.TryRemove(key, out _);
                YandexGameManager.Log($"Очищен неиспользуемый звук: {key}", "info", moduleAttribute);
            }
        }

        private Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>();

        private float GetLastPlayTime(string key)
        {
            return lastPlayTimes.TryGetValue(key, out float time) ? time : 0f;
        }

        private bool IsAudioPlaying(string key)
        {
            // Проверяем через AudioManager, играет ли сейчас этот звук
            return AudioManager.Instance != null && 
                   AudioManager.Instance.IsAudioPlaying(key);
        }

        /// <summary>
        /// Устанавливает приоритет загрузки для звука
        /// </summary>
        public void SetLoadPriority(string key, int priority)
        {
            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null) return;

            var clipData = database.GetClipData(key);
            if (clipData != null)
            {
                clipData.LoadPriority = priority;
            }
        }

        /// <summary>
        /// Получает статистику использования звуков
        /// </summary>
        public Dictionary<string, SoundUsageStats> GetSoundUsageStats()
            {
            var stats = new Dictionary<string, SoundUsageStats>();
            
            foreach (var key in audioCache.Keys)
            {
                if (soundUsageStats.TryGetValue(key, out var usageStats))
                {
                    stats[key] = usageStats;
                }
            }
            
            return stats;
        }

        private Dictionary<string, SoundUsageStats> soundUsageStats = new Dictionary<string, SoundUsageStats>();

        /// <summary>
        /// Статистика использования звука
        /// </summary>
        public class SoundUsageStats
        {
            public int PlayCount { get; set; }
            public float TotalPlayTime { get; set; }
            public float LastPlayTime { get; set; }
            public float AveragePlayTime => PlayCount > 0 ? TotalPlayTime / PlayCount : 0f;
        }

        private void UpdateSoundStats(string key, float playTime)
        {
            if (!soundUsageStats.TryGetValue(key, out var stats))
            {
                stats = new SoundUsageStats();
                soundUsageStats[key] = stats;
            }

            stats.PlayCount++;
            stats.TotalPlayTime += playTime;
            stats.LastPlayTime = Time.realtimeSinceStartup;
        }
    }
}

#endregion