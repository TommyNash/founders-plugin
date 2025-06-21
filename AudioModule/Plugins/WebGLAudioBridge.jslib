mergeInto(LibraryManager.library, {
    WebAudio_InitializeAudioSystem: function(volume) {
        if (!WebAudio) {
            console.error('WebAudio system not found');
            return;
        }
        WebAudio.InitializeAudioSystem(volume);
    },

    WebAudio_PlaySound: function(keyPtr, startTime, volume, pitch, priority, positionPtr) {
        if (!WebAudio) return -1;
        
        var key = UTF8ToString(keyPtr);
        var position = null;
        
        if (positionPtr) {
            var positionData = new Float32Array(HEAPF32.buffer, positionPtr, 3);
            position = {
                x: positionData[0],
                y: positionData[1],
                z: positionData[2]
            };
        }
        
        return WebAudio.PlaySound(key, startTime, volume, pitch, priority, position);
    },

    WebAudio_StopSound: function(keyPtr) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.StopSound(key);
    },

    WebAudio_StopAll: function() {
        if (!WebAudio) return;
        WebAudio.StopAll();
    },

    WebAudio_PauseSound: function(keyPtr) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.PauseSound(key);
    },

    WebAudio_PauseAll: function() {
        if (!WebAudio) return;
        WebAudio.PauseAll();
    },

    WebAudio_UnpauseSound: function(keyPtr) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.UnpauseSound(key);
    },

    WebAudio_UnpauseAll: function() {
        if (!WebAudio) return;
        WebAudio.UnpauseAll();
    },

    WebAudio_SetGlobalVolume: function(volume) {
        if (!WebAudio) return;
        WebAudio.SetGlobalVolume(volume);
    },

    WebAudio_SetPosition: function(keyPtr, x, y, z) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.SetPosition(key, {x: x, y: y, z: z});
    },

    WebAudio_IsUserInteracted: function() {
        return WebAudio ? WebAudio.isUserInteracted : false;
    },

    WebAudio_GetTime: function(keyPtr) {
        if (!WebAudio) return 0;
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetTime(key);
    },

    WebAudio_SetPitch: function(keyPtr, pitch) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.SetPitch(key, pitch);
    },

    WebAudio_SetTime: function(keyPtr, time) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.SetTime(key, time);
    },

    WebAudio_SetLoop: function(keyPtr, loop) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.SetLoop(key, loop);
    },

    WebAudio_SetCallback: function(type, callback) {
        var typeStr = UTF8ToString(type);
        switch(typeStr) {
            case "loaded":
                WebAudio.onSoundLoaded = function(key, success) {
                    try {
                        var keyPtr = _malloc(key.length + 1);
                        stringToUTF8(key, keyPtr, key.length + 1);
                        callback(keyPtr, success ? 1 : 0);
                        _free(keyPtr);
                    } catch (e) {
                        console.warn('Error in sound loaded callback:', e);
                    }
                };
                break;
            case "complete":
                WebAudio.onSoundComplete = function(key) {
                    try {
                        var keyPtr = _malloc(key.length + 1);
                        stringToUTF8(key, keyPtr, key.length + 1);
                        callback(keyPtr);
                        _free(keyPtr);
                    } catch (e) {
                        console.warn('Error in sound complete callback:', e);
                    }
                };
                break;
        }
    },

    WebAudio_SetErrorCallback: function(callback) {
        WebAudio.onSoundError = function(key, error) {
            try {
                var keyPtr = _malloc(key.length + 1);
                var errorPtr = _malloc(error.length + 1);
                stringToUTF8(key, keyPtr, key.length + 1);
                stringToUTF8(error, errorPtr, error.length + 1);
                callback(keyPtr, errorPtr);
                _free(keyPtr);
                _free(errorPtr);
            } catch (e) {
                console.warn('Error in sound error callback:', e);
            }
        };
    },

    WebAudio_SetMuteCallback: function(callback) {
        WebAudio.onMuteChanged = function(isMuted) {
            try {
                callback(isMuted ? 1 : 0);
            } catch (e) {
                console.warn('Error in mute changed callback:', e);
            }
        };
    },

    WebAudio_SetVolume: function(keyPtr, volume) {
        if (!WebAudio) return;
        var key = UTF8ToString(keyPtr);
        WebAudio.SetVolume(key, volume);
    },

    WebAudio_GetVolume: function(keyPtr) {
        if (!WebAudio) return 0;
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetVolume(key);
    },

    WebAudio_GetLoop: function(keyPtr) {
        if (!WebAudio) return false;
        var key = UTF8ToString(keyPtr);
        return WebAudio.GetLoop(key);
    },

    WebAudio_IsSoundLoaded: function(keyPtr) {
        if (!WebAudio) return false;
        var key = UTF8ToString(keyPtr);
        return WebAudio.IsSoundLoaded(key);
    },

    WebAudio_SetListenerPosition: function(x, y, z) {
        if (!WebAudio || !WebAudio.listener) return;
        WebAudio.SetListenerPosition(x, y, z);
    },

    WebAudio_SetListenerOrientation: function(forwardX, forwardY, forwardZ, upX, upY, upZ) {
        if (!WebAudio || !WebAudio.listener) return;
        WebAudio.SetListenerOrientation(
            0, 0, 0,  // position
            forwardX, forwardY, forwardZ,  // forward vector
            upX, upY, upZ  // up vector
        );
    },

    WebAudio_HandleSoundLoaded: function(keyPtr, success) {
        var key = UTF8ToString(keyPtr);
        if (WebAudio.onSoundLoaded) {
            try {
                WebAudio.onSoundLoaded(key, success === 1);
            } catch (e) {
                console.warn('Error in sound loaded callback:', e);
            }
        }
    },

    WebAudio_HandleSoundError: function(keyPtr, errorPtr) {
        var key = UTF8ToString(keyPtr);
        var error = UTF8ToString(errorPtr);
        if (WebAudio.onSoundError) {
            try {
                WebAudio.onSoundError(key, error);
            } catch (e) {
                console.warn('Error in sound error callback:', e);
            }
        }
    },

    WebAudio_HandleSoundComplete: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        if (WebAudio.onSoundComplete) {
            try {
                WebAudio.onSoundComplete(key);
            } catch (e) {
                console.warn('Error in sound complete callback:', e);
            }
        }
    },

    WebAudio_HandleMuteChanged: function(isMuted) {
        if (WebAudio.onMuteChanged) {
            try {
                WebAudio.onMuteChanged(isMuted === 1);
            } catch (e) {
                console.warn('Error in mute changed callback:', e);
            }
        }
    }
}); 