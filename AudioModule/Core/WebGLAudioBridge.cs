using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace FoundersPlugin.Modules
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
        private static extern void WebAudio_SetCallback(string type, Action<string> callback);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetErrorCallback(Action<string, string> callback);

        [DllImport("__Internal")]
        private static extern void WebAudio_SetMuteCallback(Action<bool> callback);

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
        private static extern void WebAudio_SetListenerOrientation(float x, float y, float z);
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
                
                // Регистрируем колбэки
                WebAudio_SetCallback("loaded", HandleSoundLoaded);
                WebAudio_SetCallback("complete", HandleSoundComplete);
                WebAudio_SetErrorCallback(HandleSoundError);
                WebAudio_SetMuteCallback(HandleMuteChanged);
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

        public static void SetListenerOrientation(Vector3 forward)
        {
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetListenerOrientation(forward.x, forward.y, forward.z);
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка установки ориентации слушателя: {ex.Message}");
            }
        }

        // Обработчики событий
        private static void HandleSoundLoaded(string key)
        {
            try
            {
                OnSoundLoaded?.Invoke(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в обработчике загрузки звука: {ex.Message}");
            }
        }

        private static void HandleSoundError(string key, string error)
        {
            try
            {
                OnSoundError?.Invoke(key, error);
                Debug.LogError($"Ошибка звука {key}: {error}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в обработчике ошибки звука: {ex.Message}");
            }
        }

        private static void HandleSoundComplete(string key)
        {
            try
            {
                OnSoundComplete?.Invoke(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в обработчике завершения звука: {ex.Message}");
            }
        }

        private static void HandleMuteChanged(bool muted)
        {
            try
            {
                OnMuteChanged?.Invoke(muted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в обработчике изменения mute: {ex.Message}");
            }
        }
    }
} 