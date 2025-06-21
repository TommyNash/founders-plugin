using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using FoundersKit.Logging;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Модуль для показа рекламы. Платформа Yandex.Games
    /// </summary>
    public class AdsModule : MonoBehaviour
    {

        #region Inspector Variables
        [Header("—— Module Settings")]
        [HideInInspector] public float adCooldown = 60f;

        [Header("—— Debugging")]
        [SerializeField] private bool simulateAdsInEditor = true;
        [Tooltip("Вкл/Выкл показа заглушки рекламы в unity")]
        [SerializeField] private float simulatedAdDuration = 2f;
        [Tooltip("Продолжительность симуляции заглкшки")]

        [Header("—— Global variables")]
        public bool isAdCurrentlyLoading = false;
        #endregion

        #region Private Variables
        public const string CONTEXT_NAME = "AdsModule";

        private float _lastAdTime;
        private bool _isShowingAd;
        private int _currentRewardId;
        private static bool isInitialized = false;

        private Canvas _adCanvas;
        private GameObject _adPreviewInstance;
        #endregion

        #region Singleton
        private static AdsModule _instance;
        public static AdsModule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AdsModule>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AdsModule");
                        _instance = go.AddComponent<AdsModule>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Event Delegates
        public static event Action OpenFullAdEvent;
        public static event Action<bool> CloseFullAdEvent;
        public static event Action ErrorFullAdEvent;

        public static event Action OpenRewardedVideoEvent;
        public static event Action CloseRewardedVideoEvent;
        public static event Action ErrorRewardedVideoEvent;
        public static event Action<int> RewardVideoEvent;
        #endregion

        #region Unity Lifecycle
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

            if (!isInitialized)
            {
                Initialize();
            }
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
                Log.Error($"Error initializing AudioModule: {ex.Message}", CONTEXT_NAME);
            }
        }
        #endregion

        #region Public API
        public void ShowFullscreen()
        {
            if (CanShowAd())
            {
                Log.Info($"ShowFullscreen at {Time.time:F1}", CONTEXT_NAME, scope: LogScope.Local);
                ResetAdTimer();
#if UNITY_EDITOR
                StartEditorFullAdSimulation();
#else
                ShowFullscreenAd_js();
#endif
            }
            else
            {
                float timeLeft = GetTimeUntilNextAd();
                Log.Warning($"Cannot show fullscreen ad. Cooldown: {timeLeft:F1}s remaining", CONTEXT_NAME, scope: LogScope.Local);
            }
        }

        public void ShowRewarded(int rewardId)
        {
            _currentRewardId = rewardId;
            Log.Info($"ShowRewarded(ID={rewardId}) at {Time.time:F1}", CONTEXT_NAME, scope: LogScope.Local);
#if UNITY_EDITOR
            StartEditorRewardedAdSimulation();
#else
            ShowRewardedVideo_js(rewardId);
#endif
        }

        public bool CanShowAd()
        {
            return !_isShowingAd && (Time.time - _lastAdTime) >= adCooldown;
        }

        public float GetTimeUntilNextAd()
        {
            float timeSince = Time.time - _lastAdTime;
            return Mathf.Max(0f, adCooldown - timeSince);
        }
        #endregion

        #region Internal Helpers
        private void ResetAdTimer()
        {
            _lastAdTime = Time.time;
        }

        private void StartEditorFullAdSimulation()
        {
            _isShowingAd = true;
            ShowAdPreview(false);
            StartCoroutine(HideAdPreviewAfterDelay(simulatedAdDuration, false));
        }

        private void StartEditorRewardedAdSimulation()
        {
            _isShowingAd = true;
            ShowAdPreview(true);
            OpenRewardedVideoEvent?.Invoke();
            RewardVideoEvent?.Invoke(_currentRewardId);
            StartCoroutine(HideAdPreviewAfterDelay(simulatedAdDuration, true));
        }

        private void ShowAdPreview(bool isRewarded)
        {
            if (!simulateAdsInEditor) return;

            if (_adCanvas == null)
            {
                _adCanvas = new GameObject("AdPreviewCanvas").AddComponent<Canvas>();
                _adCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _adCanvas.sortingOrder = 1000;
                DontDestroyOnLoad(_adCanvas.gameObject);
            }
            if (_adPreviewInstance == null)
            {
                _adPreviewInstance = new GameObject("AdPreview");
                _adPreviewInstance.transform.SetParent(_adCanvas.transform, false);

                var rect = _adPreviewInstance.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                var img = _adPreviewInstance.AddComponent<Image>();
                img.color = isRewarded ? Color.blue : Color.green;
            }

            _adPreviewInstance.SetActive(true);
            if (isRewarded)
                OpenRewardedVideoEvent?.Invoke();
            else
                OpenFullAdEvent?.Invoke();
        }

        private IEnumerator HideAdPreviewAfterDelay(float delay, bool isRewarded)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (_adPreviewInstance != null)
                _adPreviewInstance.SetActive(false);

            if (isRewarded)
                CloseRewardedVideoEvent?.Invoke();
            else
                CloseFullAdEvent?.Invoke(true);

            YandexGameManager.ResumeGame();
            YandexGameManager.GameplayStart();

            _isShowingAd = false;

            // Очистка preview объектов
            if (_adPreviewInstance != null)
                Destroy(_adPreviewInstance);
            if (_adCanvas != null)
                Destroy(_adCanvas.gameObject);

            _adPreviewInstance = null;
            _adCanvas = null;
        }
        #endregion

        #region JS Callbacks
        public void OnFullAdOpen()
        {
            _isShowingAd = true;

            YandexGameManager.UnityLog("Full ad opened", "info", CONTEXT_NAME);

            OpenFullAdEvent?.Invoke();
        }

        public void OnFullAdClose(string wasShown)
        {
            bool shown = wasShown.ToLower() == "true";

            if (shown)
            {
                YandexGameManager.UnityLog($"Full ad closed. Shown={shown}", "success", CONTEXT_NAME);
            }
            else
            {
                YandexGameManager.UnityLog($"Full ad closed. Shown={shown}", "warning", CONTEXT_NAME);
            }

            CloseFullAdEvent?.Invoke(shown);
            _isShowingAd = false;
        }

        public void OnFullAdError()
        {
            YandexGameManager.UnityLog("Full ad error", "error", CONTEXT_NAME);

            ErrorFullAdEvent?.Invoke();
            _isShowingAd = false;
        }

        public void OnRewardedOpen()
        {
            _isShowingAd = true;

            YandexGameManager.UnityLog("Rewarded ad opened", "info", CONTEXT_NAME);

            OpenRewardedVideoEvent?.Invoke();
        }

        public void OnRewardedClose()
        {
            YandexGameManager.UnityLog("Rewarded ad closed", "success", CONTEXT_NAME);

            CloseRewardedVideoEvent?.Invoke();
            _isShowingAd = false;
        }

        public void OnRewardedError()
        {
            YandexGameManager.UnityLog("Rewarded ad error", "error", CONTEXT_NAME);

            ErrorRewardedVideoEvent?.Invoke();
            _isShowingAd = false;
        }

        public void OnRewarded(int id)
        {
            YandexGameManager.UnityLog($"Video ad rewarded, ID: {id}", "success", CONTEXT_NAME);

            RewardVideoEvent?.Invoke(id);
        }
        #endregion

        #region Native JS Interfaces
        [DllImport("__Internal")]
        private static extern void ShowFullscreenAd_js();

        [DllImport("__Internal")]
        private static extern void ShowRewardedVideo_js(int rewardId);
        #endregion
    }
}
