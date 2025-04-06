// Assets/FoundersPlugin/Modules/Ads Module/AdsModule.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Модуль для показа рекламы в Яндекс.Играх
    /// </summary>
    public class AdsModule : MonoBehaviour
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

        // Основные свойства модуля
        public string ModuleName => "Yandex Ads";
        public string ModuleVersion => "1.0";
        public string ModuleDescription => "Модуль рекламы Яндекс.Игр";
        public bool IsEnabled { get; set; } = true;

        // Singleton для статического доступа
        private static AdsModule _instance;
        public static AdsModule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<AdsModule>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("AdsModule не найден в сцене. Убедитесь, что объект с компонентом AdsModule добавлен.");
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

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Показывает рекламу с вознаграждением
        /// </summary>
        /// <param name="id">ID вознаграждения</param>
        public void ShowRewarded(int id)
        {
            ShowRewarded(id, false);
        }

        /// <summary>
        /// Показывает рекламу с вознаграждением с возможностью игнорировать cooldown
        /// </summary>
        /// <param name="id">ID вознаграждения</param>
        /// <param name="ignoreCooldown">Игнорировать проверку таймера между рекламой</param>
        public void ShowRewarded(int id, bool ignoreCooldown)
        {
            #if UNITY_EDITOR
                Debug.Log("Show rewarded video ad in Editor");
                SimulateRewardedAd(id);
                return;
            #elif UNITY_WEBGL && !UNITY_EDITOR
                if (ignoreCooldown || CanShowAd())
                {
                    try
                    {
                        ShowRewardedVideo_js(id);
                        if (!ignoreCooldown) ResetAdTimer();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error showing rewarded video: {e.Message}");
                        OnRewardedVideoAdError();
                    }
                }
                else
                {
                    Debug.Log("Ad cooldown is active. Can't show rewarded video now.");
                }
            #endif
        }

        // Показать полноэкранную рекламу
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

        // Методы обратного вызова для JS
        public void OnRewardedVideoAdOpened()
        {
            Debug.Log("Rewarded Video ad opened");
            OpenRewardedVideoEvent?.Invoke();
        }

        public void OnRewardedVideoAdClosed()
        {
            Debug.Log("Video ad closed");
            CloseRewardedVideoEvent?.Invoke();
        }

        public void OnRewardedVideoAdError()
        {
            Debug.LogError("Error showing video ad");
            ErrorRewardedVideoEvent?.Invoke();
        }

        public void OnVideoAdRewarded(int id)
        {
            Debug.Log($"Video ad rewarded, ID: {id}");
            RewardVideoEvent?.Invoke(id);
        }

        // События для полноэкранной рекламы
        public void OnFullAdOpened()
        {
            Debug.Log("Fullscreen ad opened");
            OpenFullAdEvent?.Invoke();
        }

        public void OnFullAdClosed(string wasShownString)
        {
            bool wasShown = wasShownString == "true";
            Debug.Log($"Fullscreen ad closed, was shown: {wasShown}");
            CloseFullAdEvent?.Invoke(wasShown);
        }

        public void OnFullAdError()
        {
            Debug.LogError("Error showing fullscreen ad");
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

        private void Update()
        {
            if (timerShowAd < adCooldown)
            {
                timerShowAd += Time.deltaTime;
            }
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
            Debug.Log($"Simulating rewarded ad. Reward ID: {rewardId} given.");
            RewardVideoEvent?.Invoke(rewardId);
        }
    }
}