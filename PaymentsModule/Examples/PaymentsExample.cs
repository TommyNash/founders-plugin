using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using FoundersKit.Logging;

namespace FoundersKit.Modules.Examples
{
    /// <summary>
    /// Демонстрация всех возможностей PaymentsModule через UI
    /// 
    /// ИНТЕГРАЦИЯ С PurchaseItemUI:
    /// 
    /// 1. Создайте префаб с компонентом PurchaseItemUI:
    ///    - Создайте UI GameObject (например, Panel)
    ///    - Добавьте компонент PurchaseItemUI
    ///    - Настройте UI элементы (TextMeshProUGUI для цены, названия, описания)
    ///    - Добавьте Image для иконки валюты
    ///    - Добавьте Image для изображения товара (imageURI)
    ///    - Добавьте Button для покупки
    ///    - Назначьте все ссылки в инспекторе PurchaseItemUI
    /// 
    /// 2. Настройте PaymentsExample:
    ///    - Установите usePurchaseItemUI = true
    ///    - Назначьте productsContainer (Transform для размещения товаров)
    ///    - Назначьте purchaseItemPrefab (ваш префаб с PurchaseItemUI)
    ///    - Настройте флаги отображения (showProductTitles, showProductDescriptions, showProductImages, etc.)
    ///    - Выберите размер иконок валют (currencyImageSize: Small, Medium, Svg)
    /// 
    /// 3. Система автоматически:
    ///    - Создаст UI элементы для каждого доступного товара
    ///    - Настроит отображение цены, названия, описания, иконки валюты и изображения товара
    ///    - Добавит функциональные кнопки покупки
    ///    - Обновит UI при изменении данных товаров
    /// 
    /// ПРИМЕР ПРЕФАБА PurchaseItemUI:
    /// - Background (Image) - фон элемента
    /// - ProductImage (Image) - изображение товара (imageURI)
    /// - Title (TextMeshProUGUI) - название товара
    /// - Description (TextMeshProUGUI) - описание товара  
    /// - Price (TextMeshProUGUI) - цена товара
    /// - CurrencyImage (Image) - иконка валюты
    /// - BuyButton (Button) - кнопка покупки
    /// 
    /// ДАННЫЕ О ТОВАРЕ (согласно документации Yandex Games):
    /// - id: string - идентификатор товара
    /// - title: string - название товара
    /// - description: string - описание товара
    /// - imageURI: string - URL изображения товара
    /// - price: string - стоимость в формате "цена код_валюты"
    /// - priceValue: string - стоимость товара
    /// - priceCurrencyCode: string - код валюты
    /// - currencyImageUrl: string - URL иконки валюты (по умолчанию)
    /// - currencyImageUrlSmall: string - URL маленькой иконки валюты
    /// - currencyImageUrlMedium: string - URL иконки валюты среднего размера
    /// - currencyImageUrlSvg: string - URL векторной иконки валюты
    /// - getPriceCurrencyImage(size): метод получения иконки валюты
    ///   size: 'small' (по умолчанию), 'medium', 'svg'
    /// </summary>
    public class PaymentsExample : MonoBehaviour
    {
        [Header("—— Модули")]
        [SerializeField] private PaymentsModule paymentsModule;

        [Header("UI Компоненты")]
        [SerializeField] private Button buyItemButton;
        [SerializeField] private Button checkPurchasesButton;
        [SerializeField] private Button clearPurchasesButton;
        [SerializeField] private TMP_Dropdown itemDropdown;
        [SerializeField] private TMP_InputField itemIdInput;
        [SerializeField] private Button addItemButton;

