using TMPro;
using UnityEngine;
using System.Collections;

namespace FoundersPlugin.Modules
{
    public class TimerAds : MonoBehaviour
    {
        #region Inspector Variables

        [Header("—— Ads settings")]
        [Tooltip("Интервал показа рекламы в секундах")]
        [SerializeField] public int advertisingInterval = 60;

        [Header("—— Timer Screens")]
        [Tooltip("Экран таймера")]
        [SerializeField] private GameObject adTimerObject;
        [Tooltip("Текст таймера")]
        [SerializeField] private TextMeshProUGUI adTimerText;

        [Header("—— Alert Screens")]
        [Tooltip("Предупреждение о загрузке рекламы")]
        [SerializeField] private GameObject adLoadingWarningScreen;
        [Tooltip("Экран ошибки рекламы")]
        [SerializeField] private GameObject adErrorScreen;

        [Header("—— Purchasing UI")]
        [Tooltip("Используется если у вас есть кнопка, которая открывает меню с отлючением рекламы")]
        [SerializeField] private bool useDisableAdButton = true;
        [Tooltip("Кнопка отлючения рекламы")]
        [SerializeField] private GameObject disableAdButton;
        [Tooltip("Экран отключения рекламы")]
        [SerializeField] private GameObject disableAdScreen;

        #endregion

        #region Private Variables

        [Header("—— Technical variables")]
        private bool adDisplayAvailable = true;
        private bool isAdDisplayError = false;
        private bool _countdownActive;

        private int countdownTimer = 2;

        private Coroutine _countdownRoutine;
        private AdsModule adsModule;

        private string moduleAttribute = "TimerAds";

        #endregion

        #region UI
        private void UICheck()
        {
            var missing = new System.Text.StringBuilder();
            if (adTimerObject == null) missing.Append("adTimerObject, ");
            if (adTimerText == null) missing.Append("adTimerText, ");
            if (adLoadingWarningScreen == null) missing.Append("adLoadingWarningScreen, ");
            if (adErrorScreen == null) missing.Append("adErrorScreen, ");
            if (useDisableAdButton)
            {
                if (disableAdButton == null) missing.Append("disableAdButton, ");
                if (disableAdScreen == null) missing.Append("disableAdScreen, ");
            }
            if (missing.Length > 0)
            {
                Debug.LogError($"[TimerAds] Не назначены поля: {missing.ToString().TrimEnd(' ', ',')}");
                enabled = false;
            }
        }

        private void CheckAdUiState()
        {
            if (!YandexGameManager.savesData.isAdAllowed)
            {
                disableAdButton.SetActive(false);
                disableAdScreen.SetActive(false);
            }
        }

        private void ResetAdUI()
        {
            adTimerObject.SetActive(false);
            adLoadingWarningScreen.SetActive(false);
            adErrorScreen.SetActive(false);
            isAdDisplayError = false;
        }

        #endregion

        #region Events

        private void OnEnable()
        {
            AdsModule.OpenFullAdEvent += HandleOpenFullAdEvent;
            AdsModule.CloseFullAdEvent += HandleCloseFullAdEvent;
            AdsModule.ErrorFullAdEvent += HandleErrorAdEvent;
            AdsModule.OpenRewardedVideoEvent += HandleOpenRewardedEvent;
            AdsModule.CloseRewardedVideoEvent += HandleCloseRewardedEvent;
            AdsModule.ErrorRewardedVideoEvent += HandleErrorRewardedEvent;
        }

        private void OnDisable()
        {
            AdsModule.OpenFullAdEvent -= HandleOpenFullAdEvent;
            AdsModule.CloseFullAdEvent -= HandleCloseFullAdEvent;
            AdsModule.ErrorFullAdEvent -= HandleErrorAdEvent;
            AdsModule.OpenRewardedVideoEvent -= HandleOpenRewardedEvent;
            AdsModule.CloseRewardedVideoEvent -= HandleCloseRewardedEvent;
            AdsModule.ErrorRewardedVideoEvent -= HandleErrorRewardedEvent;

            StopCountdown();
        }

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            UICheck();

