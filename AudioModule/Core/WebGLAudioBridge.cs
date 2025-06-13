using UnityEngine;
using System.Runtime.InteropServices;
using System;
using AOT;

namespace FoundersKit.Modules
{
    /// <summary>
    /// Мост для работы с WebGL аудио системой
    /// </summary>
    public static class WebGLAudioBridge
    {
        // События
        public static event Action<string> OnSoundLoaded;
        public static event Action<string, string> OnSoundError;
        public static event Action<string> OnSoundComplete;
        public static event Action<bool> OnMuteChanged;

        // Делегаты для колбэков
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SoundLoadedCallback(string key, bool success);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SoundErrorCallback(string key, string error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SoundCompleteCallback(string key);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MuteChangedCallback(bool isMuted);

        // Состояние
        private static bool isInitialized = false;
        private static float globalVolume = 1f;

        #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebAudio_InitializeAudioSystem(float volume);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetGlobalVolume(float volume);

        [DllImport("__Internal")]
        private static extern void WebAudio_PlaySound(string key, float startTime, float volume, float pitch, int priority, float posX, float posY, float posZ);

        [DllImport("__Internal")]
        private static extern void WebAudio_StopSound(string key);

        [DllImport("__Internal")]
        private static extern void WebAudio_PauseSound(string key);

        [DllImport("__Internal")]
        private static extern void WebAudio_UnpauseSound(string key);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetVolume(string key, float volume);

        [DllImport("__Internal")]
        private static extern void WebAudio_StopAll();

        [DllImport("__Internal")]
        private static extern void WebAudio_PauseAll();

        [DllImport("__Internal")]
        private static extern void WebAudio_UnpauseAll();

        [DllImport("__Internal")]
        private static extern void WebAudio_SetPitch(string key, float pitch);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetTime(string key, float time);

        [DllImport("__Internal")]
        private static extern float WebAudio_GetTime(string key);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetCallback(string type, IntPtr callback);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetErrorCallback(IntPtr callback);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetMuteCallback(IntPtr callback);

        [DllImport("__Internal")]
        private static extern bool WebAudio_IsUserInteracted();

        [DllImport("__Internal")]
        private static extern void WebAudio_SetLoop(string key, bool loop);

        [DllImport("__Internal")]
        private static extern bool WebAudio_GetLoop(string key);

        [DllImport("__Internal")]
        private static extern float WebAudio_GetVolume(string key);

        [DllImport("__Internal")]
        private static extern bool WebAudio_IsSoundLoaded(string key);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetPosition(string key, float x, float y, float z);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetListenerPosition(float x, float y, float z);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetListenerOrientation(float forwardX, float forwardY, float forwardZ, float upX = 0f, float upY = 1f, float upZ = 0f);
        #endif

        public static void InitializeAudioSystem(float volume)
        {
            if (isInitialized)
            {
                Debug.LogWarning("WebGL Audio System уже инициализирована");
                return;
            }

            try
            {
                globalVolume = volume;

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_InitializeAudioSystem(volume);
                
                // Регистрируем колбэки с явным приведением типов
                var loadedCallback = (SoundLoadedCallback)HandleSoundLoadedCallback;
                var errorCallback = (SoundErrorCallback)HandleSoundErrorCallback;
                var completeCallback = (SoundCompleteCallback)HandleSoundCompleteCallback;
                var muteCallback = (MuteChangedCallback)HandleMuteChangedCallback;

                var loadedPtr = Marshal.GetFunctionPointerForDelegate(loadedCallback);
                var errorPtr = Marshal.GetFunctionPointerForDelegate(errorCallback);
                var completePtr = Marshal.GetFunctionPointerForDelegate(completeCallback);
                var mutePtr = Marshal.GetFunctionPointerForDelegate(muteCallback);

                WebAudio_SetCallback("loaded", loadedPtr);
                WebAudio_SetCallback("complete", completePtr);
                WebAudio_SetErrorCallback(errorPtr);
                WebAudio_SetMuteCallback(mutePtr);

                // Сохраняем делегаты, чтобы они не были собраны сборщиком мусора
                GC.KeepAlive(loadedCallback);
                GC.KeepAlive(errorCallback);
                GC.KeepAlive(completeCallback);
                GC.KeepAlive(muteCallback);
                #endif

                isInitialized = true;
                Debug.Log("WebGL Audio System успешно инициализирована");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка инициализации WebGL Audio System: {ex.Message}");
            }
        }

        public static void SetGlobalVolume(float volume)
        {
            try
            {
                globalVolume = Mathf.Clamp01(volume);
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetGlobalVolume(globalVolume);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки глобальной громкости: {ex.Message}");
            }
        }

        public static void PlaySound(string key, float startTime, float volume, float pitch, int priority = 0, Vector3? position = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("WebGL Audio System не инициализирована");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Ключ звука не может быть пустым");
                return;
            }

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                if (!WebAudio_IsUserInteracted())
                {
                    Debug.LogWarning($"Звук {key} отложен до взаимодействия пользователя");
                    return;
                }
                if (position.HasValue)
                {
                    WebAudio_PlaySound(key, startTime, volume * globalVolume, pitch, priority, position.Value.x, position.Value.y, position.Value.z);
                }
                else
                {
                    WebAudio_PlaySound(key, startTime, volume * globalVolume, pitch, priority, 0f, 0f, 0f);
                }
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка воспроизведения звука {key}: {ex.Message}");
                OnSoundError?.Invoke(key, ex.Message);
            }
        }

