// paymants.js

let callCount_GetPayments = 0;
let attemptsCount = 3;
let isPaymentsInitialized = false;

// Ждем инициализации Yandex SDK и затем вызываем InitPayments
if (window.YandexSDK) {
    InitPayments();
} else {
    document.addEventListener('YandexSDKInitialized', InitPayments);
}

// Функция для инициализации платежей
function InitPayments() {
    var ysdk = window.YandexSDK;
    var moduleTag = "PaymentsModule";

    if (ysdk) {
        ysdk.getPayments({ signed: true }).then(function (_payments) {
            window.payments = _payments;
            isPaymentsInitialized = true;
            logMessage("Payments initialized successfully", "success", moduleTag);
            GetCatalogAndUpdateUI(); // Вызываем функцию после инициализации платежей
        }).catch(function (err) {
            logMessage("Failed to initialize payments system: " + err, "error", moduleTag);
        });
    } else if (callCount_GetPayments < attemptsCount) {
        callCount_GetPayments++;
        setTimeout(InitPayments, 1000); // Повторная попытка через 1 секунду
    } else {
        logMessage("Yandex SDK is not available", "error", moduleTag);
    }
}

// Функция для получения каталога и обновления UI
function GetCatalogAndUpdateUI() {

    if (typeof payments !== 'undefined') {
        payments.getCatalog().then(products => {
            let productID = [];
            let title = [];
            let description = [];
            let imageURI = [];
            let price = [];
            let priceValue = [];
            let priceCurrencyCode = [];
            let currencyImageUrl = [];
            let currencyImageUrlSmall = [];
            let currencyImageUrlMedium = [];
            let currencyImageUrlSvg = [];

            payments.getPurchases().then(purchases => {
                products.forEach((product, i) => {
                    productID[i] = product.id || 'undefined';
                    title[i] = product.title || 'No title';
                    description[i] = product.description || '';
                    imageURI[i] = product.imageURI || '';
                    price[i] = product.price || '';
                    priceValue[i] = product.priceValue || '';
                    priceCurrencyCode[i] = product.priceCurrencyCode || '';
                    
                    // Получаем иконки валют разных размеров
                    currencyImageUrl[i] = product.getPriceCurrencyImage() || '';
                    currencyImageUrlSmall[i] = product.getPriceCurrencyImage('small') || '';
                    currencyImageUrlMedium[i] = product.getPriceCurrencyImage('medium') || '';
                    currencyImageUrlSvg[i] = product.getPriceCurrencyImage('svg') || '';

                    // Проверка на наличие покупок
                    let isConsumed = true;
                    purchases.forEach(purchase => {
                        if (purchase.productID === productID[i]) {
                            isConsumed = false;
                        }
                    });

                    const productData = {
                        id: productID[i],
                        title: title[i],
                        description: description[i],
                        imageURI: imageURI[i],
                        price: price[i],
                        priceValue: priceValue[i],
                        priceCurrencyCode: priceCurrencyCode[i],
                        currencyImageUrl: currencyImageUrl[i],
                        currencyImageUrlSmall: currencyImageUrlSmall[i],
                        currencyImageUrlMedium: currencyImageUrlMedium[i],
                        currencyImageUrlSvg: currencyImageUrlSvg[i]
                    };

                    waitForUnityInstance(() => {
                        if (productID[i] && productID[i] !== 'undefined') {
                            unityInstance.SendMessage("PaymentsModule", 'UpdateProductUI', JSON.stringify(productData));
                        } else {
                            logMessage('Product ID is empty or undefined, skipping Unity call', 'error', "PaymentsModule");
                        }
                    });
                });
            });
        }).catch(function (err) {
            logMessage("Failed to get catalog: " + err, "error", "PaymentsModule");
        });
    } else {
        logMessage("Yandex SDK is not available", "error", "PaymentsModule");
    }
}