            if (advertisingInterval < 60)
            {
                YandexGameManager.Log(
                    $"Change the Ads Display Interval value. Must be >= 60. Current: {advertisingInterval}",
                    "warning", "TimerAds");
                adDisplayAvailable = false;
            }

            adsModule = AdsModule.Instance;
        }

        private void Start()
        {
            if (useDisableAdButton)
                CheckAdUiState();

            ResetAdUI();
        }

        private void Update()
        {
            if (!_countdownActive
                && adDisplayAvailable
                && IsInputDetected()
                && YandexGameManager.timerShowAd >= advertisingInterval
                && adsModule.CanShowAd()
                && !isAdDisplayError
                && YandexGameManager.savesData.isAdAllowed
                && !YandexGameManager.serviceData.isScreenOpen)
            {
                _countdownRoutine = StartCoroutine(CountdownToShowAd());
                _countdownActive = true;
            }
        }

        #endregion

        #region Countdown Ad logic
        private bool IsInputDetected()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
            return false;
        }

        public void CallCountdownAd()
        {
            if (!_countdownActive && adDisplayAvailable)
            {
                _countdownRoutine = StartCoroutine(CountdownToShowAd());
                _countdownActive = true;
            }
        }

        private IEnumerator CountdownToShowAd()
        {
            PauseAll();
            adTimerObject.SetActive(true);

            for (int i = countdownTimer; i > 0; i--)
            {
                adTimerText.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            ShowFullscreenAd();
        }

        private void ShowFullscreenAd()
        {
            if (!isAdDisplayError)
            {
                adLoadingWarningScreen.SetActive(true);
                if (adsModule != null)
                    adsModule.ShowFullscreen();
                else
                    HandleError("Ads Module not available.");
            }
            else
            {
                HandleError("Ad display error state.");
            }
        }

        private void StopCountdown()
        {
            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }
            _countdownActive = false;
        }

        #endregion

        #region AdsModule Event Handlers

        private void HandleOpenFullAdEvent()
        {
            adLoadingWarningScreen.SetActive(false);
            PauseAll();
        }


        private void HandleCloseFullAdEvent(bool shown)
        {
            YandexGameManager.timerShowAd = 0;
            adLoadingWarningScreen.SetActive(false);
            ResumeAll();
            ResetAdUI();
        }

        private void HandleErrorAdEvent() => HandleError("Fullscreen ad error");

        private void HandleOpenRewardedEvent() => PauseAll();

        private void HandleCloseRewardedEvent()
        {
            YandexGameManager.timerShowAd = 0;
            ResumeAll();
            ResetAdUI();
        }

        private void HandleErrorRewardedEvent() => HandleError("Rewarded ad error");

        public void OnAdErrorScreenClose()
        {
            ResetAdUI();
            ResumeAll();
        }

        private void HandleError(string message)
        {
            isAdDisplayError = true;
            adErrorScreen.SetActive(true);

            YandexGameManager.UnityLog( $"{message}", "warning", moduleAttribute);
            ResumeAll();
        }
        #endregion

        #region Process state change
        private void PauseAll()
        {
            YandexGameManager.PauseGame();
            YandexGameManager.GameplayStop();
            SafeStopAudio();
            YandexGameManager.serviceData.isScreenOpen = true;
        }

        private void ResumeAll()
        {
            SafeUnpauseAudio();
            YandexGameManager.ResumeGame();
            YandexGameManager.GameplayStart();
            YandexGameManager.serviceData.isScreenOpen = false;
            _countdownActive = false;
            _countdownRoutine = null;
        }

        #endregion

        #region Audio Start/Stop
        private void SafeStopAudio()
        {
            /*
            try
            {
                if (SourceAudio.Instance != null && SourceAudio.Instance.IsPlaying)
                    SourceAudio.Instance.Stop();
            }
            catch { }
            */
        }

        private void SafeUnpauseAudio()
        {
            /*
            try
            {
                if (SourceAudio.Instance != null && !SourceAudio.Instance.IsPlaying)
                    SourceAudio.Instance.UnPause();
            }
            catch { }
            */
        }
        #endregion
    }
}
