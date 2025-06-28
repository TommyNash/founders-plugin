// data.js

var player;
var ysdk;

// Ждем инициализации Yandex SDK
if (window.YandexSDK) {
    initYandexPlayer();
} else {
    document.addEventListener('YandexSDKInitialized', initYandexPlayer);
}

// Функция для инициализации Yandex Player и SafeStorage для iOS
function initYandexPlayer() {
    ysdk = window.YandexSDK;
    var moduleTag  = "DataModule";

    if (ysdk) {
        // SafeStorage для iOS (iframe)
        ysdk.getStorage().then(safeStorage => {
            Object.defineProperty(window, 'localStorage', { get: () => safeStorage });
            logMessage('SafeStorage enabled for iOS', 'info', moduleTag);
        }).catch(() => {});

        ysdk.getPlayer().then(_player => {
            player = _player;
            logMessage('Yandex Player initialized successfully', 'success', moduleTag);

            // Загрузка сохранений из облака
            YandexLoadData();

        }).catch(err => {
            logMessage('Error initializing Yandex Player: ' + err, 'error', moduleTag);
        });
    } else {
        logMessage('Yandex SDK is not initialized.', 'error', moduleTag);
    }
}

// Ограничения: не более ~100 запросов за 5 минут!

function YandexSaveData(json) {
    var moduleTag  = "DataModule";
    if (player) {
        // Удалена проверка кэша
        let timeout = setTimeout(() => {
            logMessage('Timeout when saving data on the Yandex server', 'error', moduleTag);
        }, 15000);
        player.getData(['gameData']).then(data => {
            if (data.gameData !== json) {
                player.setData({ gameData: json }, true).then(() => {
                    clearTimeout(timeout);
                    logMessage('The data is stored on the Yandex server', 'success', moduleTag);
                }).catch(err => {
                    clearTimeout(timeout);
                    logMessage('Error when saving data on the Yandex server: ' + err, 'error', moduleTag);
                });
            } else {
                clearTimeout(timeout);
                logMessage('The data has not changed, no saving is required', 'info', moduleTag);
            }
        }).catch(err => {
            clearTimeout(timeout);
            logMessage('Error in retrieving previous data: ' + err, 'error', moduleTag);
        });
    } else {
        logMessage('The player is not initialized.', 'error', moduleTag);
    }
}

function YandexLoadData() {
    var moduleTag  = "DataModule";
    if (player) {
        let timeout = setTimeout(() => {
            logMessage('Timeout when loading data from Yandex server', 'error', moduleTag);
        }, 15000);
        player.getData(['gameData']).then(data => {
            clearTimeout(timeout);
            if (data.gameData) {
                waitForUnityInstance(() => {
                    unityInstance.SendMessage('DataModule', 'OnYandexDataLoaded', data.gameData);
                    logMessage('Data loaded from Yandex server', 'success', moduleTag);
                });
            } else {
                logMessage('No data found on Yandex server.', 'warning', moduleTag);
            }
        }).catch(err => {
            clearTimeout(timeout);
            logMessage('Error loading data from Yandex server: ' + err, 'error', moduleTag);
        });
    } else {
        logMessage('Player is not initialized.', 'error', moduleTag);
    }
}

function YandexDeleteData() {
    var moduleTag  = "DataModule";
    if (player) {
        let timeout = setTimeout(() => {
            logMessage('Timeout when deleting data from Yandex server', 'error', moduleTag);
        }, 15000);
        player.setData({ gameData: null }, true).then(() => {
            clearTimeout(timeout);
            logMessage('Data deleted from Yandex server', 'success', moduleTag);
            waitForUnityInstance(() => {
                unityInstance.SendMessage('DataModule', 'OnYandexDataDeleted');
            });
        }).catch(err => {
            clearTimeout(timeout);
            logMessage('Error deleting data from Yandex server: ' + err, 'error', moduleTag);
        });
    } else {
        logMessage('Player is not initialized.', 'error', moduleTag);
    }
}