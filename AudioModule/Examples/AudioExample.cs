using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System;

namespace FoundersPlugin.Modules.Examples
{
    /// <summary>
    /// Пример использования аудио модуля
    /// </summary>
    public class AudioExample : MonoBehaviour
    {
        [Header("Звуки")]
        [AudioKey] public string backgroundMusic;
        [AudioKey] public string buttonClick;
        [AudioKey] public string notification;
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

        [Header("Пространственный звук")]
        [SerializeField] private Transform soundSource; // Движущийся источник звука
        [SerializeField] private Transform listener; // Слушатель (камера)
        [SerializeField] private float sourceMovementRadius = 5f; // Радиус движения источника
        [SerializeField] private float sourceMovementSpeed = 1f; // Скорость движения источника
        [SerializeField] private TextMeshProUGUI spatialInfoText; // Информация о позиции
        [SerializeField] private Button toggleSpatialButton; // Кнопка для включения/выключения
        [SerializeField] private TextMeshProUGUI spatialButtonText; // Текст на кнопке

        // Константы для pitch
        private const float MIN_PITCH = 0.5f;
        private const float MAX_PITCH = 2f;
        private const float DEFAULT_PITCH = 1f;

        private SourceAudio musicSource;
        private SourceAudio effectSource;
        private SourceAudio spatialSource; // Источник для пространственного звука
        private AudioDatabase database;
        private string currentMusicKey;
        private bool isUpdatingSlider;
        private bool isDraggingSlider;
        private TimeSliderHandler timeSliderHandler;
        private float currentClipLength;
        private float spatialAngle; // Угол для движения источника звука
        private float savedTime; // Добавляем переменную для сохранения времени

        private void Start()
        {
            // Инициализация источников звука
            musicSource = gameObject.AddComponent<SourceAudio>();
            effectSource = gameObject.AddComponent<SourceAudio>();
            
            // Создаем объект для пространственного звука
            if (soundSource != null)
            {
                spatialSource = soundSource.gameObject.AddComponent<SourceAudio>();
            }

            // Загрузка базы данных
            database = Resources.Load<AudioDatabase>("AudioDatabase");

            // Настройка UI
            SetupUI();

            // Подписка на события
            SubscribeToEvents();

            // Настройка слайдера времени
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = 1f;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
                
                // Добавляем обработчик событий для слайдера
                timeSliderHandler = timeSlider.gameObject.AddComponent<TimeSliderHandler>();
                timeSliderHandler.Initialize(this);
            }

            // Запускаем обновление UI
            InvokeRepeating(nameof(UpdateTimeUI), 0.1f, 0.1f);
        }

        private async void StartSpatialDemo()
        {
            if (spatialSource != null && !string.IsNullOrEmpty(spatialSound))
            {
                var parameters = new PlaySoundParameters
                {
                    loop = true,
                    spatial = true,
                    volume = 1f
                };

                await spatialSource.PlayWithParameters(spatialSound, parameters);
            }
        }

