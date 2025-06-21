using UnityEngine;
using System;
using System.Collections;
using System.Linq;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Тип провайдера для проигрывания звуков
    /// </summary>
    public enum AudioProviderType
    {
        Unity,  // Стандартный Unity AudioSource
        WebGL   // WebGL провайдер
    }

    /// <summary>
    /// Параметры воспроизведения звука
    /// </summary>
    public class PlaySoundParameters
    {
        public float startTime = 0f;
        public bool loop = false;
        public float volume = -1f;
        public float pitch = -1f;
        public bool spatial = false;
    }

    /// <summary>
    /// Компонент для проигрывания звуков с поддержкой различных провайдеров
    /// </summary>
    [AddComponentMenu("FoundersKit/Audio/Source Audio")]
    public class SourceAudio : MonoBehaviour
    {
        // Константы
        private const float MIN_VOLUME = 0f;
        private const float MAX_VOLUME = 1f;
        public const float MIN_PITCH = 0.5f;
        public const float MAX_PITCH = 2f;
        private const float DEFAULT_VOLUME = 1f;
        private const float DEFAULT_PITCH = 1f;
        private const float DEFAULT_FADE_TIME = 1f;

        [SerializeField] private AudioProviderType providerType = AudioProviderType.Unity;
        [SerializeField] private AudioSource unityAudioSource;
        
        private bool isInitialized;
        private string currentKey;
        private float currentTime;
        private float baseVolume = DEFAULT_VOLUME;
        private float basePitch = DEFAULT_PITCH;
        private int priority = 0;
        private bool wasPlaying = false;
        private bool isSpatial;
        private Vector3 lastPosition;
        private bool isPositionDirty;
        private Coroutine fadeCoroutine;
        private float targetVolume;
        private float currentFadeTime;
        private bool isFading;

        // События
        public event Action OnFinished;
        public event Action<string> OnLoaded;
        public event Action<string, string> OnError;
        public event Action OnPlay;
        public event Action OnStop;
        public event Action<bool> OnMute;
        public event Action<bool> OnFadeComplete;

        // Свойства
        public string CurrentKey => currentKey;
        public float Volume 
        { 
            get => GetVolume();
            set => SetVolume(value);
        }
        public float Pitch
        {
            get => GetPitch();
            set => SetPitch(value);
        }
        public bool Mute
        {
            get => GetMute();
            set => SetMute(value);
        }
        public bool IsPlaying => GetIsPlaying();
        public bool Loop
        {
            get => GetLoop();
            set => SetLoop(value);
        }
        public float Time
        {
            get => GetTime();
            set => SetTime(value);
        }
        public bool IsSpatial
        {
            get => isSpatial;
            set
            {
                if (isSpatial != value)
                {
                    isSpatial = value;
                    if (providerType == AudioProviderType.Unity)
                    {
                        if (unityAudioSource != null)
                        {
                            unityAudioSource.spatialBlend = value ? 1f : 0f;
                        }
                    }
                }
            }
        }
        public bool IsFading => isFading;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!isInitialized)
            {
                if (unityAudioSource == null)
                {
                    unityAudioSource = gameObject.AddComponent<AudioSource>();
                }

                // Регистрируем источник в AudioModule
                AudioModule.Instance.RegisterSource(this);
                
                // Подписываемся на события WebGL
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebGLAudioBridge.OnSoundLoaded += HandleSoundLoaded;
                WebGLAudioBridge.OnSoundError += HandleSoundError;
                WebGLAudioBridge.OnSoundComplete += HandleSoundComplete;
                WebGLAudioBridge.OnMuteChanged += HandleMuteChanged;
                #endif

                isInitialized = true;
            }
        }

        public void PlayWithParameters(string key, PlaySoundParameters parameters = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                OnError?.Invoke(key, "Ключ звука не может быть пустым");
                return;
            }

            try
            {
                if (parameters == null)
                    parameters = new PlaySoundParameters();

                currentKey = key;
                currentTime = parameters.startTime;

                IsSpatial = parameters.spatial;

                // Загружаем настройки из базы данных
                var database = Resources.Load<AudioDatabase>("AudioDatabase");
                if (database != null)
                {
                    var clipData = database.GetClipData(key);
                    if (clipData != null)
                    {
                        baseVolume = parameters.volume > 0 ? parameters.volume : clipData.DefaultVolume;
                        SetVolume(baseVolume);
                        SetPitch(parameters.pitch > 0 ? parameters.pitch : clipData.Pitch);
                        SetLoop(parameters.loop || clipData.Loop);
                        priority = clipData.LoadPriority;
                        IsSpatial = parameters.spatial || clipData.Spatial;
                    }
                }

                #if UNITY_WEBGL && !UNITY_EDITOR
                if (!WebGLAudioBridge.IsUserInteracted())
                {
                    Debug.LogWarning($"Звук {key} отложен до взаимодействия пользователя");
                    return;
                }
                #endif

                switch (providerType)
                {
                    case AudioProviderType.Unity:
                        PlayUnity(key);
                        break;
                    case AudioProviderType.WebGL:
                        PlayWebGL(key);
                        break;
                }

                wasPlaying = true;
                OnPlay?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка воспроизведения звука {key}: {ex.Message}");
                OnError?.Invoke(key, ex.Message);
            }
        }

        public void Play(string key)
        {
            PlayWithParameters(key, new PlaySoundParameters());
        }

        public void PlayFromTime(string key, float startTime)
        {
            PlayWithParameters(key, new PlaySoundParameters { startTime = startTime });
        }

        public void PlayLooped(string key)
        {
            PlayWithParameters(key, new PlaySoundParameters 
            { 
                loop = true
            });
        }

        public void PlayOneShot(string key)
        {
            if (providerType != AudioProviderType.Unity)
            {
                Debug.LogWarning("PlayOneShot доступен только для Unity провайдера");
                return;
            }

            try
            {
                var clip = AudioModule.Instance.LoadAudioClip(key);
                if (clip != null && unityAudioSource != null)
                {
                    unityAudioSource.PlayOneShot(clip);
                    OnPlay?.Invoke();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(key, ex.Message);
                Debug.LogError($"Ошибка при проигрывании звука {key}: {ex}");
            }
        }

        private void PlayUnity(string key)
        {
            try
            {
                var clip = AudioModule.Instance.LoadAudioClip(key);
                if (clip != null && unityAudioSource != null)
                {
                    unityAudioSource.clip = clip;
                    unityAudioSource.time = Mathf.Clamp(currentTime, 0f, clip.length);
                    unityAudioSource.Play();
                    
                    if (!unityAudioSource.loop)
                    {
                        StartCoroutine(WaitForSoundFinish());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при проигрывании Unity звука: {ex.Message}");
            }
        }

        private void PlayWebGL(string key)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Ключ звука не может быть пустым");
                }

                if (!WebGLAudioBridge.IsInitialized)
                {
                    Debug.LogWarning($"WebGLAudioBridge не инициализирован. Пропускаем воспроизведение звука {key}");
                    return;
                }

                var clip = AudioModule.Instance.LoadAudioClip(key);
                if (clip == null)
                {
                    throw new Exception($"Не удалось загрузить аудио клип для ключа {key}");
                }

                WebGLAudioBridge.PlaySound(key, currentTime, baseVolume, basePitch, priority, isSpatial ? transform.position : (Vector3?)null);
                wasPlaying = true;
                OnPlay?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при проигрывании WebGL звука {key}: {ex.Message}");
                OnError?.Invoke(key, ex.Message);
            }
            #endif
        }

        public void Stop()
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.Stop();
            }
            wasPlaying = false;
            OnStop?.Invoke();
        }

        public void Pause()
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.Pause();
            }
        }

        public void UnPause()
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.UnPause();
            }
        }

        private float GetVolume()
        {
            if (providerType == AudioProviderType.Unity)
            {
                return unityAudioSource != null ? unityAudioSource.volume : 0f;
            }
            return baseVolume;
        }

        private void SetVolume(float value)
        {
            value = Mathf.Clamp(value, MIN_VOLUME, MAX_VOLUME);
            baseVolume = value;
            
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                {
                    unityAudioSource.volume = value;
                }
            }
        }

        private float GetPitch()
        {
            return basePitch;
        }

        private void SetPitch(float value)
        {
            value = Mathf.Clamp(value, MIN_PITCH, MAX_PITCH);
            basePitch = value;
            
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.pitch = value;
            }
        }

        private bool GetMute()
        {
            if (providerType == AudioProviderType.Unity)
            {
                return unityAudioSource != null && unityAudioSource.mute;
            }
            return false;
        }

        private void SetMute(bool value)
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                {
                    unityAudioSource.mute = value;
                }
            }
            OnMute?.Invoke(value);
        }

        private bool GetIsPlaying()
        {
            if (providerType == AudioProviderType.Unity)
                return unityAudioSource != null && unityAudioSource.isPlaying;
            return wasPlaying;
        }

        private bool GetLoop()
        {
            if (providerType == AudioProviderType.Unity)
                return unityAudioSource != null && unityAudioSource.loop;
            return false;
        }

        private float GetTime()
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null && unityAudioSource.clip != null)
                {
                    return Mathf.Clamp(unityAudioSource.time, 0f, unityAudioSource.clip.length);
                }
                return 0f;
            }
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLAudioBridge.IsInitialized)
            {
                Debug.LogWarning("WebGLAudioBridge не инициализирован. Возвращаем 0 для времени воспроизведения");
                return 0f;
            }
            return WebGLAudioBridge.GetTime(currentKey);
            #else
            return 0f;
            #endif
        }

        private void SetTime(float value)
        {
            try
            {
                if (providerType == AudioProviderType.Unity)
                {
                    if (unityAudioSource != null && unityAudioSource.clip != null)
                    {
                        float clampedTime = Mathf.Clamp(value, 0f, unityAudioSource.clip.length);
                        if (clampedTime != value)
                        {
                            Debug.LogWarning($"Время воспроизведения {value} выходит за пределы длины клипа {unityAudioSource.clip.length}. Установлено значение {clampedTime}");
                        }
                        unityAudioSource.time = clampedTime;
                    }
                    else
                    {
                        Debug.LogWarning("Невозможно установить время воспроизведения: аудио клип не загружен");
                    }
                }
                else
                {
                    #if UNITY_WEBGL && !UNITY_EDITOR
                    if (!WebGLAudioBridge.IsInitialized)
                    {
                        Debug.LogWarning("WebGLAudioBridge не инициализирован. Пропускаем установку времени воспроизведения");
                        return;
                    }
                    if (value >= 0)
                    {
                        WebGLAudioBridge.SetTime(currentKey, value);
                    }
                    else
                    {
                        Debug.LogWarning($"Невозможно установить отрицательное время воспроизведения: {value}");
                    }
                    #endif
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при установке времени воспроизведения: {ex.Message}");
            }
        }

        private IEnumerator WaitForSoundFinish()
        {
            while (unityAudioSource != null && unityAudioSource.isPlaying)
            {
                yield return null;
            }
            wasPlaying = false;
            OnFinished?.Invoke();
        }

        private void HandleSoundLoaded(string key)
        {
            if (key == currentKey)
                OnLoaded?.Invoke(key);
        }

        private void HandleSoundError(string key, string error)
        {
            if (key == currentKey)
                OnError?.Invoke(key, error);
        }

        private void HandleSoundComplete(string key)
        {
            if (key == currentKey)
            {
                wasPlaying = false;
                OnFinished?.Invoke();
            }
        }

        private void HandleMuteChanged(bool muted)
        {
            OnMute?.Invoke(muted);
        }

        private void OnEnable()
        {
            // Логика восстановления воспроизведения при необходимости
        }

        private void OnDisable()
        {
            Stop();

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            Stop();
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.OnSoundLoaded -= HandleSoundLoaded;
            WebGLAudioBridge.OnSoundError -= HandleSoundError;
            WebGLAudioBridge.OnSoundComplete -= HandleSoundComplete;
            WebGLAudioBridge.OnMuteChanged -= HandleMuteChanged;
            #endif

            if (AudioModule.Instance != null)
            {
                AudioModule.Instance.UnregisterSource(this);
            }
        }

        public static bool IsKeyPlayingAnywhere(string key)
        {
            var sources = UnityEngine.Object.FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
            return sources.Any(source => source.IsPlayingKey(key));
        }

        public static SourceAudio GetSourcePlayingKey(string key)
        {
            var sources = UnityEngine.Object.FindObjectsByType<SourceAudio>(FindObjectsSortMode.None);
            return sources.FirstOrDefault(source => source.IsPlayingKey(key));
        }

        public bool IsPlayingKey(string key)
        {
            return IsPlaying && currentKey == key;
        }

        private void Update()
        {
            if (IsPlaying && isSpatial && transform.position != lastPosition)
            {
                lastPosition = transform.position;
                isPositionDirty = true;
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (!isPositionDirty) return;

            if (providerType == AudioProviderType.Unity)
            {
                // Unity уже обрабатывает позицию через transform
            }
            else
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (!WebGLAudioBridge.IsInitialized)
                {
                    Debug.LogWarning("WebGLAudioBridge не инициализирован. Пропускаем обновление позиции");
                    return;
                }
                WebGLAudioBridge.SetPosition(currentKey, transform.position);
                #endif
            }

            isPositionDirty = false;
        }

        public void FadeIn(float duration = DEFAULT_FADE_TIME)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            targetVolume = baseVolume;
            fadeCoroutine = StartCoroutine(FadeCoroutine(0f, baseVolume, duration));
        }

        public void FadeOut(float duration = DEFAULT_FADE_TIME)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            targetVolume = 0f;
            fadeCoroutine = StartCoroutine(FadeCoroutine(baseVolume, 0f, duration));
        }

        private IEnumerator FadeCoroutine(float startVolume, float endVolume, float duration)
        {
            isFading = true;
            float elapsed = 0f;
            float startValue = startVolume;
            
            while (elapsed < duration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                float normalizedTime = elapsed / duration;
                float currentVolume = Mathf.Lerp(startValue, endVolume, normalizedTime);
                
                if (providerType == AudioProviderType.Unity && unityAudioSource != null)
                {
                    unityAudioSource.volume = currentVolume;
                }
                
                yield return null;
            }

            if (endVolume == 0f && IsPlaying)
            {
                Stop();
            }
            
            isFading = false;
            fadeCoroutine = null;
            OnFadeComplete?.Invoke(endVolume > 0);
        }

        private void UnPauseJS()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLAudioBridge.IsInitialized)
            {
                Debug.LogWarning("WebGLAudioBridge не инициализирован. Пропускаем возобновление воспроизведения");
                return;
            }
            WebGLAudioBridge.UnpauseSound(currentKey);
            #endif
        }

        private void SetLoop(bool value)
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.loop = value;
            }
            else
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (!WebGLAudioBridge.IsInitialized)
                {
                    Debug.LogWarning("WebGLAudioBridge не инициализирован. Пропускаем установку зацикливания");
                    return;
                }
                WebGLAudioBridge.SetLoop(currentKey, value);
                #endif
            }
        }
    }
} 