// Функция для отправки информации о покупках
function SendPurchasesInfo(purchases) {
    if (purchases && purchases.length > 0) {
        const purchasesInfo = {
            count: purchases.length,
            items: purchases.map(purchase => ({
                productID: purchase.productID,
                purchaseToken: purchase.purchaseToken,
                developerPayload: purchase.developerPayload || ''
            }))
        };
        
        waitForUnityInstance(() => {
            unityInstance.SendMessage("PaymentsModule", 'UpdatePurchasesInfo', JSON.stringify(purchasesInfo));
            logMessage('Purchases info sent to Unity: ' + purchases.length + ' purchases', 'info', "PaymentsModule");
        });
    } else {
        logMessage('No purchases found', 'info', "PaymentsModule");
    }
}

// Функция для покупки товара
function BuyItem_js(itemId) {
    if (typeof payments !== 'undefined' && isPaymentsInitialized) {
        logMessage("Attempting to buy item: " + itemId, "info", "PaymentsModule");
        
        payments.purchase({ id: itemId }).then(purchase => {
            logMessage("Purchase successful: " + itemId, "success", "PaymentsModule");
            
            waitForUnityInstance(() => {
                unityInstance.SendMessage("PaymentsModule", 'OnPurchaseSuccessWrapper', itemId);
            });
            
        }).catch(err => {
            logMessage("Purchase failed: " + itemId + " - " + err, "error", "PaymentsModule");
            
            waitForUnityInstance(() => {
                unityInstance.SendMessage("PaymentsModule", 'OnPurchaseFailedWrapper', itemId);
            });
        });
    } else {
        logMessage("Payments system is not initialized", "error", "PaymentsModule");
        
        waitForUnityInstance(() => {
            unityInstance.SendMessage("PaymentsModule", 'OnPurchaseFailedWrapper', itemId);
        });
    }
}

// Функция для обработки необработанных покупок
function ConsumePurchases() {
    if (typeof payments !== 'undefined' && isPaymentsInitialized) {
        logMessage("Checking for unprocessed purchases...", "info", "PaymentsModule");
        
        payments.getPurchases().then(purchases => {
            if (purchases && purchases.length > 0) {
                logMessage("Found " + purchases.length + " unprocessed purchases", "info", "PaymentsModule");
                
                purchases.forEach(purchase => {
                    // Здесь можно добавить логику для обработки покупок
                    logMessage("Processing purchase: " + purchase.productID, "info", "PaymentsModule");
                    
                    // Отправляем информацию о покупке в Unity
                    waitForUnityInstance(() => {
                        unityInstance.SendMessage("PaymentsModule", 'ProcessPurchase', JSON.stringify(purchase));
                    });
                });
            } else {
                logMessage("No unprocessed purchases found", "info", "PaymentsModule");
            }
        }).catch(err => {
            logMessage("Failed to get purchases for consumption: " + err, "error", "PaymentsModule");
        });
    } else {
        logMessage("Payments system is not initialized for consumption", "error", "PaymentsModule");
    }
}

// Функция для обновления каталога
function RefreshCatalog() {
    if (isPaymentsInitialized) {
        logMessage("Refreshing catalog...", "info", "PaymentsModule");
        GetCatalogAndUpdateUI();
    } else {
        logMessage("Cannot refresh catalog - payments not initialized", "error", "PaymentsModule");
    }
}

// Функция для получения детальной информации о товаре
function GetProductDetails(productId) {
    if (typeof payments !== 'undefined' && isPaymentsInitialized) {
        payments.getCatalog().then(products => {
            const product = products.find(p => p.id === productId);
            if (product) {
                const details = {
                    id: product.id,
                    title: product.title,
                    description: product.description,
                    priceValue: product.priceValue,
                    currencyCode: product.priceCurrencyCode,
                    currencyImageUrl: product.getPriceCurrencyImage ? product.getPriceCurrencyImage() : '',
                    fullPrice: product.price
                };
                
                waitForUnityInstance(() => {
                    unityInstance.SendMessage("PaymentsModule", 'UpdateProductDetails', JSON.stringify(details));
                });
            }
        });
    }
}

// Функция для ожидания инициализации Unity
function waitForUnityInstance(callback) {
    if (typeof unityInstance !== 'undefined') {
        callback();
    } else {
        setTimeout(() => waitForUnityInstance(callback), 100);
    }
}