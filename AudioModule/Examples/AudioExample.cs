using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoundersKit.Modules.Examples
{
    /// <summary>
    /// Демонстрация всех возможностей AudioModule и SourceAudio через UI
    /// </summary>
    public class AudioExample : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("—— Модули")]
        [SerializeField] private AudioModule audioModule;

        [Header("Звуки")]
        [AudioKey] public string backgroundMusic;
        [AudioKey] public string buttonClick;
        [AudioKey] public string spatialSound;

        [Header("Компоненты")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider pitchSlider;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle loopToggle;
        [SerializeField] private Button playButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button effectButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button unpauseButton;
        [SerializeField] private Button jumpToStartButton;
        [SerializeField] private Button jumpToMiddleButton;
        [SerializeField] private Button fadeInButton;
        [SerializeField] private Button fadeOutButton;
        [SerializeField] private Button stopAllButton;
        [SerializeField] private Button pauseAllButton;
        [SerializeField] private Button unpauseAllButton;
        [SerializeField] private Button clearCacheButton;
        [SerializeField] private TMP_Dropdown soundsDropdown;
        [SerializeField] private TMP_Dropdown tagsDropdown;

        [Header("Текст")]
        [SerializeField] private TextMeshProUGUI volumeSliderText;
        [SerializeField] private TextMeshProUGUI pitchSliderText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI soundInfoText;
        [SerializeField] private TextMeshProUGUI cacheInfoText;
        [SerializeField] private TextMeshProUGUI currentTimeText;
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI activeSourcesText;

        [Header("Пространственный звук")]
        [SerializeField] private Transform soundSource; // Движущийся источник звука
        [SerializeField] private Transform listener; // Слушатель (камера)
        [SerializeField] private float sourceMovementRadius = 5f;
        [SerializeField] private float sourceMovementSpeed = 1f;
        [SerializeField] private TextMeshProUGUI spatialInfoText;
        [SerializeField] private Button toggleSpatialButton;
        [SerializeField] private TextMeshProUGUI spatialButtonText;
        [SerializeField] private Slider spatialDistanceSlider;
        [SerializeField] private TextMeshProUGUI spatialDistanceText;

        // Константы для pitch
        private const float MIN_PITCH = 0.5f;
        private const float MAX_PITCH = 2f;
        private const float DEFAULT_PITCH = 1f;

        private SourceAudio musicSource;
        private SourceAudio effectSource;
        private SourceAudio spatialSource;
        private AudioDatabase database;
        private string currentMusicKey;
        private bool isUpdatingSlider;
        private bool isDraggingSlider;
        private float currentClipLength;
        private float spatialAngle;
        private float savedTime;

        private void Awake()
        {
            // Если модуль не указан через инспектор, найдем его на сцене
            if (audioModule == null)
            {
                audioModule = FindFirstObjectByType<AudioModule>();
            }

            // Подписываемся на события модуля
            if (audioModule != null)
            {
                audioModule.OnSoundLoaded += OnSoundLoaded;
                audioModule.OnSoundFailed += OnSoundFailed;
                audioModule.OnSoundPlayed += OnSoundPlayed;
                audioModule.OnSoundStopped += OnSoundStopped;
                audioModule.OnMainTrackStarted += OnMainTrackStarted;
                audioModule.OnMainTrackStopped += OnMainTrackStopped;
                audioModule.OnOneShotPlayed += OnOneShotPlayed;
                audioModule.OnVolumeChanged += OnGlobalVolumeChanged;
            }

            // Подписываемся на события WebGL Audio Bridge
            WebGLAudioBridge.OnSoundLoaded += OnWebGLSoundLoaded;
            WebGLAudioBridge.OnSoundError += OnWebGLSoundError;
            WebGLAudioBridge.OnSoundComplete += OnWebGLSoundComplete;
            WebGLAudioBridge.OnMuteChanged += OnWebGLMuteChanged;
        }

        private void Start()
        {
            // Инициализация источников звука
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<SourceAudio>();
            }

            if (effectSource == null)
            {
                var effectObj = new GameObject("EffectSource");
                effectObj.transform.SetParent(transform);
                effectSource = effectObj.AddComponent<SourceAudio>();
            }
            
            // Создаем объект для пространственного звука только если он нужен
            if (soundSource != null && spatialSource == null)
            {
                spatialSource = soundSource.gameObject.AddComponent<SourceAudio>();
                spatialSource.IsSpatial = true;
            }

            // Загрузка базы данных
            database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null)
            {
                Debug.LogError("AudioDatabase не найден в Resources!");
                return;
            }

            // Инициализируем WebGL Audio System для тестирования в редакторе
            #if UNITY_EDITOR
            if (!WebGLAudioBridge.IsInitialized)
            {
                WebGLAudioBridge.InitializeAudioSystem(audioModule.GetVolume());
            }
            #endif

            SetupUI();
            SubscribeToEvents();

            // Запускаем обновление UI
            InvokeRepeating(nameof(UpdateTimeUI), 0.1f, 0.1f);
            InvokeRepeating(nameof(UpdateCacheInfo), 1f, 1f);
            InvokeRepeating(nameof(UpdateActiveSourcesInfo), 0.5f, 0.5f);
        }

        private void SetupUI()
        {
            // Настройка слайдера громкости
            if (volumeSlider != null)
            {
                volumeSlider.value = audioModule.GetVolume();
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeText(volumeSlider.value);
            }

            // Настройка слайдера pitch
            if (pitchSlider != null)
            {
                pitchSlider.minValue = MIN_PITCH;
                pitchSlider.maxValue = MAX_PITCH;
                pitchSlider.value = DEFAULT_PITCH;
                pitchSlider.onValueChanged.AddListener(OnPitchChanged);
                UpdatePitchText(DEFAULT_PITCH);
            }

            // Настройка слайдера расстояния для пространственного звука
            if (spatialDistanceSlider != null)
            {
                spatialDistanceSlider.minValue = 1f;
                spatialDistanceSlider.maxValue = 50f;
                spatialDistanceSlider.value = sourceMovementRadius;
                spatialDistanceSlider.onValueChanged.AddListener(OnSpatialDistanceChanged);
                UpdateSpatialDistanceText(sourceMovementRadius);
            }

            // Настройка переключателя mute
            if (muteToggle != null)
            {
                muteToggle.isOn = false;
                muteToggle.onValueChanged.AddListener(OnMuteChanged);
            }

            // Настройка переключателя loop
            if (loopToggle != null)
            {
                loopToggle.isOn = false;
                loopToggle.onValueChanged.AddListener(OnLoopChanged);
            }

            // Настройка кнопок
            if (playButton != null)
                playButton.onClick.AddListener(PlaySelectedSound);
            if (stopButton != null)
                stopButton.onClick.AddListener(StopSelectedSound);
            if (effectButton != null)
                effectButton.onClick.AddListener(PlayEffect);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseSelectedSound);
            if (unpauseButton != null)
                unpauseButton.onClick.AddListener(UnpauseSelectedSound);
            if (stopAllButton != null)
                stopAllButton.onClick.AddListener(StopAllSounds);
            if (pauseAllButton != null)
                pauseAllButton.onClick.AddListener(PauseAllSounds);
            if (unpauseAllButton != null)
                unpauseAllButton.onClick.AddListener(UnpauseAllSounds);
            if (clearCacheButton != null)
                clearCacheButton.onClick.AddListener(ClearCache);

            // Настройка кнопок позиции
            if (jumpToStartButton != null)
                jumpToStartButton.onClick.AddListener(JumpToStart);
            if (jumpToMiddleButton != null)
                jumpToMiddleButton.onClick.AddListener(JumpToMiddle);

            // Настройка кнопок фейда
            if (fadeInButton != null)
                fadeInButton.onClick.AddListener(FadeInMusic);
            if (fadeOutButton != null)
                fadeOutButton.onClick.AddListener(FadeOutMusic);

            // Заполнение выпадающего списка звуков
            if (soundsDropdown != null && database != null)
            {
                soundsDropdown.ClearOptions();
                var options = database.GetAllKeys().ToList();
                soundsDropdown.AddOptions(options);
                soundsDropdown.onValueChanged.AddListener(OnSoundSelected);
            }

            // Заполнение выпадающего списка тегов
            if (tagsDropdown != null && database != null)
            {
                tagsDropdown.ClearOptions();
                var options = database.GetAllTags().ToList();
                options.Insert(0, "Все звуки");
                tagsDropdown.AddOptions(options);
                tagsDropdown.onValueChanged.AddListener(OnTagSelected);
            }

            // Настройка кнопки пространственного звука
            if (toggleSpatialButton != null)
            {
                toggleSpatialButton.onClick.AddListener(ToggleSpatialSound);
                UpdateSpatialButtonText();
            }
        }

        private void SubscribeToEvents()
        {
            if (musicSource != null)
            {
                musicSource.OnFinished += OnMusicFinished;
                musicSource.OnLoaded += OnSourceLoaded;
                musicSource.OnError += OnSourceError;
            }

            if (effectSource != null)
            {
                effectSource.OnFinished += OnEffectFinished;
                effectSource.OnLoaded += OnSourceLoaded;
                effectSource.OnError += OnSourceError;
            }

            if (spatialSource != null)
            {
                spatialSource.OnFinished += OnSpatialSoundFinished;
                spatialSource.OnLoaded += OnSourceLoaded;
                spatialSource.OnError += OnSourceError;
            }
        }

        #region Обработчики событий модуля
        private void OnSoundLoaded(string key)
        {
            ShowMessage($"Звук загружен: {key}");
            UpdateSoundInfo(key);
        }

        private void OnSoundFailed(string key, string error)
        {
            ShowMessage($"Ошибка загрузки звука: {key} - {error}", true);
        }

        private void OnSoundPlayed(string key)
        {
            ShowMessage($"Звук воспроизводится: {key}");
        }

        private void OnSoundStopped(string key)
        {
            ShowMessage($"Звук остановлен: {key}");
        }

        private void OnMainTrackStarted(string key)
        {
            ShowMessage($"Основной трек начат: {key}");
        }

        private void OnMainTrackStopped(string key)
        {
            ShowMessage($"Основной трек остановлен: {key}");
        }

        private void OnOneShotPlayed(string key)
        {
            ShowMessage($"Воспроизведен одиночный звук: {key}");
        }

        private void OnGlobalVolumeChanged(float volume)
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = volume;
                UpdateVolumeText(volume);
            }
        }
        #endregion

        #region Обработчики событий источников
        private void OnSourceLoaded(string key)
        {
            ShowMessage($"Источник загрузил звук: {key}");
        }

        private void OnSourceError(string key, string error)
        {
            ShowMessage($"Ошибка источника {key}: {error}", true);
        }

        private void OnMusicFinished()
        {
            ShowMessage("Музыка завершила воспроизведение");
        }

        private void OnEffectFinished()
        {
            ShowMessage("Эффект завершил воспроизведение");
        }

        private void OnSpatialSoundFinished()
        {
            ShowMessage("Пространственный звук завершил воспроизведение");
            UpdateSpatialButtonText();
        }
        #endregion

        #region UI обработчики
        private void OnVolumeChanged(float value)
        {
            audioModule.SetVolume(value);
            UpdateVolumeText(value);
        }

        private void OnPitchChanged(float value)
        {
            if (musicSource != null)
                musicSource.Pitch = value;
            UpdatePitchText(value);
        }

        private void OnSpatialDistanceChanged(float value)
        {
            sourceMovementRadius = value;
            UpdateSpatialDistanceText(value);
        }

        private void OnMuteChanged(bool value)
        {
            if (value)
                audioModule.Mute();
            else
                audioModule.Unmute();
            ShowMessage(value ? "Звук отключен" : "Звук включен");
        }

        private void OnLoopChanged(bool value)
        {
            if (musicSource != null)
                musicSource.Loop = value;
        }

        private void UpdateVolumeText(float value)
        {
            if (volumeSliderText != null)
                volumeSliderText.text = $"Громкость: {value:F2}";
        }

        private void UpdatePitchText(float value)
        {
            if (pitchSliderText != null)
                pitchSliderText.text = $"Pitch: {value:F2}";
        }

        private void UpdateSpatialDistanceText(float value)
        {
            if (spatialDistanceText != null)
                spatialDistanceText.text = $"Расстояние: {value:F1}м";
        }

        private void UpdateActiveSourcesInfo()
        {
            if (activeSourcesText != null)
            {
                var sources = FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
                var activeCount = sources.Count(s => s.IsPlaying);
                activeSourcesText.text = $"Активные источники: {activeCount}/{sources.Length}";
            }
        }

        private void ShowMessage(string message, bool isError = false)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = isError ? Color.red : Color.white;
            }
        }
        #endregion

        #region Управление воспроизведением
        private void PlaySelectedSound()
        {
            if (soundsDropdown != null && musicSource != null)
            {
                string selectedSound = soundsDropdown.options[soundsDropdown.value].text;
                currentMusicKey = selectedSound;

                // Останавливаем предыдущее воспроизведение
                if (musicSource.IsPlaying)
                {
                    musicSource.Stop();
                }

                // Загружаем и воспроизводим новый звук
                var parameters = new PlaySoundParameters
                {
                    volume = volumeSlider.value,
                    loop = loopToggle.isOn,
                    pitch = pitchSlider.value
                };

                musicSource.PlayWithParameters(selectedSound, parameters);
                
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    currentClipLength = unitySource.clip.length;
                    UpdateTimeUI();
                }
                
                UpdateSoundInfo(selectedSound);
                ShowMessage($"Начато воспроизведение: {selectedSound}");
            }
        }

        private void StopSelectedSound()
        {
            if (musicSource != null)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    musicSource.Stop();
                    savedTime = 0f;
                    // Не сбрасываем UI времени, чтобы пользователь мог видеть общую длительность
                    ShowMessage("Воспроизведение остановлено");
                }
            }
        }

        private void PauseSelectedSound()
        {
            if (musicSource != null)
            {
                if (musicSource.IsPlaying)
                {
                    var unitySource = musicSource.GetComponent<AudioSource>();
                    if (unitySource != null && unitySource.clip != null)
                    {
                        savedTime = unitySource.time;
                        musicSource.Pause();
                        ShowMessage("Воспроизведение приостановлено");
                    }
                }
                else
                {
                    ShowMessage("Нет активного воспроизведения для паузы", true);
                }
            }
        }

        private void UnpauseSelectedSound()
        {
            if (musicSource != null)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null && !musicSource.IsPlaying && savedTime > 0)
                {
                    musicSource.UnPause();
                    try
                    {
                        unitySource.time = savedTime;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Не удалось восстановить время воспроизведения: {e.Message}");
                    }
                    ShowMessage("Воспроизведение возобновлено");
                }
                else
                {
                    ShowMessage("Нет приостановленного воспроизведения", true);
                }
            }
        }

        private void PlayEffect()
        {
            if (!string.IsNullOrEmpty(buttonClick))
            {
                // Если эффект уже воспроизводится, останавливаем его
                if (effectSource != null && effectSource.IsPlaying)
                {
                    effectSource.Stop();
                }

                // Воспроизводим эффект
                var parameters = new PlaySoundParameters
                {
                    volume = volumeSlider.value,
                    pitch = pitchSlider.value
                };

                effectSource.PlayWithParameters(buttonClick, parameters);
            }
        }

        private void StopAllSounds()
        {
            // Останавливаем все источники
            if (musicSource != null)
            {
                musicSource.Stop();
                savedTime = 0f;
            }

            if (effectSource != null)
            {
                effectSource.Stop();
            }

            if (spatialSource != null)
            {
                spatialSource.Stop();
            }

            // Очищаем все дополнительные источники
            var allSources = FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
            foreach (var source in allSources)
            {
                if (source != null && source != musicSource && source != effectSource && source != spatialSource)
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }

            // Не сбрасываем UI времени, чтобы пользователь мог видеть общую длительность
            ShowMessage("Все звуки остановлены");
        }

        private void PauseAllSounds()
        {
            audioModule.PauseAll();
            ShowMessage("Все звуки приостановлены");
        }

        private void UnpauseAllSounds()
        {
            audioModule.UnpauseAll();
            ShowMessage("Все звуки возобновлены");
        }

        private void ClearCache()
        {
            // Останавливаем все звуки перед очисткой кэша
            audioModule.StopAll();
            // Здесь должен быть метод очистки кэша, если он есть в AudioModule
            ShowMessage("Кэш очищен");
            UpdateCacheInfo();
        }

        private void StartSpatialDemo()
        {
            // Проверяем инициализацию WebGL Audio System для пространственного звука
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLAudioBridge.IsInitialized)
            {
                ShowMessage("WebGL Audio System не инициализирована. Пространственный звук недоступен.", true);
                return;
            }
            #endif

            if (spatialSource != null && !string.IsNullOrEmpty(spatialSound))
            {
                var parameters = new PlaySoundParameters
                {
                    loop = true,
                    spatial = true,
                    volume = audioModule.GetVolume()
                };

                spatialSource.PlayWithParameters(spatialSound, parameters);
            }
        }

        private void ToggleSpatialSound()
        {
            // Проверяем инициализацию WebGL Audio System для пространственного звука
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLAudioBridge.IsInitialized)
            {
                ShowMessage("WebGL Audio System не инициализирована. Пространственный звук недоступен.", true);
                return;
            }
            #endif

            if (spatialSource != null)
            {
                if (spatialSource.IsPlaying)
                {
                    spatialSource.Stop();
                }
                else
                {
                    StartSpatialDemo();
                }
                UpdateSpatialButtonText();
            }
        }

        private void UpdateSpatialButtonText()
        {
            if (spatialButtonText != null)
            {
                spatialButtonText.text = spatialSource != null && spatialSource.IsPlaying 
                    ? "Остановить пространственный звук" 
                    : "Запустить пространственный звук";
            }
        }

        private void JumpToStart()
        {
            if (musicSource != null && musicSource.IsPlaying)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    try
                    {
                        unitySource.time = 0f;
                        UpdateTimeUI();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Не удалось перемотать к началу: {e.Message}");
                    }
                }
            }
        }

        private void JumpToMiddle()
        {
            if (musicSource != null && musicSource.IsPlaying)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    try
                    {
                        unitySource.time = unitySource.clip.length / 2f;
                        UpdateTimeUI();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Не удалось перемотать к середине: {e.Message}");
                    }
                }
            }
        }

        private void FadeInMusic()
        {
            if (musicSource != null)
            {
                if (!musicSource.IsPlaying)
                {
                    PlaySelectedSound();
                }
                musicSource.FadeIn(2f);
                ShowMessage("Начат фейд-ин музыки");
            }
        }

        private void FadeOutMusic()
        {
            if (musicSource != null && musicSource.IsPlaying)
            {
                musicSource.FadeOut(1.5f);
                ShowMessage("Начат фейд-аут музыки");
            }
        }
        #endregion

        #region Обновление UI
        private void UpdateTimeUI()
        {
            if (musicSource == null)
            {
                ResetTimeUI();
                return;
            }

            var unitySource = musicSource.GetComponent<AudioSource>();
            if (unitySource == null || unitySource.clip == null)
            {
                ResetTimeUI();
                return;
            }

            // Если звук не воспроизводится и нет сохраненного времени, сбрасываем UI
            if (!musicSource.IsPlaying && savedTime <= 0)
            {
                ResetTimeUI();
                return;
            }

            if (!isDraggingSlider)
            {
                try
                {
                    float currentTime;
                    float totalTime = unitySource.clip.length;
                    
                    // Определяем текущее время
                    if (musicSource.IsPlaying)
                    {
                        currentTime = unitySource.time;
                        savedTime = currentTime; // Обновляем сохраненное время
                    }
                    else
                    {
                        // Если звук на паузе, используем сохраненное время
                        currentTime = savedTime;
                    }
                    
                    if (timeSlider != null)
                    {
                        timeSlider.value = totalTime > 0 ? currentTime / totalTime : 0f;
                    }
                    
                    UpdateTimeText(currentTime, totalTime);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Ошибка при обновлении UI времени: {e.Message}");
                    ResetTimeUI();
                }
            }
        }

        private void ResetTimeUI()
        {
            if (timeSlider != null) timeSlider.value = 0f;
            if (currentTimeText != null) currentTimeText.text = "0:00";
            if (totalTimeText != null) totalTimeText.text = "0:00";
        }

        private void UpdateTimeText(float currentTime, float totalTime)
        {
            if (currentTimeText != null)
                currentTimeText.text = FormatTime(currentTime);
            if (totalTimeText != null)
                totalTimeText.text = FormatTime(totalTime);
        }

        private string FormatTime(float timeInSeconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        }

        private void UpdateCacheInfo()
        {
            var cacheInfo = AudioModule.Instance.GetCacheInfo();
            int count = (int)cacheInfo["count"];
            float memory = (float)cacheInfo["memory"];
            cacheInfoText.text = $"Кэш: {count} клипов ({memory:F1} MB)";
        }

        private void UpdateSoundInfo(string key)
        {
            if (soundInfoText != null && database != null)
            {
                var clipData = database.GetClipData(key);
                if (clipData != null)
                {
                    var tagsList = database.GetClipTags(key).ToList();
                    string tagsString = tagsList.Count > 0 ? string.Join(", ", tagsList) : "нет тегов";
                    
                    soundInfoText.text = $"Информация о звуке:\n" +
                                       $"Ключ: {clipData.Key}\n" +
                                       $"Путь: {clipData.Path}\n" +
                                       $"Предзагрузка: {clipData.PreloadOnStart}\n" +
                                       $"Громкость по умолчанию: {clipData.DefaultVolume}\n" +
                                       $"Зацикленность: {clipData.Loop}\n" +
                                       $"Pitch: {clipData.Pitch}\n" +
                                       $"Теги: {tagsString}\n" +
                                       $"Приоритет загрузки: {clipData.LoadPriority}\n" +
                                       $"Пространственный: {clipData.Spatial}\n" +
                                       $"Мин. дистанция: {clipData.MinDistance}\n" +
                                       $"Макс. дистанция: {clipData.MaxDistance}\n" +
                                       $"Фактор затухания: {clipData.RolloffFactor}";
                }
            }
        }
        #endregion

        #region Обработка пространственного звука
        private void Update()
        {
            if (soundSource != null && spatialSource != null && spatialSource.IsPlaying)
            {
                // Вращаем источник звука по кругу
                spatialAngle += sourceMovementSpeed * Time.deltaTime;
                float x = Mathf.Cos(spatialAngle) * sourceMovementRadius;
                float z = Mathf.Sin(spatialAngle) * sourceMovementRadius;
                soundSource.position = new Vector3(x, 0f, z);

                // Обновляем позицию слушателя, если он есть и WebGL система инициализирована
                if (listener != null && WebGLAudioBridge.IsInitialized)
                {
                    WebGLAudioBridge.SetListenerPosition(listener.position);
                    var f = listener.forward;
                    var u = listener.up;
                    WebGLAudioBridge.SetListenerOrientation(f.x, f.y, f.z, u.x, u.y, u.z);
                }

                // Обновляем информацию о позиции
                if (spatialInfoText != null)
                {
                    float distance = Vector3.Distance(soundSource.position, listener != null ? listener.position : Vector3.zero);
                    spatialInfoText.text = $"Пространственный звук:\n" +
                                         $"Позиция источника: {soundSource.position:F1}\n" +
                                         $"Расстояние до слушателя: {distance:F1}м";
                }
            }
        }
        #endregion

        #region Обработка слайдера времени
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == timeSlider?.gameObject)
            {
                isDraggingSlider = true;
                if (musicSource != null)
                {
                    var unitySource = musicSource.GetComponent<AudioSource>();
                    if (unitySource != null && unitySource.clip != null)
                    {
                        savedTime = unitySource.time;
                    }
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == timeSlider?.gameObject)
            {
                isDraggingSlider = false;
            if (timeSlider != null)
                {
                    OnTimeSliderChanged(timeSlider.value);
                }
            }
        }

        private void OnTimeSliderChanged(float normalizedTime)
        {
            if (musicSource != null)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    try
                    {
                        normalizedTime = Mathf.Clamp01(normalizedTime);
                        float newTime = normalizedTime * unitySource.clip.length;
                        
                        // Если звук воспроизводится, устанавливаем время напрямую
                        if (musicSource.IsPlaying)
                        {
                            unitySource.time = newTime;
                        }
                        else
                        {
                            // Если звук на паузе, сохраняем время для последующего воспроизведения
                            savedTime = newTime;
                        }
                        
                        // Обновляем UI времени
                        UpdateTimeText(newTime, unitySource.clip.length);
                        
                        // Для WebGL также обновляем время через WebGLAudioBridge
                        #if UNITY_WEBGL && !UNITY_EDITOR
                        if (WebGLAudioBridge.IsInitialized && !string.IsNullOrEmpty(musicSource.CurrentKey))
                        {
                            WebGLAudioBridge.SetTime(musicSource.CurrentKey, newTime);
                        }
                        #endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Не удалось установить время воспроизведения: {e.Message}");
                    }
                }
            }
        }
        #endregion

        private void OnSoundSelected(int idx)
        {
            var allKeys = database.GetAllKeys().ToList();
            if (idx >= 0 && idx < allKeys.Count)
            {
                currentMusicKey = allKeys[idx];
            }
        }

        private void OnTagSelected(int idx)
        {
            var allTags = database.GetAllTags().ToList();
            if (idx > 0 && idx <= allTags.Count)
            {
                string selectedTag = allTags[idx - 1];
                var filteredKeys = database.GetClipsByTag(selectedTag).Select(c => c.Key).ToList();
                soundsDropdown.ClearOptions();
                soundsDropdown.AddOptions(filteredKeys);
                currentMusicKey = filteredKeys.Count > 0 ? filteredKeys[0] : null;
            }
            else if (idx == 0) // "Все звуки"
            {
                var allKeys = database.GetAllKeys().ToList();
                soundsDropdown.ClearOptions();
                soundsDropdown.AddOptions(allKeys);
                currentMusicKey = allKeys.Count > 0 ? allKeys[0] : null;
            }
        }

        private void OnDestroy()
        {
            // Отписываемся от событий модуля
            if (audioModule != null)
            {
                audioModule.OnSoundLoaded -= OnSoundLoaded;
                audioModule.OnSoundFailed -= OnSoundFailed;
                audioModule.OnSoundPlayed -= OnSoundPlayed;
                audioModule.OnSoundStopped -= OnSoundStopped;
                audioModule.OnMainTrackStarted -= OnMainTrackStarted;
                audioModule.OnMainTrackStopped -= OnMainTrackStopped;
                audioModule.OnOneShotPlayed -= OnOneShotPlayed;
                audioModule.OnVolumeChanged -= OnGlobalVolumeChanged;
            }

            // Отписываемся от событий WebGL Audio Bridge
            WebGLAudioBridge.OnSoundLoaded -= OnWebGLSoundLoaded;
            WebGLAudioBridge.OnSoundError -= OnWebGLSoundError;
            WebGLAudioBridge.OnSoundComplete -= OnWebGLSoundComplete;
            WebGLAudioBridge.OnMuteChanged -= OnWebGLMuteChanged;

            // Останавливаем и очищаем основные источники
            if (musicSource != null)
            {
                musicSource.OnFinished -= OnMusicFinished;
                musicSource.OnLoaded -= OnSourceLoaded;
                musicSource.OnError -= OnSourceError;
                musicSource.Stop();
                Destroy(musicSource.gameObject);
            }

            if (effectSource != null)
            {
                effectSource.OnFinished -= OnEffectFinished;
                effectSource.OnLoaded -= OnSourceLoaded;
                effectSource.OnError -= OnSourceError;
                effectSource.Stop();
                Destroy(effectSource.gameObject);
            }

            if (spatialSource != null)
            {
                spatialSource.OnFinished -= OnSpatialSoundFinished;
                spatialSource.OnLoaded -= OnSourceLoaded;
                spatialSource.OnError -= OnSourceError;
                spatialSource.Stop();
                Destroy(spatialSource.gameObject);
            }

            // Останавливаем таймеры
            if (IsInvoking(nameof(UpdateTimeUI)))
                CancelInvoke(nameof(UpdateTimeUI));
            if (IsInvoking(nameof(UpdateCacheInfo)))
                CancelInvoke(nameof(UpdateCacheInfo));
            if (IsInvoking(nameof(UpdateActiveSourcesInfo)))
                CancelInvoke(nameof(UpdateActiveSourcesInfo));

            // Очищаем все оставшиеся источники звука
            var allSources = FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
            foreach (var source in allSources)
            {
                if (source != null)
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }
        }

        #region Обработчики событий WebGL
        private void OnWebGLSoundLoaded(string key)
        {
            ShowMessage($"WebGL: Звук {key} загружен");
        }

        private void OnWebGLSoundError(string key, string error)
        {
            ShowMessage($"WebGL: Ошибка загрузки {key}: {error}", true);
        }

        private void OnWebGLSoundComplete(string key)
        {
            ShowMessage($"WebGL: Звук {key} завершен");
            // Обновляем UI времени при завершении звука
            if (musicSource != null && musicSource.CurrentKey == key)
            {
                savedTime = 0f;
            }
        }

        private void OnWebGLMuteChanged(bool isMuted)
        {
            if (muteToggle != null)
            {
                muteToggle.isOn = isMuted;
            }
        }
        #endregion
    }
}
