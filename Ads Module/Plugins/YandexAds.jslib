// YandexAds.jslib

mergeInto(LibraryManager.library, {
    ShowFullscreenAd_js: function(controllerName) {
        var controller = UTF8ToString(controllerName);
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showFullscreenAdv({
                callbacks: {
                    onOpen: function() {
                        logMessage('Fullscreen ad opened', 'info', 'YandexAds');
                        Unity.SendMessage(controller, "OnFullAdOpened", "");
                    },
                    onClose: function(wasShown) {
                        logMessage('Fullscreen ad closed. Was shown: ' + wasShown, 'info', 'YandexAds');
                        Unity.SendMessage(controller, "OnFullAdClosed", wasShown ? "true" : "false");
                    },
                    onError: function(error) {
                        logMessage('Error while showing fullscreen ad: ' + error, 'error', 'YandexAds');
                        Unity.SendMessage(controller, "OnFullAdError", "");
                    },
                    onOffline: function() {
                        logMessage('No network connection', 'warning', 'YandexAds');
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', 'YandexAds');
        }
    },

    ShowRewardedVideo_js: function(controllerName, id) {
        var controller = UTF8ToString(controllerName);
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showRewardedVideo({
                callbacks: {
                    onOpen: function() {
                        logMessage('Rewarded video ad opened', 'info', 'YandexAds');
                        Unity.SendMessage(controller, "OnRewardedVideoAdOpened", "");
                    },
                    onRewarded: function() {
                        logMessage('Reward received for video ad', 'success', 'YandexAds');
                        Unity.SendMessage(controller, "OnVideoAdRewarded", id);
                    },
                    onClose: function() {
                        logMessage('Rewarded video ad closed', 'info', 'YandexAds');
                        Unity.SendMessage(controller, "OnRewardedVideoAdClosed", "");
                    },
                    onError: function(error) {
                        logMessage('Error while showing rewarded video ad: ' + error, 'error', 'YandexAds');
                        Unity.SendMessage(controller, "OnRewardedVideoAdError", "");
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', 'YandexAds');
        }
    }
});

