mergeInto(LibraryManager.library, {
    WebAudio_InitializeAudioSystem: function(volume) {
        WebAudio.InitializeAudioSystem(volume);
    },

    WebAudio_SetGlobalVolume: function(volume) {
        WebAudio.SetGlobalVolume(volume);
    },

    WebAudio_StopAll: function() {
        WebAudio.StopAll();
    },

    WebAudio_PauseAll: function() {
        WebAudio.PauseAll();
    },

    WebAudio_UnpauseAll: function() {
        WebAudio.UnpauseAll();
    },

    WebAudio_PlaySound: function(keyPtr, startTime, volume, pitch, priority, posX, posY, posZ) {
        var key = UTF8ToString(keyPtr);
        var position = { x: posX, y: posY, z: posZ };
        WebAudio.PlaySound(key, startTime, volume, pitch, priority, position);
    },

    WebAudio_StopSound: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        WebAudio.StopSound(key);
    },

    WebAudio_PauseSound: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        WebAudio.PauseSound(key);
    },

    WebAudio_UnpauseSound: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        WebAudio.UnpauseSound(key);
    },

    WebAudio_SetVolume: function(keyPtr, volume) {
        var key = UTF8ToString(keyPtr);
        WebAudio.SetVolume(key, volume);
    },

    WebAudio_SetPitch: function(keyPtr, pitch) {
        var key = UTF8ToString(keyPtr);
        WebAudio.SetPitch(key, pitch);
    },

    WebAudio_GetTime: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetTime(key);
    },

    WebAudio_SetTime: function(keyPtr, time) {
        var key = UTF8ToString(keyPtr);
        WebAudio.SetTime(key, time);
    },

    WebAudio_SetPosition: function(keyPtr, x, y, z) {
        var key = UTF8ToString(keyPtr);
        WebAudio.SetPosition(key, x, y, z);
    },

    WebAudio_SetListenerPosition: function(x, y, z) {
        WebAudio.SetListenerPosition(x, y, z);
    },

    WebAudio_SetListenerOrientation: function(x, y, z) {
        WebAudio.SetListenerOrientation(x, y, z);
    },

    WebAudio_SetCallback: function(typePtr, callback) {
        var type = UTF8ToString(typePtr);
        switch (type) {
            case "loaded":
                WebAudio.onSoundLoaded = callback;
                break;
            case "complete":
                WebAudio.onSoundComplete = callback;
                break;
        }
    },

    WebAudio_SetErrorCallback: function(callback) {
        WebAudio.onSoundError = callback;
    },

    WebAudio_SetMuteCallback: function(callback) {
        WebAudio.onMuteChanged = callback;
    },

    WebAudio_IsUserInteracted: function() {
        return WebAudio.isUserInteracted;
    },

    WebAudio_SetLoop: function(keyPtr, loop) {
        var key = UTF8ToString(keyPtr);
        WebAudio.SetLoop(key, loop);
    },

    WebAudio_GetLoop: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetLoop(key);
    },

    WebAudio_GetVolume: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetVolume(key);
    },

    WebAudio_IsSoundLoaded: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return WebAudio.IsSoundLoaded(key);
    }
}); 