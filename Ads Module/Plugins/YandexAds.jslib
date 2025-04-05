// YandexAds.jslib

mergeInto(LibraryManager.library, {
    ShowFullscreenAd_js: function() {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showFullscreenAdv({
                callbacks: {
                    onOpen: function() {
                        logMessage('Fullscreen ad opened', 'info', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnFullAdOpened");
                    },
                    onClose: function(wasShown) {
                        logMessage('Fullscreen ad closed. Was shown: ' + wasShown, 'info', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnFullAdClosed", wasShown ? "true" : "false");
                    },
                    onError: function(error) {
                        logMessage('Error while showing fullscreen ad: ' + error, 'error', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnFullAdError");
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

    ShowRewardedVideo_js: function(id) {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showRewardedVideo({
                callbacks: {
                    onOpen: function() {
                        logMessage('Rewarded video ad opened', 'info', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnRewardedVideoAdOpened");
                    },
                    onRewarded: function() {
                        logMessage('Reward received for video ad', 'success', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnVideoAdRewarded", id);
                    },
                    onClose: function() {
                        logMessage('Rewarded video ad closed', 'info', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnRewardedVideoAdClosed");
                    },
                    onError: function(error) {
                        logMessage('Error while showing rewarded video ad: ' + error, 'error', 'YandexAds');
                        gameInstance.SendMessage("AdsManager", "OnRewardedVideoAdError");
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', 'YandexAds');
        }
    }
});