        private void Update()
        {
            if (soundSource != null && spatialSource != null && spatialSource.IsPlaying)
            {
                // Вращаем источник звука по кругу
                spatialAngle += sourceMovementSpeed * Time.deltaTime;
                float x = Mathf.Cos(spatialAngle) * sourceMovementRadius;
                float z = Mathf.Sin(spatialAngle) * sourceMovementRadius;
                soundSource.position = new Vector3(x, 0f, z);

                // Обновляем позицию слушателя, если он есть
                if (listener != null)
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

        private void SetupUI()
        {
            // Настройка слайдера громкости
            if (volumeSlider != null)
            {
                volumeSlider.value = 1f;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeText(1f);
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

            // Обновление информации о кэше
            UpdateCacheInfo();

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
            }

            if (effectSource != null)
            {
                effectSource.OnFinished += OnEffectFinished;
            }
        }

        private void OnVolumeChanged(float value)
        {
            if (musicSource != null)
                musicSource.Volume = value;
            UpdateVolumeText(value);
        }

        private void OnPitchChanged(float value)
        {
            if (musicSource != null)
                musicSource.Pitch = value;
            UpdatePitchText(value);
        }

        private void OnMuteChanged(bool value)
        {
            if (musicSource != null)
                musicSource.Mute = value;
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

        private void UpdateTimeUI()
        {
            if (musicSource == null)
            {
                if (timeSlider != null) timeSlider.value = 0f;
                if (currentTimeText != null) currentTimeText.text = "0:00";
                if (totalTimeText != null) totalTimeText.text = "0:00";
                return;
            }

            var unitySource = musicSource.GetComponent<AudioSource>();
            if (unitySource != null && unitySource.clip != null && !isDraggingSlider)
            {
                float currentTime;
                float totalTime = unitySource.clip.length;

                // Используем сохраненное время при паузе
                if (!musicSource.IsPlaying && savedTime > 0)
                {
                    currentTime = savedTime;
                }
                else
                {
                    currentTime = unitySource.time;
                }
                
                // Обновляем слайдер нормализованным значением
                if (timeSlider != null)
                {
                    timeSlider.value = totalTime > 0 ? currentTime / totalTime : 0f;
                }
                
                // Обновляем текст времени
                UpdateTimeText(currentTime, totalTime);
            }
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

        public void OnTimeSliderBeginDrag()
        {
            isDraggingSlider = true;
        }

        public void OnTimeSliderEndDrag()
        {
            isDraggingSlider = false;
            if (musicSource != null && musicSource.IsPlaying)
            {
                float normalizedTime = timeSlider.value;
                SetCurrentTime(normalizedTime);
            }
        }

        private void SetCurrentTime(float normalizedTime)
        {
            if (musicSource != null)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    float clipTime = Mathf.Clamp01(normalizedTime) * unitySource.clip.length;
                    musicSource.Time = clipTime;
                }
            }
        }

        private void OnTimeSliderChanged(float normalizedValue)
        {
            if (isDraggingSlider)
            {
                // Обновляем только отображение времени во время перетаскивания
                if (musicSource != null)
                {
                    var unitySource = musicSource.GetComponent<AudioSource>();
                    if (unitySource != null && unitySource.clip != null)
                    {
                        float currentTime = normalizedValue * unitySource.clip.length;
                        float totalTime = unitySource.clip.length;
                        UpdateTimeText(currentTime, totalTime);
                    }
                }
            }
        }

        private void JumpToStart()
        {
            if (musicSource != null)
            {
                musicSource.Time = 0f;
                if (timeText != null)
                {
                    timeText.text = FormatTime(0f);
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
                    float middleTime = unitySource.clip.length / 2;
                    musicSource.Time = middleTime;
                    if (timeText != null)
                    {
                        timeText.text = FormatTime(middleTime);
                    }
                }
            }
        }

        private async void PlaySelectedSound()
        {
            if (soundsDropdown != null && musicSource != null)
            {
                string selectedSound = soundsDropdown.options[soundsDropdown.value].text;
                currentMusicKey = selectedSound;
                await musicSource.Play(selectedSound);
                
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null && unitySource.clip != null)
                {
                    currentClipLength = unitySource.clip.length;
                }
                
                UpdateSoundInfo(selectedSound);
            }
        }

        private void StopSelectedSound()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        private void PauseSelectedSound()
        {
            if (musicSource != null)
            {
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null)
                {
                    savedTime = unitySource.time;
                }
                musicSource.Pause();
                
                // Принудительно обновляем UI после паузы
                UpdateTimeUI();
            }
        }

        private void UnpauseSelectedSound()
        {
            if (musicSource != null)
            {
                musicSource.UnPause();
                
                var unitySource = musicSource.GetComponent<AudioSource>();
                if (unitySource != null)
                {
                    unitySource.time = savedTime;
                }
                
                // Принудительно обновляем UI после снятия с паузы
                UpdateTimeUI();
            }
        }

        private async void PlayEffect()
        {
            if (effectSource != null && !string.IsNullOrEmpty(buttonClick))
            {
                await effectSource.PlayOneShot(buttonClick);
            }
        }

        private void OnMusicFinished()
        {
            ShowMessage("Музыка завершила воспроизведение");
        }

        private void OnEffectFinished()
        {
            ShowMessage("Эффект завершил воспроизведение");
        }

