
const moduleTag = "AdsModule";

mergeInto(LibraryManager.library, {
    ShowFullscreenAd_js: function () {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showFullscreenAdv({
                callbacks: {
                    onOpen: function () {
                        logMessage('Fullscreen ad opened', 'info', moduleTag);
                        SendMessage("AdsModule", "OnFullAdOpen");
                    },
                    onClose: function (wasShown) {
                        logMessage('Fullscreen ad closed. Was shown: ' + wasShown, 'info', moduleTag);
                        SendMessage("AdsModule", "OnFullAdClose", wasShown ? "true" : "false");
                    },
                    onError: function (error) {
                        logMessage('Error while showing fullscreen ad: ' + error, 'error', moduleTag);
                        SendMessage("AdsModule", "OnFullAdError");
                    },
                    onOffline: function () {
                        logMessage('No network connection', 'warning', moduleTag);
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', moduleTag);
        }
    },

    ShowRewardedVideo_js: function (id) {
        var ysdk = window.YandexSDK;

        if (ysdk && ysdk.adv) {
            ysdk.adv.showRewardedVideo({
                callbacks: {
                    onOpen: function () {
                        logMessage('Rewarded video ad opened', 'info', moduleTag);
                        SendMessage("AdsModule", "OnRewardedOpen");
                    },
                    onRewarded: function () {
                        logMessage('Reward received for video ad', 'success', moduleTag);
                        SendMessage("AdsModule", "OnRewarded", id);
                    },
                    onClose: function () {
                        logMessage('Rewarded video ad closed', 'info', moduleTag);
                        SendMessage("AdsModule", "OnRewardedClose");
                    },
                    onError: function (error) {
                        logMessage('Error while showing rewarded video ad: ' + error, 'error', moduleTag);
                        SendMessage("AdsModule", "OnRewardedError");
                    }
                }
            });
        } else {
            logMessage('Yandex SDK is not initialized', 'error', moduleTag);
        }
    }
});