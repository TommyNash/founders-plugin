// Assets/FoundersPlugin/Scripts/YandexAds/AdsModule.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using FoundersPlugin.Core;
using FoundersPlugin.Interfaces;
using System.Runtime.InteropServices;

namespace FoundersPlugin.Modules
{
    public class AdsModule : MonoBehaviour, IAdsModule
    {
        // Внешние методы JavaScript для работы с рекламой в Yandex Games
#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void ShowFullscreenAd_js();

        [DllImport("__Internal")]
        private static extern void ShowRewardedVideo_js(int id);
#else
        // Заглушки для редактора
        private static void ShowFullscreenAd_js() 
        {
            Debug.Log("[MOCK] Показ полноэкранной рекламы");
        }
        
        private static void ShowRewardedVideo_js(int id) 
        {
            Debug.Log($"[MOCK] Показ рекламы с вознаграждением, id: {id}");
        }
#endif

        // Реализация IModule
        public string ModuleName => "Yandex Ads";
        public string ModuleVersion => "1.0";
        public string ModuleDescription => "Модуль рекламы Яндекс.Игр";
        public bool IsEnabled { get; set; } = true;
        public string GitHubRepository => "your-repo";
        public string GitHubBranch => "main";

        // Singleton для статического доступа
        private static AdsModule _instance;
        public static AdsModule Instance
        {
            get
            {
                if (_instance == null)
                {
                    //_instance = // YandexGameManager.GetAdsModule();
                    if (_instance == null)
                    {
                        Debug.LogWarning("AdsModule не инициализирован. Используйте // YandexGameManager для инициализации модуля.");
                    }
                }
                return _instance;
            }
        }

        // События для видео и полноэкранной рекламы
        public static event Action OpenFullAdEvent;
        public static event Action<bool> CloseFullAdEvent;
        public static event Action ErrorFullAdEvent;

        // События для Rewarded рекламы
        public static event Action OpenRewardedVideoEvent;
        public static event Action<int> RewardVideoEvent;
        public static event Action CloseRewardedVideoEvent;
        public static event Action ErrorRewardedVideoEvent;

        // Поля для предпросмотра рекламы
        private Canvas adCanvas;
        private GameObject adPreviewInstance;
        private float adCooldown = 30f; // Время между показами рекламы
        private float timerShowAd = 0f;

        // Флаг, указывающий был ли модуль уже инициализирован
        private bool isInitialized = false;

        // Реализация IAdsModule
        public void ShowRewarded(int id)
        {
            if (CanShowAd())
            {
#if UNITY_EDITOR
                ShowAdPreview();
                SimulateRewardedAd(id);
#else
                ShowRewardedVideo_js(id);
#endif
                ResetAdTimer();
            }
        }

        public void ShowFullscreen()
        {
            if (CanShowAd())
            {
#if UNITY_EDITOR
                ShowAdPreview();
#else
                ShowFullscreenAd_js();
#endif
                ResetAdTimer();
            }
        }

        // Методы для работы с рекламой
        public void OnRewardedVideoAdOpened()
        {
            // YandexGameManager.Log("Rewarded Video ad opened", "info", "Manager Ad");
            OpenRewardedVideoEvent?.Invoke();
        }

        public void OnRewardedVideoAdClosed()
        {
            // YandexGameManager.Log("Video ad closed", "info", "Manager Ad");
            CloseRewardedVideoEvent?.Invoke();
        }

        public void OnRewardedVideoAdError()
        {
            // YandexGameManager.Log("Error showing video ad", "error", "Manager Ad");
            ErrorRewardedVideoEvent?.Invoke();
        }

        public void OnVideoAdRewarded(int id)
        {
            // YandexGameManager.Log($"Video ad rewarded, ID: {id}", "success", "Manager Ad");
            RewardVideoEvent?.Invoke(id);
        }

        // События для полноэкранной рекламы
        public void OnFullAdOpened()
        {
            // YandexGameManager.Log("Fullscreen ad opened", "info", "Ad");
            OpenFullAdEvent?.Invoke();
        }

        public void OnFullAdClosed(string wasShownString)
        {
            bool wasShown = wasShownString == "true";
            // YandexGameManager.Log($"Fullscreen ad closed, was shown: {wasShown}", "info", "Manager Ad");
            CloseFullAdEvent?.Invoke(wasShown);
        }

        public void OnFullAdError()
        {
            // YandexGameManager.Log("Error showing fullscreen ad", "error", "Manager Ad");
            ErrorFullAdEvent?.Invoke();
        }

        // Вспомогательные методы
        public bool CanShowAd()
        {
            return timerShowAd >= adCooldown;
        }

        public float GetTimeUntilNextAd()
        {
            return Mathf.Max(0, adCooldown - timerShowAd);
        }

        private void ResetAdTimer()
        {
            timerShowAd = 0f;
        }

        /// <summary>
        /// Устанавливает таймер на максимальное значение, чтобы реклама была доступна сразу
        /// </summary>
        public void ForceAdAvailable()
        {
            timerShowAd = adCooldown;
        }

        private void ShowAdPreview()
        {
            // Удаляем предыдущий Canvas, если он существует
            if (adCanvas != null)
            {
                Destroy(adCanvas.gameObject);
            }
            
            // Создаем Canvas для предпросмотра рекламы
            adCanvas = new GameObject("AdPreviewCanvas").AddComponent<Canvas>();
            adCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            adCanvas.sortingOrder = 1000;
            DontDestroyOnLoad(adCanvas.gameObject);

            // Создаем зеленый экран для рекламы
            adPreviewInstance = new GameObject("AdPreview");
            adPreviewInstance.transform.SetParent(adCanvas.transform, false);

            RectTransform rectTransform = adPreviewInstance.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            Image image = adPreviewInstance.AddComponent<Image>();
            image.color = Color.green;

            // Показываем предпросмотр рекламы
            adPreviewInstance.SetActive(true);
            OpenFullAdEvent?.Invoke();
            StartCoroutine(HideAdPreviewAfterDelay(1f));
        }

