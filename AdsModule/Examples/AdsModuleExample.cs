using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FoundersKit.Examples
{
    /// <summary>
    /// Пример использования модуля рекламы
    /// </summary>
    public class AdsModuleExample : MonoBehaviour
    {
        #region Inspector Variables
        [Header("(—— Scene Settings")]
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected string moduleTitle = "Модуль рекламы";

        [Header("—— Moduls")]
        [SerializeField] private Modules.AdsModule adsModule;
        [SerializeField] private Modules.TimerAds timerAds;

        [Header("—— UI")]
        [SerializeField] private Button showFullscreenAdButton;
        [SerializeField] private Button showRewardedAdButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI adTimerText;

        #endregion

        #region Private Variables
        // Локальная переменная для отслеживания времени рекламы
        private float adTimer = 0f;
        private float adTimerBeforeReset = 60f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Если модуль не указан через инспектор, найдем его на сцене
            if (adsModule == null)
            {
                adsModule = FindFirstObjectByType<Modules.AdsModule>();
            }

            if (timerAds == null)
            {
                timerAds = FindFirstObjectByType<Modules.TimerAds>();
            }
        }

        protected virtual void Start()
        {

            // Настраиваем заголовок модуля, если он указан
            if (titleText != null)
            {
                titleText.text = moduleTitle;
            }

            // Настраиваем кнопки
            if (showFullscreenAdButton != null)
            {
                showFullscreenAdButton.onClick.AddListener(OnShowFullscreenAdClicked);
            }

            if (showRewardedAdButton != null)
            {
                showRewardedAdButton.onClick.AddListener(() => OnShowRewardedAdClicked(1));
            }

            if (timerAds != null && adTimerText != null)
            {
                float timeAdsLeft = timerAds.advertisingInterval;

                adTimerText.text = $"Таймер реклама показывается каждые: {timeAdsLeft:F1} секунд.";
            }

            UpdateUI();
        }

        private void Update()
        {
            // Обновляем отсчет таймера
            adTimer += Time.deltaTime;

           

            // Обновляем информацию о таймере
            if (adsModule != null && timerText != null)
            {
                float timeLeft = adsModule.GetTimeUntilNextAd();
                timerText.text = $"Показ рекламы доступен не раньше: {timeLeft:F1} секунд." ;
            }

            // Обновляем состояние кнопок
            UpdateUI();
        }

        #endregion

        #region Methods

        public void TestTimerAdUI()
        {
            if (timerAds == null) return;

            // Активируем экран таймера
            timerAds.SendMessage("PauseAll", SendMessageOptions.DontRequireReceiver);
            timerAds.SendMessage("ResetAdUI", SendMessageOptions.DontRequireReceiver);

            if (timerAds.gameObject.activeInHierarchy)
            {
                var adTimerObj = timerAds.GetType()
                    .GetField("adTimerObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(timerAds) as GameObject;

                var adLoadingWarning = timerAds.GetType()
                    .GetField("adLoadingWarningScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(timerAds) as GameObject;

                var adTimerTextField = timerAds.GetType()
                    .GetField("adTimerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(timerAds) as TextMeshProUGUI;

                if (adTimerObj != null && adTimerTextField != null)
                {
                    adTimerObj.SetActive(true);
                    adTimerTextField.text = "3";
                }

                StartCoroutine(TestTimerCoroutine(adTimerTextField, adTimerObj, adLoadingWarning));
            }
        }

        private IEnumerator TestTimerCoroutine(TextMeshProUGUI textField, GameObject timerObj, GameObject loadingScreen)
        {
            yield return new WaitForSecondsRealtime(1f);
            textField.text = "2";
            yield return new WaitForSecondsRealtime(1f);
            textField.text = "1";
            yield return new WaitForSecondsRealtime(1f);

            timerObj.SetActive(false);
            loadingScreen?.SetActive(true);

            yield return new WaitForSecondsRealtime(2f);

            loadingScreen?.SetActive(false);
            timerAds.SendMessage("ResumeAll", SendMessageOptions.DontRequireReceiver);
        }


        public void ResetTimer()
        {
            if (adsModule != null)
            {
                adTimerBeforeReset = adsModule.adCooldown;
                adsModule.adCooldown = 0;
            }
        }

        private void UpdateUI()
        {
            if (adsModule == null)
            {
                if (statusText != null)
                {
                    statusText.text = "Модуль рекламы не найден!";
                    statusText.color = Color.red;
                }

                if (showFullscreenAdButton != null) showFullscreenAdButton.interactable = false;
                if (showRewardedAdButton != null) showRewardedAdButton.interactable = false;
                return;
            }

            bool canShowAd = adsModule.CanShowAd();

            if (statusText != null)
            {
                statusText.text = canShowAd 
                    ? "Реклама доступна для показа" 
                    : "Полноэкранная реклама недоступна (время ожидания)";
                statusText.color = canShowAd ? Color.green : Color.yellow;
            }

            // Полноэкранная реклама с таймером
            if (showFullscreenAdButton != null) showFullscreenAdButton.interactable = canShowAd;
            
            // Реклама с вознаграждением всегда доступна
            if (showRewardedAdButton != null) showRewardedAdButton.interactable = true;
        }

        #endregion

        #region Button Handlers

        private void OnShowFullscreenAdClicked()
        {
            if (adsModule != null)
            {
                statusText.text = "Показываю полноэкранную рекламу...";
                adsModule.ShowFullscreen();
            }
        }

        private void OnShowRewardedAdClicked(int rewardId)
        {
            if (adsModule != null)
            {
                statusText.text = "Показываю рекламу с вознаграждением...";
                adsModule.ShowRewarded(rewardId);
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnable()
        {
            // Подписываемся на события рекламы
            Modules.AdsModule.OpenFullAdEvent += OnFullAdOpened;
            Modules.AdsModule.CloseFullAdEvent += OnFullAdClosed;
            Modules.AdsModule.ErrorFullAdEvent += OnFullAdError;
            Modules.AdsModule.RewardVideoEvent += OnVideoAdRewarded;
        }

        private void OnDisable()
        {
            // Отписываемся от событий рекламы
            Modules.AdsModule.OpenFullAdEvent -= OnFullAdOpened;
            Modules.AdsModule.CloseFullAdEvent -= OnFullAdClosed;
            Modules.AdsModule.ErrorFullAdEvent -= OnFullAdError;
            Modules.AdsModule.RewardVideoEvent -= OnVideoAdRewarded;
        }

        private void OnFullAdOpened()
        {
            Debug.Log("Полноэкранная реклама открыта");
            statusText.text = "Реклама открыта";
        }

        private void OnFullAdClosed(bool wasShown)
        {
            ResetAdTimerBack();

            Debug.Log($"Полноэкранная реклама закрыта. Была показана: {wasShown}");
            statusText.text = wasShown 
                ? "Реклама успешно показана" 
                : "Реклама была закрыта, но не показана";
        }

        private void OnFullAdError()
        {
            ResetAdTimerBack();

            Debug.LogError("Ошибка при показе рекламы");
            statusText.text = "Ошибка при показе рекламы!";
            statusText.color = Color.red;
        }

        private void OnVideoAdRewarded(int rewardId)
        {
            ResetAdTimerBack();

            Debug.Log($"Получена награда за просмотр рекламы: {rewardId}");
            
            // Обрабатываем различные типы наград
            string rewardDescription = rewardId switch
            {
                1 => "+100 монет",
                2 => "+1 жизнь",
                3 => "Бонусное оружие",
                _ => $"Неизвестная награда ({rewardId})"
            };
            
            if (rewardText != null)
            {
                rewardText.text = $"Награда получена: {rewardDescription}";
                rewardText.gameObject.SetActive(true);
                
                // Скрываем текст через 3 секунды
                Invoke(nameof(HideRewardText), 3f);
            }
        }

        private void ResetAdTimerBack()
        {
            if (adsModule != null)
            {
                adsModule.adCooldown = adTimerBeforeReset;
            }
        }

        private void HideRewardText()
        {
            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(false);
            }
        }

        #endregion
    }
} 