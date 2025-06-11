// YandexAds.jslib

mergeInto(LibraryManager.library, {
    ShowFullscreenAd_js: function () {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showFullscreenAdv({
                callbacks: {
                    onOpen: function () {
                        logMessage('Fullscreen ad opened', 'info', 'YandexAds');
                        SendMessage("AdsModule", "OnFullAdOpen");
                    },
                    onClose: function (wasShown) {
                        logMessage('Fullscreen ad closed. Was shown: ' + wasShown, 'info', 'YandexAds');
                        SendMessage("AdsModule", "OnFullAdClose", wasShown ? "true" : "false");
                    },
                    onError: function (error) {
                        logMessage('Error while showing fullscreen ad: ' + error, 'error', 'YandexAds');
                        SendMessage("AdsModule", "OnFullAdError");
                    },
                    onOffline: function () {
                        logMessage('No network connection', 'warning', 'YandexAds');
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', 'YandexAds');
        }
    },

    ShowRewardedVideo_js: function (id) {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showRewardedVideo({
                callbacks: {
                    onOpen: function () {
                        logMessage('Rewarded video ad opened', 'info', 'YandexAds');
                        SendMessage("AdsModule", "OnRewardedOpen");
                    },
                    onRewarded: function () {
                        logMessage('Reward received for video ad', 'success', 'YandexAds');
                        SendMessage("AdsModule", "OnRewarded", id);
                    },
                    onClose: function () {
                        logMessage('Rewarded video ad closed', 'info', 'YandexAds');
                        SendMessage("AdsModule", "OnRewardedClose");
                    },
                    onError: function (error) {
                        logMessage('Error while showing rewarded video ad: ' + error, 'error', 'YandexAds');
                        SendMessage("AdsModule", "OnRewardedError");
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', 'YandexAds');
        }
    }
});