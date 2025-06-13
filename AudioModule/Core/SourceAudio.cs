using UnityEngine;
using System;
using System.Threading.Tasks;
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
        public bool dontDestroyOnLoad = false;
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
        private const float MIN_PITCH = 0.5f;
        private const float MAX_PITCH = 2f;
        private const float DEFAULT_VOLUME = 1f;
        private const float DEFAULT_PITCH = 1f;
        private const float DEFAULT_FADE_TIME = 1f;

        [SerializeField] private AudioProviderType providerType = AudioProviderType.Unity;
        [SerializeField] private AudioSource unityAudioSource;
        
        private bool isInitialized;
        private string currentKey;
        private float currentTime;
        private bool preserveOnSceneLoad = false;
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
            if (isInitialized) return;

            try
            {
                // Создаем AudioSource если используется Unity провайдер и он не назначен
                if (providerType == AudioProviderType.Unity && unityAudioSource == null)
                {
                    unityAudioSource = gameObject.AddComponent<AudioSource>();
                    unityAudioSource.playOnAwake = false;
                }

                // Регистрируем источник в AudioManager
                AudioManager.Instance.RegisterSource(this);
                
                // Подписываемся на события WebGL
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebGLAudioBridge.OnSoundLoaded += HandleSoundLoaded;
                WebGLAudioBridge.OnSoundError += HandleSoundError;
                WebGLAudioBridge.OnSoundComplete += HandleSoundComplete;
                WebGLAudioBridge.OnMuteChanged += HandleMuteChanged;
                #endif

                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка инициализации SourceAudio: {ex}");
            }
        }

        public async Task PlayWithParameters(string key, PlaySoundParameters parameters = null)
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
                preserveOnSceneLoad = parameters.dontDestroyOnLoad;
                IsSpatial = parameters.spatial;

                if (preserveOnSceneLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }

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
                        await PlayUnity(key);
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

        public async Task Play(string key)
        {
            await PlayWithParameters(key, new PlaySoundParameters());
        }

        public async Task PlayFromTime(string key, float startTime)
        {
            await PlayWithParameters(key, new PlaySoundParameters { startTime = startTime });
        }

        public async Task PlayLooped(string key, bool dontDestroyOnLoad = false)
        {
            await PlayWithParameters(key, new PlaySoundParameters 
            { 
                loop = true, 
                dontDestroyOnLoad = dontDestroyOnLoad 
            });
        }

        public async Task PlayOneShot(string key)
        {
            if (providerType != AudioProviderType.Unity)
            {
                Debug.LogWarning("PlayOneShot доступен только для Unity провайдера");
                return;
            }

            try
            {
                var clip = await AudioModule.Instance.LoadAudioClipAsync(key);
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

        private async Task PlayUnity(string key)
        {
            try
            {
                var clip = await AudioModule.Instance.LoadAudioClipAsync(key);
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
            return baseVolume;
        }

        private void SetVolume(float value)
        {
            value = Mathf.Clamp(value, MIN_VOLUME, MAX_VOLUME);
            baseVolume = value;
            
            float actualVolume = value * AudioManager.Instance.GetVolume();
            
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.volume = actualVolume;
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
                return unityAudioSource != null && unityAudioSource.mute;
            return false;
        }

        private void SetMute(bool value)
        {
            if (providerType == AudioProviderType.Unity)
            {
                if (unityAudioSource != null)
                    unityAudioSource.mute = value;
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
            if (preserveOnSceneLoad && !string.IsNullOrEmpty(currentKey))
            {
                var parameters = new PlaySoundParameters
                {
                    startTime = currentTime,
                    loop = Loop,
                    volume = Volume,
                    pitch = Pitch,
                    dontDestroyOnLoad = true,
                    spatial = IsSpatial
                };
                
                PlayWithParameters(currentKey, parameters).ConfigureAwait(false);
            }
        }

        private void OnDisable()
        {
            if (!preserveOnSceneLoad)
            {
                Stop();
            }
            else
            {
                currentTime = Time;
            }

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.UnregisterSource(this);
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
                WebGLAudioBridge.SetLoop(currentKey, value);
                #endif
            }
        }
    }
} 
