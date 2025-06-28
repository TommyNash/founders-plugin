using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using FoundersKit.Modules;
using FoundersKit.Logging;

/// <summary>
/// UI компонент для отображения информации о товаре и кнопки покупки
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// 1. Добавьте этот компонент на UI GameObject
/// 2. Настройте ссылки на UI элементы в инспекторе:
///    - priceText: TextMeshProUGUI для отображения цены
///    - titleText: TextMeshProUGUI для отображения названия
///    - descriptionText: TextMeshProUGUI для отображения описания
///    - currencyImage: Image для отображения иконки валюты
///    - productImage: Image для отображения изображения товара
///    - buyButton: Button для покупки товара
/// 
/// 3. Установите ID товара (id) и настройте флаги отображения
/// 4. Выберите размер иконки валюты (currencyImageSize: Small, Medium, Svg)
/// 5. Компонент автоматически подпишется на события PaymentsModule
///    и будет обновлять UI при получении данных о товаре
/// 
/// ПОДДЕРЖИВАЕМЫЕ ДАННЫЕ О ТОВАРЕ:
/// - id: идентификатор товара
/// - title: название товара
/// - description: описание товара
/// - imageURI: URL изображения товара
/// - price: стоимость в формате "цена код_валюты"
/// - priceValue: стоимость товара
/// - priceCurrencyCode: код валюты
/// - currencyImageUrl: URL иконки валюты (по умолчанию)
/// - currencyImageUrlSmall: URL маленькой иконки валюты
/// - currencyImageUrlMedium: URL иконки валюты среднего размера
/// - currencyImageUrlSvg: URL векторной иконки валюты
/// 
/// ИНТЕГРАЦИЯ С PaymentsExample:
/// - Используйте этот компонент как префаб в PaymentsExample
/// - PaymentsExample автоматически создаст экземпляры для каждого товара
/// - Настроит ID и параметры отображения
/// - Добавит обработчики событий покупки
/// </summary>
public class PurchaseItemUI : MonoBehaviour
{
    public const string CONTEXT_NAME = "PaymentsModuleUtils";

    [Header("—— ID")]
    public string id;                                           // Идентификатор товара

    [Header("—— Fields")]
    [SerializeField] private TextMeshProUGUI priceText;         // Поле для цены
    [SerializeField] private TextMeshProUGUI titleText;         // Поле для названия
    [SerializeField] private TextMeshProUGUI descriptionText;   // Поле для описания

    [Header("—— Images")]
    [SerializeField] private Image currencyImage;               // Иконка валюты
    [SerializeField] private Image productImage;                // Изображение товара

    [Header("—— Button")]
    [SerializeField] private Button buyButton;                  // Кнопка покупки

    [Header("—— Flag Settings   ")]
    [SerializeField] public bool showPrice = true;              // Отображение цены
    [SerializeField] public bool showCurrencyImage = true;      // Отображение иконки валюты
    [SerializeField] public bool showTitle = false;             // Отображение названия
    [SerializeField] public bool showDescription = false;       // Отображение описания
    [SerializeField] public bool showProductImage = true;       // Отображение изображения товара
    [SerializeField] public bool showBuyButton = true;          // Отображение кнопки покупки

    [Header("—— Currency Image Settings")]
    [SerializeField] public CurrencyImageSize currencyImageSize = CurrencyImageSize.Small; // Размер иконки валюты

    public enum CurrencyImageSize
    {
        Small,      // Маленькая иконка
        Medium,     // Иконка среднего размера
        Svg         // Векторная иконка
    }

    private void Start()
    {
        if (PaymentsModule.Instance != null)
        {
            PaymentsModule.OnProductUpdated += UpdateUI;

            // Обновляем UI, если данные уже загружены
            foreach (var product in PaymentsModule.Instance.productDataList)
            {
                UpdateUI(product);
            }
        }
        else
        {
            StartCoroutine(WaitForManager());
        }

        // Настраиваем кнопку покупки
        SetupBuyButton();
    }

