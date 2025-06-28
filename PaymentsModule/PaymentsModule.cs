using System;
using System.Collections.Generic;
using UnityEngine;
using FoundersKit.Logging;
using System.Runtime.InteropServices;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Модуль 
    /// </summary>
    public class PaymentsModule : MonoBehaviour
    {
        public const string CONTEXT_NAME = "PaymentsModule";

        #region Singleton
        private static PaymentsModule instance;
        public static PaymentsModule Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PaymentsModule");
                    instance = go.AddComponent<PaymentsModule>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Event Delegates
        public static event Action<string> PurchaseSuccessEvent;
        public static event Action<string> PurchaseFailedEvent;
        public static event Action<ProductData> OnProductUpdated;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // Инициализируем обработчик покупок
            PurchaseHandler.Initialize();

            // Проверка необработанных покупок
            CheckConsumePurchases();
        }
        #endregion

        #region In-app purchases


        // Проверка необработанных покупок
        public void CheckConsumePurchases()
        {
#if !UNITY_EDITOR
                    // Проверка и обработка неоплаченных покупок при запуске игры
                    ConsumePurchases();
#else
                Log.Info("Request for ConsumePurchases is completed", CONTEXT_NAME);
#endif
        }

        // Покупка предмета
        public static void BuyItem(string itemId)
        {
#if !UNITY_EDITOR
            Log.Info("Attempting to buy item", CONTEXT_NAME);
            BuyItem_js(itemId);  // Вызов JavaScript функции для WebGL напрямую
#else
            // Симуляция успешной покупки в редакторе Unity
            Log.Info("Simulating purchase for item: " + itemId, CONTEXT_NAME, scope: LogScope.Local);
            OnPurchaseSuccess(itemId);  // Вызываем успешную покупку сразу
    #endif
        }

        // Обработка успешной покупки
        // Обработка успешной покупки
        public static void OnPurchaseSuccess(string itemId)
        {
#if UNITY_EDITOR
            Log.Success("Simulating purchase for item: " + itemId, CONTEXT_NAME, scope: LogScope.Local);
#endif

            // Вызываем событие успешной покупки - PurchaseHandler подпишется на него
            PurchaseSuccessEvent?.Invoke(itemId);
        }

        // Обработка неудачной покупки
        public static void OnPurchaseFailed(string itemId)
        {
            Log.Error("Purchase failed for item: " + itemId, CONTEXT_NAME, scope: LogScope.Local);

            // Вызов события неудачной покупки
            PurchaseFailedEvent?.Invoke(itemId);
        }

        // Нестатические методы-обертки для вызова из JavaScript через SendMessage
        public void OnPurchaseSuccessWrapper(string itemId)
        {
            OnPurchaseSuccess(itemId);
        }

        public void OnPurchaseFailedWrapper(string itemId)
        {
            OnPurchaseFailed(itemId);
        }

#endregion

        #region Portal Currency

        public List<ProductData> productDataList = new List<ProductData>();  // Сохраняем данные о товарах

        [System.Serializable]
        public class ProductData
        {
            public string id;                // Идентификатор товара
            public string title;             // Название товара
            public string description;       // Описание товара
            public string imageURI;          // URL изображения товара
            public string price;             // Стоимость товара в формате "цена код_валюты"
            public string priceValue;        // Стоимость товара в формате "цена"
            public string priceCurrencyCode; // Код валюты
            public string currencyImageUrl;  // URL иконки валюты (устаревшее поле, оставлено для совместимости)
            public string currencyImageUrlSmall;   // URL маленькой иконки валюты
            public string currencyImageUrlMedium;  // URL иконки валюты среднего размера
            public string currencyImageUrlSvg;     // URL векторной иконки валюты
        }


        public void UpdateProductUI(string productJson)
        {
            // Разбор JSON в объект ProductData
            ProductData productData = JsonUtility.FromJson<ProductData>(productJson);

            // Проверка наличия id продукта
            if (!string.IsNullOrEmpty(productData.id))
            {
                productDataList.Add(productData);  // Сохранение данных

                OnProductUpdated?.Invoke(productData);  // Триггер события обновления продукта
            }
            else
            {
                Log.Error("Product ID is empty or undefined!", CONTEXT_NAME);
            }
        }

        // Метод для передачи данных новому объекту UI
        public void UpdateNewUI(PurchaseItemUI uiComponent)
        {
            foreach (var product in productDataList)
            {
                if (product.id == uiComponent.id)
                {
                    // Обновляем поля, используя имеющиеся методы для обновления UI
                    if (uiComponent.showPrice)
                    {
                        // Используем priceValue и priceCurrencyCode для отображения цены
                        uiComponent.UpdatePrice(product.priceValue, product.priceCurrencyCode);
                    }
                    if (uiComponent.showTitle)
                    {
                        uiComponent.UpdateTitle(product.title);
                    }
                    if (uiComponent.showDescription)
                    {
                        uiComponent.UpdateDescription(product.description);
                    }
                    if (uiComponent.showCurrencyImage)
                    {
                        // Используем маленькую иконку по умолчанию, если доступна
                        string currencyImageUrl = !string.IsNullOrEmpty(product.currencyImageUrlSmall) 
                            ? product.currencyImageUrlSmall 
                            : product.currencyImageUrl;
                        uiComponent.UpdateCurrencyImage(currencyImageUrl);
                    }
                    if (uiComponent.showProductImage)
                    {
                        uiComponent.UpdateProductImage(product.imageURI);
                    }
                }
            }
        }

        #endregion

        #region Native JS Interfaces
        [DllImport("__Internal")]
        private static extern void BuyItem_js(string itemId);

        [DllImport("__Internal")]
        private static extern void ConsumePurchases();
        #endregion
    }
}