        private void ShowMessage(string message)
        {
            if (messageText != null)
                messageText.text = message;
        }

        private void OnSoundSelected(int index)
        {
            if (soundsDropdown != null)
            {
                string selectedSound = soundsDropdown.options[index].text;
                UpdateSoundInfo(selectedSound);
            }
        }

        private void OnTagSelected(int index)
        {
            if (tagsDropdown != null && soundsDropdown != null && database != null)
            {
                soundsDropdown.ClearOptions();
                var options = index == 0 
                    ? database.GetAllKeys().ToList()
                    : database.GetClipsByTag(tagsDropdown.options[index].text)
                            .Select(c => c.Key)
                            .ToList();
                soundsDropdown.AddOptions(options);
                
                if (options.Count > 0)
                {
                    soundsDropdown.value = 0;
                    UpdateSoundInfo(options[0]);
                }
            }
        }

        private void UpdateSoundInfo(string key)
        {
            if (soundInfoText != null && database != null)
            {
                var clipData = database.GetClipData(key);
                if (clipData != null)
                {
                    string tags = string.Join(", ", database.GetClipTags(key));
                    soundInfoText.text = $"Информация о звуке:\n" +
                                       $"Ключ: {clipData.Key}\n" +
                                       $"Путь: {clipData.Path}\n" +
                                       $"Предзагрузка: {clipData.PreloadOnStart}\n" +
                                       $"Громкость по умолчанию: {clipData.DefaultVolume}\n" +
                                       $"Зацикленность: {clipData.Loop}\n" +
                                       $"Pitch: {clipData.Pitch}\n" +
                                       $"Теги: {tags}\n" +
                                       $"Приоритет загрузки: {clipData.LoadPriority}";
                }
            }
        }

        private void UpdateCacheInfo()
        {
            if (cacheInfoText != null)
            {
                var info = AudioModule.Instance.GetCacheInfo();
                cacheInfoText.text = $"Информация о кэше:\n" +
                                   $"Количество звуков: {info.count}\n" +
                                   $"Использование памяти: {info.memoryUsageMB:F2} MB";
            }
        }

        private void ToggleSpatialSound()
        {
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

        private void FadeInMusic()
        {
            if (musicSource != null)
            {
                if (!musicSource.IsPlaying)
                {
                    PlaySelectedSound();
                }
                musicSource.FadeIn(2f); // Фейд за 2 секунды
                ShowMessage("Начат фейд-ин музыки");
            }
        }

        private void FadeOutMusic()
        {
            if (musicSource != null && musicSource.IsPlaying)
            {
                musicSource.FadeOut(1.5f); // Фейд за 1.5 секунды
                ShowMessage("Начат фейд-аут музыки");
            }
        }

        private void OnDestroy()
        {
            if (timeSlider != null)
            {
                timeSlider.onValueChanged.RemoveListener(OnTimeSliderChanged);
                if (timeSliderHandler != null)
                {
                    Destroy(timeSliderHandler);
                }
            }

            if (musicSource != null)
            {
                musicSource.OnFinished -= OnMusicFinished;
            }

            if (effectSource != null)
            {
                effectSource.OnFinished -= OnEffectFinished;
            }

            if (IsInvoking(nameof(UpdateTimeUI)))
            {
                CancelInvoke(nameof(UpdateTimeUI));
            }

            if (spatialSource != null)
            {
                spatialSource.Stop();
            }

            if (toggleSpatialButton != null)
            {
                toggleSpatialButton.onClick.RemoveListener(ToggleSpatialSound);
            }

            if (fadeInButton != null)
                fadeInButton.onClick.RemoveListener(FadeInMusic);
            if (fadeOutButton != null)
                fadeOutButton.onClick.RemoveListener(FadeOutMusic);
        }
    }

    /// <summary>
    /// Обработчик событий для слайдера времени
    /// </summary>
    public class TimeSliderHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private AudioExample audioExample;

        public void Initialize(AudioExample example)
        {
            audioExample = example;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            audioExample?.OnTimeSliderBeginDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            audioExample?.OnTimeSliderEndDrag();
        }
    }
} 