        [Header("Текст")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI purchaseHistoryText;

        [Header("Окна")]
        [SerializeField] private GameObject errorScreen;

        [Header("PurchaseItemUI Настройки")]
        [SerializeField] private Transform productsContainer;           // Контейнер для товаров
        [SerializeField] private GameObject purchaseItemPrefab;         // Префаб PurchaseItemUI
        [SerializeField] private bool usePurchaseItemUI = true;         // Использовать PurchaseItemUI
        [SerializeField] private bool showProductTitles = true;         // Показывать названия товаров
        [SerializeField] private bool showProductDescriptions = true;   // Показывать описания товаров
        [SerializeField] private bool showCurrencyImages = true;        // Показывать иконки валют
        [SerializeField] private bool showProductImages = true;         // Показывать изображения товаров
        [SerializeField] private PurchaseItemUI.CurrencyImageSize currencyImageSize = PurchaseItemUI.CurrencyImageSize.Small; // Размер иконок валют

        [Header("Настройки высоты контейнера")]
        [SerializeField] private bool autoAdjustContainerHeight = true; // Автоматически подстраивать высоту контейнера
        [SerializeField] private float itemHeight = 200f;               // Высота одного элемента товара
        [SerializeField] private float spacing = 10f;                   // Отступ между элементами
        [SerializeField] private float padding = 20f;                   // Отступы сверху и снизу контейнера
        [SerializeField] private float minHeight = 100f;                // Минимальная высота контейнера
        [SerializeField] private float maxHeight = 800f;                // Максимальная высота контейнера

        [Header("Настройки")]
        [SerializeField] private List<string> availableItems = new List<string> { "disable_ad", "coins_100", "coins_500", "premium_pass" };
        [SerializeField] private string defaultItemId = "test";

        private List<string> purchaseHistory = new List<string>();
        private bool isModuleInitialized = false;
        private List<PurchaseItemUI> purchaseItemUIs = new List<PurchaseItemUI>();

        private void Awake()
        {
            // Если модуль не указан через инспектор, найдем его на сцене
            if (paymentsModule == null)
            {
                paymentsModule = FindFirstObjectByType<PaymentsModule>();
            }
        }

        private void Start()
        {
            SetupUI();
            SubscribeToEvents();
            UpdateModuleStatus();
            
            if (usePurchaseItemUI)
            {
                StartCoroutine(InitializePurchaseItemUIs());
            }
        }

        private IEnumerator InitializePurchaseItemUIs()
        {
            // Ждем инициализации модуля
            while (paymentsModule == null || paymentsModule.productDataList.Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
            }

            CreatePurchaseItemUIs();
        }

        private void CreatePurchaseItemUIs()
        {
            if (productsContainer == null || purchaseItemPrefab == null)
            {
                Log.Error("ProductsContainer или PurchaseItemPrefab не настроены!", "PaymentsExample");
                return;
            }

            // Очищаем существующие UI элементы
            ClearPurchaseItemUIs();

            // Создаем UI элементы для каждого товара
            foreach (var product in paymentsModule.productDataList)
            {
                GameObject itemGO = Instantiate(purchaseItemPrefab, productsContainer);
                PurchaseItemUI purchaseItemUI = itemGO.GetComponent<PurchaseItemUI>();
                
                if (purchaseItemUI != null)
                {
                    // Настраиваем параметры отображения
                    purchaseItemUI.id = product.id;
                    purchaseItemUI.showPrice = true;
                    purchaseItemUI.showTitle = showProductTitles;
                    purchaseItemUI.showDescription = showProductDescriptions;
                    purchaseItemUI.showCurrencyImage = showCurrencyImages;
                    purchaseItemUI.showProductImage = showProductImages;
                    purchaseItemUI.currencyImageSize = currencyImageSize;

                    // Добавляем кнопку покупки
                    Button buyButton = itemGO.GetComponentInChildren<Button>();
                    if (buyButton != null)
                    {
                        string productId = product.id; // Создаем локальную копию для замыкания
                        buyButton.onClick.AddListener(() => OnPurchaseItemClicked(productId));
                    }

                    purchaseItemUIs.Add(purchaseItemUI);
                    
                    // Принудительно обновляем UI
                    paymentsModule.UpdateNewUI(purchaseItemUI);
                }
            }

            UpdateStatusText($"Создано {purchaseItemUIs.Count} элементов UI для товаров");
            
            // Подстраиваем высоту контейнера
            AdjustContainerHeight();
        }

        private void ClearPurchaseItemUIs()
        {
            foreach (var ui in purchaseItemUIs)
            {
                if (ui != null)
                {
                    DestroyImmediate(ui.gameObject);
                }
            }
            purchaseItemUIs.Clear();
            
            // Подстраиваем высоту контейнера после очистки
            AdjustContainerHeight();
        }

        /// <summary>
        /// Автоматически подстраивает высоту контейнера товаров в зависимости от количества элементов
        /// </summary>
        private void AdjustContainerHeight()
        {
            if (!autoAdjustContainerHeight || productsContainer == null)
                return;

            RectTransform containerRect = productsContainer as RectTransform;
            if (containerRect == null)
                return;

            int itemCount = purchaseItemUIs.Count;
            
            // Рассчитываем необходимую высоту
            float calculatedHeight = padding * 2; // Отступы сверху и снизу
            
            if (itemCount > 0)
            {
                calculatedHeight += itemCount * itemHeight; // Высота всех элементов
                calculatedHeight += (itemCount - 1) * spacing; // Отступы между элементами
            }

            // Ограничиваем высоту минимальным и максимальным значениями
            calculatedHeight = Mathf.Clamp(calculatedHeight, minHeight, maxHeight);

            // Устанавливаем новую высоту
            Vector2 sizeDelta = containerRect.sizeDelta;
            sizeDelta.y = calculatedHeight;
            containerRect.sizeDelta = sizeDelta;

            Log.Info($"Высота контейнера товаров установлена: {calculatedHeight}px для {itemCount} элементов", "PaymentsExample");
        }

        private void OnPurchaseItemClicked(string productId)
        {
            if (!isModuleInitialized)
            {
                UpdateStatusText("Ошибка: Модуль не инициализирован", true);
                return;
            }

            UpdateStatusText($"Попытка покупки товара: {productId}");
            PaymentsModule.BuyItem(productId);
        }

        private void SetupUI()
        {
            // Настройка кнопки покупки
            if (buyItemButton != null)
            {
                buyItemButton.onClick.AddListener(OnBuyItemClicked);
                buyItemButton.interactable = false; // Будет активирована после инициализации
            }

            // Настройка кнопки проверки покупок
            if (checkPurchasesButton != null)
            {
                checkPurchasesButton.onClick.AddListener(OnCheckPurchasesClicked);
            }

            // Настройка кнопки очистки истории
            if (clearPurchasesButton != null)
            {
                clearPurchasesButton.onClick.AddListener(OnClearHistoryClicked);
            }

            // Настройка кнопки добавления товара
            if (addItemButton != null)
            {
                addItemButton.onClick.AddListener(OnAddItemClicked);
            }

            if (errorScreen != null)
            {
                errorScreen.SetActive(false);
            }

            // Настройка выпадающего списка товаров
            if (itemDropdown != null)
            {
                itemDropdown.ClearOptions();
                itemDropdown.AddOptions(availableItems);
                int defaultIndex = availableItems.IndexOf(defaultItemId);
                itemDropdown.value = defaultIndex >= 0 ? defaultIndex : 0;
                itemDropdown.onValueChanged.AddListener(OnItemSelected);
            }

            // Настройка поля ввода ID товара
            if (itemIdInput != null)
            {
                itemIdInput.text = defaultItemId;
                itemIdInput.onEndEdit.AddListener(OnItemIdChanged);
            }

            UpdateStatusText("Модуль инициализируется...");
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на события модуля платежей
            if (paymentsModule != null)
            {
                PaymentsModule.PurchaseSuccessEvent += OnPurchaseSuccess;
                PaymentsModule.PurchaseFailedEvent += OnPurchaseFailed;
                PaymentsModule.OnProductUpdated += OnProductUpdated;
            }
        }

        private void UpdateModuleStatus()
        {
            if (paymentsModule != null)
            {
                isModuleInitialized = true;
                if (buyItemButton != null)
                {
                    buyItemButton.interactable = true;
                }
                UpdateStatusText("Модуль платежей готов к работе");
            }
            else
            {
                UpdateStatusText("Ошибка: Модуль платежей не найден!");
            }
        }

        #region UI Event Handlers

        private void OnBuyItemClicked()
        {
            if (!isModuleInitialized)
            {
                UpdateStatusText("Ошибка: Модуль не инициализирован", true);
                return;
            }

            string itemId = GetSelectedItemId();
            if (string.IsNullOrEmpty(itemId))
            {
                UpdateStatusText("Ошибка: ID товара не указан", true);
                return;
            }

            UpdateStatusText($"Попытка покупки товара: {itemId}");
            PaymentsModule.BuyItem(itemId);
        }

        private void OnCheckPurchasesClicked()
        {
            if (paymentsModule != null)
            {
                paymentsModule.CheckConsumePurchases();
                UpdateStatusText("Проверка необработанных покупок...");
            }
        }

        private void OnClearHistoryClicked()
        {
            purchaseHistory.Clear();
            UpdatePurchaseHistoryText();
            UpdateStatusText("История покупок очищена");
        }

        private void OnAddItemClicked()
        {
            if (itemIdInput != null && !string.IsNullOrEmpty(itemIdInput.text))
            {
                string newItemId = itemIdInput.text.Trim();
                if (!availableItems.Contains(newItemId))
                {
                    availableItems.Add(newItemId);
                    
                    if (itemDropdown != null)
                    {
                        itemDropdown.ClearOptions();
                        itemDropdown.AddOptions(availableItems);
                    }
                    
                    UpdateStatusText($"Добавлен новый товар: {newItemId}");
                }
                else
                {
                    UpdateStatusText($"Товар {newItemId} уже существует", true);
                }
            }
        }

        private void OnItemSelected(int index)
        {
            if (index >= 0 && index < availableItems.Count)
            {
                string selectedItem = availableItems[index];
                if (itemIdInput != null)
                {
                    itemIdInput.text = selectedItem;
                }
                UpdateStatusText($"Выбран товар: {selectedItem}");
            }
        }

        private void OnItemIdChanged(string newValue)
        {
            if (!string.IsNullOrEmpty(newValue))
            {
                UpdateStatusText($"ID товара изменен на: {newValue}");
            }
        }

        #endregion

        #region Payment Event Handlers

        private void OnPurchaseSuccess(string itemId)
        {
            purchaseHistory.Add($"[{DateTime.Now:HH:mm:ss}] Успешная покупка: {itemId}");
            UpdatePurchaseHistoryText();
            UpdateStatusText($"Покупка успешна: {itemId}", false, true);
        }

        private void OnPurchaseFailed(string itemId)
        {
            purchaseHistory.Add($"[{DateTime.Now:HH:mm:ss}] Неудачная покупка: {itemId}");
            UpdatePurchaseHistoryText();
            UpdateStatusText($"Покупка не удалась: {itemId}", true);

            if (errorScreen != null)
            {
                errorScreen.SetActive(true);
            }
        }

        private void OnProductUpdated(PaymentsModule.ProductData productData)
        {
            UpdateStatusText($"Обновлены данные товара: {productData.title}");
            
            // Обновляем PurchaseItemUI если используется
            if (usePurchaseItemUI)
            {
                RefreshPurchaseItemUIs();
            }
        }

        private void RefreshPurchaseItemUIs()
        {
            // Пересоздаем UI элементы при обновлении данных товаров
            if (purchaseItemUIs.Count != paymentsModule.productDataList.Count)
            {
                CreatePurchaseItemUIs();
            }
            else
            {
                // Обновляем существующие элементы
                foreach (var ui in purchaseItemUIs)
                {
                    if (ui != null)
                    {
                        paymentsModule.UpdateNewUI(ui);
                    }
                }
                
                // Подстраиваем высоту контейнера после обновления
                AdjustContainerHeight();
            }
        }

        #endregion

        #region Helper Methods

        private string GetSelectedItemId()
        {
            if (itemIdInput != null && !string.IsNullOrEmpty(itemIdInput.text))
            {
                return itemIdInput.text.Trim();
            }
            
            if (itemDropdown != null && itemDropdown.value >= 0 && itemDropdown.value < availableItems.Count)
            {
                return availableItems[itemDropdown.value];
            }
            
            return defaultItemId;
        }

        private void UpdateStatusText(string message, bool isError = false, bool isSuccess = false)
        {
            if (statusText != null)
            {
                statusText.text = message;
                
                // Изменение цвета в зависимости от типа сообщения
                if (isError)
                {
                    statusText.color = Color.red;
                }
                else if (isSuccess)
                {
                    statusText.color = Color.green;
                }
                else
                {
                    statusText.color = Color.white;
                }
            }
        }

        private void UpdatePurchaseHistoryText()
        {
            if (purchaseHistoryText != null)
            {
                string history = "История покупок:\n";
                int startIndex = Math.Max(0, purchaseHistory.Count - 10); // Показываем последние 10 записей
                
                for (int i = startIndex; i < purchaseHistory.Count; i++)
                {
                    history += purchaseHistory[i] + "\n";
                }
                
                purchaseHistoryText.text = history;
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (paymentsModule != null)
            {
                PaymentsModule.PurchaseSuccessEvent -= OnPurchaseSuccess;
                PaymentsModule.PurchaseFailedEvent -= OnPurchaseFailed;
                PaymentsModule.OnProductUpdated -= OnProductUpdated;
            }
        }
    }
} 