        private IEnumerator HideAdPreviewAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            CloseFullAdEvent?.Invoke(true);
            
            // Уничтожаем объект рекламы и канвас
            if (adCanvas != null)
            {
                Destroy(adCanvas.gameObject);
                adCanvas = null;
                adPreviewInstance = null;
            }
        }

        private void SimulateRewardedAd(int rewardId)
        {
            // YandexGameManager.Log($"Simulating rewarded ad. Reward ID: {rewardId} given.", "success", "Ad");
            RewardVideoEvent?.Invoke(rewardId);
        }

        private void Update()
        {
            if (timerShowAd < adCooldown)
            {
                timerShowAd += Time.deltaTime;
            }
        }

        // Реализация методов IModule
        public void Initialize() 
        {
            if (!isInitialized)
            {
                _instance = this;
                // YandexGameManager.Log($"Модуль {ModuleName} v{ModuleVersion} инициализирован", "info", "Модуль");
                isInitialized = true;
                
                // Регистрируем этот модуль как текущий экземпляр синглтона
                _instance = this;
         
            }
        }
        
        public void OnEnable() 
        {
            // YandexGameManager.Log($"Модуль {ModuleName} включен", "info", "Модуль");
            // Дополнительный код при включении модуля, если необходим
        }
        
        public void OnDisable() 
        {
            // YandexGameManager.Log($"Модуль {ModuleName} отключен", "info", "Модуль");
            // Дополнительный код при отключении модуля, если необходим
        }
        
        public void OnGUI() 
        {
            // Код для редактора, если необходим
        }
    }

    // Статический класс для удобного доступа
    public static class Ads
    {
        // События для полноэкранной рекламы
        public static event Action OnFullscreenOpened
        {
            add => AdsModule.OpenFullAdEvent += value;
            remove => AdsModule.OpenFullAdEvent -= value;
        }
        
        public static event Action<bool> OnFullscreenClosed
        {
            add => AdsModule.CloseFullAdEvent += value;
            remove => AdsModule.CloseFullAdEvent -= value;
        }
        
        public static event Action OnFullscreenError
        {
            add => AdsModule.ErrorFullAdEvent += value;
            remove => AdsModule.ErrorFullAdEvent -= value;
        }

        // События для Rewarded рекламы
        public static event Action OnRewardedOpened
        {
            add => AdsModule.OpenRewardedVideoEvent += value;
            remove => AdsModule.OpenRewardedVideoEvent -= value;
        }
        
        public static event Action OnRewardedClosed
        {
            add => AdsModule.CloseRewardedVideoEvent += value;
            remove => AdsModule.CloseRewardedVideoEvent -= value;
        }
        
        public static event Action<int> OnRewarded
        {
            add => AdsModule.RewardVideoEvent += value;
            remove => AdsModule.RewardVideoEvent -= value;
        }
        
        public static event Action OnRewardedError
        {
            add => AdsModule.ErrorRewardedVideoEvent += value;
            remove => AdsModule.ErrorRewardedVideoEvent -= value;
        }

        /// <summary>
        /// Проверяет, инициализирован ли модуль рекламы
        /// </summary>
        private static bool IsModuleAvailable()
        {
            if (AdsModule.Instance == null)
            {
                Debug.LogWarning("Модуль рекламы не инициализирован или отключен в // YandexGameManager");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Показывает полноэкранную рекламу
        /// </summary>
        public static void ShowFullscreen()
        {
            if (IsModuleAvailable())
            {
                AdsModule.Instance.ShowFullscreen();
            }
        }

        /// <summary>
        /// Показывает рекламу с вознаграждением
        /// </summary>
        public static void ShowRewarded(int id)
        {
            if (IsModuleAvailable())
            {
                AdsModule.Instance.ShowRewarded(id);
            }
        }

        // Свойства для доступа к данным модуля
        public static string Name => AdsModule.Instance?.ModuleName ?? "Ads Module";
        public static string Version => AdsModule.Instance?.ModuleVersion ?? "1.0";
        public static string Description => AdsModule.Instance?.ModuleDescription ?? "Модуль рекламы Яндекс.Игр";
        public static bool IsEnabled
        {
            get => AdsModule.Instance?.IsEnabled ?? false;
            set
            {
                if (AdsModule.Instance != null)
                {
                    AdsModule.Instance.IsEnabled = value;
                }
            }
        }

        // Методы для работы с модулем
        public static void Initialize()
        {
            if (AdsModule.Instance != null)
            {
                AdsModule.Instance.Initialize();
            }
        }

        public static void Enable()
        {
            if (AdsModule.Instance != null)
            {
                AdsModule.Instance.OnEnable();
            }
        }

        public static void Disable()
        {
            if (AdsModule.Instance != null)
            {
                AdsModule.Instance.OnDisable();
            }
        }

        // Методы для проверки состояния
        public static bool CanShowAd()
        {
            return AdsModule.Instance?.CanShowAd() ?? false;
        }

        public static float GetTimeUntilNextAd()
        {
            return AdsModule.Instance?.GetTimeUntilNextAd() ?? 0f;
        }
    }
}