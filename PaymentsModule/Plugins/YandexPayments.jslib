 // YandexPayments.jslib

mergeInto(LibraryManager.library, {
    // Метод для покупки предмета
    BuyItem_js: function (itemIdPtr) {
        var itemId = UTF8ToString(itemIdPtr);
        var moduleTag  = "PaymentsModule";

        if (typeof payments !== 'undefined') {
            payments.purchase({ id: itemId }).then(function(purchase) {
                logMessage('Purchase successful', "success", moduleTag);

                // Вызов метода в Unity на успешную покупку
                unityInstance.SendMessage(moduleTag, 'OnPurchaseSuccessWrapper', itemId);

                // После успешной покупки поглощаем её
                payments.consumePurchase(purchase.purchaseToken).then(function() {
                    logMessage('Purchase consumed', "success", moduleTag);
                }).catch(function(err) {
                    logMessage('Failed to consume purchase: ' + itemId + " Error: " + err, "error", moduleTag);
                });

            }).catch(function(err) {
                logMessage('Purchase failed for item: ' + itemId + " Error: " + err, "error", moduleTag);
                unityInstance.SendMessage(moduleTag, 'OnPurchaseFailedWrapper', itemId);
            });
        } else {
            logMessage("Payments system not initialized", "error", moduleTag);
        }
    },

    // Метод для обработки необработанных покупок
    ConsumePurchases: function() {

        var moduleTag  = "PaymentsModule";
        
        if (typeof payments !== 'undefined') {
            payments.getPurchases().then(function(purchases) {
                if (purchases.length > 0) {
                    logMessage('Unprocessed purchases: ' + purchases.length, "info", moduleTag);
                    purchases.forEach(function(purchase) {
                        // Отправляем покупку в Unity для обработки
                        unityInstance.SendMessage(moduleTag, 'OnPurchaseSuccessWrapper', purchase.productID);

                        // Поглощаем покупку после обработки
                        payments.consumePurchase(purchase.purchaseToken).then(function() {
                            logMessage('Purchase consumed: ' + purchases.length, "success", moduleTag);
                        }).catch(function(err) {
                            logMessage('Failed to consume purchase: ' + purchase.productID + " Error: " + err, "error", moduleTag);
                        });
                    });
                } else {
                    logMessage('No unprocessed purchases found', "info", moduleTag);
                }
            }).catch(function(err) {
                logMessage('Error retrieving purchases: ' + err, "error", moduleTag);
            });
        } else {
            logMessage("Payments system not initialized", "error", moduleTag);
        }
    },
});