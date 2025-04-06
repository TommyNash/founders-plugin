using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using FoundersPlugin.Modules;
using UnityEngine.SceneManagement;

namespace FoundersPlugin.Examples
{
    /// <summary>
    /// Пример использования модуля рекламы
    /// </summary>
    public class AdsModuleExample : MonoBehaviour
    {
        [Header("Настройки возврата")]
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected string moduleTitle = "Модуль рекламы";

        [Header("Модуль рекламы")]
        [SerializeField] private AdsModule adsModule;

        [Header("UI элементы")]
        [SerializeField] private Button showFullscreenAdButton;
        [SerializeField] private Button showRewardedAdButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI rewardText;

        private void Awake()
        {
            // Если модуль не указан через инспектор, найдем его на сцене
            if (adsModule == null)
            {
                adsModule = FindFirstObjectByType<AdsModule>();
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

            UpdateUI();
        }

        private void Update()
        {
            // Обновляем информацию о таймере
            if (adsModule != null && timerText != null)
            {
                float timeLeft = adsModule.GetTimeUntilNextAd();
                timerText.text = $"Время до следующей рекламы: {timeLeft:F1} секунд";
            }

            // Обновляем состояние кнопок
            UpdateUI();
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
                adsModule.ShowRewarded(rewardId, true);
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnable()
        {
            // Подписываемся на события рекламы
            AdsModule.OpenFullAdEvent += OnFullAdOpened;
            AdsModule.CloseFullAdEvent += OnFullAdClosed;
            AdsModule.ErrorFullAdEvent += OnFullAdError;
            AdsModule.RewardVideoEvent += OnVideoAdRewarded;
        }

        private void OnDisable()
        {
            // Отписываемся от событий рекламы
            AdsModule.OpenFullAdEvent -= OnFullAdOpened;
            AdsModule.CloseFullAdEvent -= OnFullAdClosed;
            AdsModule.ErrorFullAdEvent -= OnFullAdError;
            AdsModule.RewardVideoEvent -= OnVideoAdRewarded;
        }

        private void OnFullAdOpened()
        {
            Debug.Log("Полноэкранная реклама открыта");
            statusText.text = "Реклама открыта";
        }

        private void OnFullAdClosed(bool wasShown)
        {
            Debug.Log($"Полноэкранная реклама закрыта. Была показана: {wasShown}");
            statusText.text = wasShown 
                ? "Реклама успешно показана" 
                : "Реклама была закрыта, но не показана";
        }

        private void OnFullAdError()
        {
            Debug.LogError("Ошибка при показе рекламы");
            statusText.text = "Ошибка при показе рекламы!";
            statusText.color = Color.red;
        }

        private void OnVideoAdRewarded(int rewardId)
        {
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