    private void SetupBuyButton()
    {
        if (buyButton != null && showBuyButton)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
            buyButton.gameObject.SetActive(true);
        }
        else if (buyButton != null)
        {
            buyButton.gameObject.SetActive(false);
        }
    }

    private void OnBuyButtonClicked()
    {
        if (!string.IsNullOrEmpty(id))
        {
            Log.Info($"Попытка покупки товара: {id}", CONTEXT_NAME);
            PaymentsModule.BuyItem(id);
        }
        else
        {
            Log.Error("ID товара не установлен!", CONTEXT_NAME);
        }
    }

    private IEnumerator WaitForManager()
    {
        while (PaymentsModule.Instance == null)
        {
            yield return null;
        }

        PaymentsModule.OnProductUpdated += UpdateUI;
    }

    private void OnDisable()
    {
        if (PaymentsModule.Instance != null)
        {
            PaymentsModule.OnProductUpdated -= UpdateUI;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }
    }

    private void UpdateUI(PaymentsModule.ProductData productData)
    {
        // Проверяем, соответствует ли продукт нужному ID
        if (productData.id == id)
        {
            bool isSuccess = true;
            string updateDetails = "Updated: ";
            string failureDetails = "Failed: ";

            // Отдельные проверки для каждого поля
            if (showPrice && priceText != null)
            {
                UpdatePrice(productData.priceValue, productData.priceCurrencyCode);
                updateDetails += "Price, ";
            }
            else if (showPrice)
            {
                failureDetails += "Price, ";
                isSuccess = false;
            }

            if (showTitle && titleText != null)
            {
                UpdateTitle(productData.title);
                updateDetails += "Title, ";
            }
            else if (showTitle)
            {
                failureDetails += "Title, ";
                isSuccess = false;
            }

            if (showDescription && descriptionText != null)
            {
                UpdateDescription(productData.description);
                updateDetails += "Description, ";
            }
            else if (showDescription)
            {
                failureDetails += "Description, ";
                isSuccess = false;
            }

            if (showCurrencyImage && currencyImage != null)
            {
                // Выбираем URL иконки валюты в зависимости от настроенного размера
                string currencyImageUrl = GetCurrencyImageUrl(productData);
                UpdateCurrencyImage(currencyImageUrl);
                updateDetails += "Currency Image, ";
            }
            else if (showCurrencyImage)
            {
                failureDetails += "Currency Image, ";
                isSuccess = false;
            }

            if (showProductImage && productImage != null)
            {
                UpdateProductImage(productData.imageURI);
                updateDetails += "Product Image";
            }
            else if (showProductImage)
            {
                failureDetails += "Product Image";
                isSuccess = false;
            }

            // Выводим только успешный или неудачный результат
            if (isSuccess)
            {
                Log.Success("UI purchase updated successfully", CONTEXT_NAME);
            }
            else
            {
                Log.Error("Error updating ui purchase", CONTEXT_NAME);
            }
        }
    }

    public void UpdatePrice(string priceValue, string currencyCode)
    {
        if (priceText != null)
        {
            priceText.text = $"{priceValue} {currencyCode}";
        }
    }

    public void UpdateTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    public void UpdateDescription(string description)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    public void UpdateCurrencyImage(string url)
    {
        if (currencyImage != null && !string.IsNullOrEmpty(url))
        {
            StartCoroutine(LoadCurrencyIcon(url));
        }
    }

    public void UpdateProductImage(string url)
    {
        if (productImage != null && !string.IsNullOrEmpty(url))
        {
            StartCoroutine(LoadProductImage(url));
        }
    }

    private IEnumerator LoadCurrencyIcon(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            currencyImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Log.Error($"Failed to load currency icon from URL: {url}, Error: {request.error}", CONTEXT_NAME);
        }
    }

    private IEnumerator LoadProductImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            productImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Log.Error($"Failed to load product image from URL: {url}, Error: {request.error}", CONTEXT_NAME);
        }
    }

    // Публичные методы для внешнего управления
    public void SetProductId(string productId)
    {
        id = productId;
    }

    public void SetDisplaySettings(bool showPrice, bool showTitle, bool showDescription, bool showCurrencyImage, bool showProductImage, bool showBuyButton, CurrencyImageSize currencyImageSize = CurrencyImageSize.Small)
    {
        this.showPrice = showPrice;
        this.showTitle = showTitle;
        this.showDescription = showDescription;
        this.showCurrencyImage = showCurrencyImage;
        this.showProductImage = showProductImage;
        this.showBuyButton = showBuyButton;
        this.currencyImageSize = currencyImageSize;
        
        SetupBuyButton();
    }

    private string GetCurrencyImageUrl(PaymentsModule.ProductData productData)
    {
        switch (currencyImageSize)
        {
            case CurrencyImageSize.Small:
                return !string.IsNullOrEmpty(productData.currencyImageUrlSmall) 
                    ? productData.currencyImageUrlSmall 
                    : productData.currencyImageUrl;
            
            case CurrencyImageSize.Medium:
                return !string.IsNullOrEmpty(productData.currencyImageUrlMedium) 
                    ? productData.currencyImageUrlMedium 
                    : productData.currencyImageUrl;
            
            case CurrencyImageSize.Svg:
                return !string.IsNullOrEmpty(productData.currencyImageUrlSvg) 
                    ? productData.currencyImageUrlSvg 
                    : productData.currencyImageUrl;
            
            default:
                return productData.currencyImageUrl;
        }
    }
}