        public static void StopSound(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_StopSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка остановки звука {key}: {ex.Message}");
            }
        }

        public static void PauseSound(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_PauseSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка паузы звука {key}: {ex.Message}");
            }
        }

        public static void UnpauseSound(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_UnpauseSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка снятия с паузы звука {key}: {ex.Message}");
            }
        }

        public static void SetVolume(string key, float volume)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                volume = Mathf.Clamp01(volume);
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetVolume(key, volume * globalVolume);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки громкости для {key}: {ex.Message}");
            }
        }

        public static void StopAll()
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_StopAll();
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка остановки всех звуков: {ex.Message}");
            }
        }

        public static void PauseAll()
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_PauseAll();
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка паузы всех звуков: {ex.Message}");
            }
        }

        public static void UnpauseAll()
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_UnpauseAll();
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка снятия с паузы всех звуков: {ex.Message}");
            }
        }

        public static void SetPitch(string key, float pitch)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetPitch(key, pitch);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки pitch для {key}: {ex.Message}");
            }
        }

        public static void SetTime(string key, float time)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                if (time < 0)
                {
                    Debug.LogWarning($"Время не может быть отрицательным: {time}");
                    time = 0;
                }

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetTime(key, time);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки времени для {key}: {ex.Message}");
            }
        }

        public static float GetTime(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0f;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetTime(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения времени для {key}: {ex.Message}");
            }
            return 0f;
        }

        public static void SetLoop(string key, bool loop)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetLoop(key, loop);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки loop для {key}: {ex.Message}");
            }
        }

        public static bool GetLoop(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetLoop(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения loop для {key}: {ex.Message}");
            }
            return false;
        }

        public static float GetVolume(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0f;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetVolume(key) / globalVolume;
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения громкости для {key}: {ex.Message}");
            }
            return 0f;
        }

        public static bool IsSoundLoaded(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_IsSoundLoaded(key);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка проверки загрузки звука {key}: {ex.Message}");
            }
            return false;
        }

        public static bool IsUserInteracted()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return WebAudio_IsUserInteracted();
            #else
            return true;
            #endif
        }

        public static void SetPosition(string key, Vector3 position)
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetPosition(key, position.x, position.y, position.z);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки позиции звука {key}: {ex.Message}");
            }
        }

        public static void SetListenerPosition(Vector3 position)
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetListenerPosition(position.x, position.y, position.z);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки позиции слушателя: {ex.Message}");
            }
        }

        public static void SetListenerOrientation(float forwardX, float forwardY, float forwardZ, float upX = 0f, float upY = 1f, float upZ = 0f)
        {
            try
            {
                // Проверяем, что все значения являются конечными числами
                if (!float.IsFinite(forwardX) || !float.IsFinite(forwardY) || !float.IsFinite(forwardZ) ||
                    !float.IsFinite(upX) || !float.IsFinite(upY) || !float.IsFinite(upZ))
                {
                    Debug.LogWarning($"SetListenerOrientation: получены некорректные значения: forward({forwardX}, {forwardY}, {forwardZ}), up({upX}, {upY}, {upZ})");
                    return;
                }

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetListenerOrientation(forwardX, forwardY, forwardZ, upX, upY, upZ);
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in SetListenerOrientation: {e}");
            }
        }

        // Обработчики событий
        [MonoPInvokeCallback(typeof(SoundLoadedCallback))]
        private static void HandleSoundLoadedCallback(string key, bool success)
        {
            try
            {
                if (success)
            {
                OnSoundLoaded?.Invoke(key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in HandleSoundLoadedCallback: {e}");
            }
        }

        [MonoPInvokeCallback(typeof(SoundErrorCallback))]
        private static void HandleSoundErrorCallback(string key, string error)
        {
            try
            {
                OnSoundError?.Invoke(key, error);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in HandleSoundErrorCallback: {e}");
            }
        }

        [MonoPInvokeCallback(typeof(SoundCompleteCallback))]
        private static void HandleSoundCompleteCallback(string key)
        {
            try
            {
                OnSoundComplete?.Invoke(key);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in HandleSoundCompleteCallback: {e}");
            }
        }

        [MonoPInvokeCallback(typeof(MuteChangedCallback))]
        private static void HandleMuteChangedCallback(bool isMuted)
        {
            try
            {
                OnMuteChanged?.Invoke(isMuted);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in HandleMuteChangedCallback: {e}");
            }
        }
    }
} 