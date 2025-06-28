// YandexData.jslib

mergeInto(LibraryManager.library, {
    YandexSaveData: function(jsonPtr) {
        var jsonString = UTF8ToString(jsonPtr);
        var moduleTag  = "DataModule .jslib";

        if (typeof YandexSaveData === 'function') {
            YandexSaveData(jsonString);
        } else {
            logMessage('YandexSaveData is not defined', 'error', moduleTag);
        }
    },

    YandexLoadData: function() {
        var moduleTag  = "DataModule .jslib";

        if (typeof YandexLoadData === 'function') {
            YandexLoadData();
        } else {
             logMessage('YandexLoadData is not defined', 'error', moduleTag);
        }
    },

    YandexDeleteData: function() {
        var moduleTag  = "DataModule .jslib";

        if (typeof YandexDeleteData === 'function') {
            YandexDeleteData();
        } else {
            logMessage('YandexDeleteData is not defined', 'error', moduleTag);
        }
    },
});
