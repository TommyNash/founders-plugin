using UnityEngine;
using System.Runtime.InteropServices;
using System;
using AOT;
using FoundersKit.Logging;

namespace FoundersKit.Modules
{
    public static class WebGLAudioBridge
    {
        public const string CONTEXT_NAME = "AudioBridge";

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
        private static bool callbacksRegistered = false;
        private static float globalVolume = 1f;

        /// <summary>
        /// Публичное свойство для проверки состояния инициализации WebGL Audio System
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Унифицированная проверка инициализации системы
        /// </summary>
        /// <param name="caller">Имя вызывающего метода для логирования</param>
        /// <returns>true если система инициализирована, false в противном случае</returns>
        private static bool EnsureInitialized(string caller)
        {
            if (!isInitialized)
            {
                Log.Warning($"Система не инициализирована: пропускаем вызов {caller}", CONTEXT_NAME);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Регистрация колбэков для WebGL аудио системы
        /// </summary>
        private static void RegisterCallbacks()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
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

               Log.Success("Колбэки успешно зарегистрированы", CONTEXT_NAME);
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка регистрации колбэков: {ex.Message}", CONTEXT_NAME);
                // Сбрасываем флаг инициализации, чтобы дальнейшие вызовы пытались инициализировать систему заново
                isInitialized = false;
                callbacksRegistered = false;
            }
#endif
        }

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
                Log.Warning("Система уже инициализирована", CONTEXT_NAME);
                return;
            }

            try
            {
                globalVolume = volume;

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_InitializeAudioSystem(volume);
                
                // Регистрируем колбэки только один раз
                if (!callbacksRegistered)
                {
                    RegisterCallbacks();
                    callbacksRegistered = true;
                }
                #else
                // В редакторе и других платформах просто помечаем колбэки как зарегистрированные
                callbacksRegistered = true;
                #endif

                isInitialized = true;
                Log.Success("Система успешно инициализирована", CONTEXT_NAME);
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка инициализации системы: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetGlobalVolume(float volume)
        {
            if (!EnsureInitialized(nameof(SetGlobalVolume)))
                return;

            try
            {
                globalVolume = Mathf.Clamp01(volume);
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetGlobalVolume(globalVolume);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки глобальной громкости: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void PlaySound(string key, float startTime, float volume, float pitch, int priority = 0, Vector3? position = null)
        {
            if (!EnsureInitialized(nameof(PlaySound)))
                return;

            if (string.IsNullOrEmpty(key))
            {
                Log.Error("Ключ звука не может быть пустым", CONTEXT_NAME);
                OnSoundError?.Invoke(key, "Ключ звука не может быть пустым");
                return;
            }

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (!WebAudio_IsUserInteracted())
                {
                    Log.Warning($"Звук {key} отложен до взаимодействия пользователя", CONTEXT_NAME);
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

                // Уведомляем Unity-слой о начале воспроизведения
                OnSoundLoaded?.Invoke(key);
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка воспроизведения звука {key}: {ex.Message}", CONTEXT_NAME);
                OnSoundError?.Invoke(key, ex.Message);
            }
        }

        public static void StopSound(string key)
        {
            if (!EnsureInitialized(nameof(StopSound)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_StopSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка остановки звука {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void PauseSound(string key)
        {
            if (!EnsureInitialized(nameof(PauseSound)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_PauseSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка паузы звука {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void UnpauseSound(string key)
        {
            if (!EnsureInitialized(nameof(UnpauseSound)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_UnpauseSound(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка снятия с паузы звука {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetVolume(string key, float volume)
        {
            if (!EnsureInitialized(nameof(SetVolume)))
                return;

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
                Log.Error($"Ошибка установки громкости для {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void StopAll()
        {
            if (!EnsureInitialized(nameof(StopAll)))
                return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_StopAll();
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка остановки всех звуков: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void PauseAll()
        {
            if (!EnsureInitialized(nameof(PauseAll)))
                return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_PauseAll();
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка паузы всех звуков: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void UnpauseAll()
        {
            if (!EnsureInitialized(nameof(UnpauseAll)))
                return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_UnpauseAll();
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка снятия с паузы всех звуков: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetPitch(string key, float pitch)
        {
            if (!EnsureInitialized(nameof(SetPitch)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetPitch(key, pitch);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки pitch для {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetTime(string key, float time)
        {
            if (!EnsureInitialized(nameof(SetTime)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                if (time < 0)
                {
                    Log.Warning($"Время не может быть отрицательным: {time}", CONTEXT_NAME);
                    time = 0;
                }

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetTime(key, time);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки времени для {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static float GetTime(string key)
        {
            if (!EnsureInitialized(nameof(GetTime)))
            {
                Log.Warning($"GetTime: возвращаем 0f для {key} - система не инициализирована", CONTEXT_NAME);
                return 0f;
            }

            if (string.IsNullOrEmpty(key)) 
            {
                Log.Warning($"GetTime: возвращаем 0f - ключ пустой", CONTEXT_NAME);
                return 0f;
            }

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetTime(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"GetTime: ошибка получения времени для {key}: {ex.Message}", CONTEXT_NAME);
            }
            return 0f;
        }

        public static void SetLoop(string key, bool loop)
        {
            if (!EnsureInitialized(nameof(SetLoop)))
                return;

            if (string.IsNullOrEmpty(key)) return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetLoop(key, loop);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки loop для {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static bool GetLoop(string key)
        {
            if (!EnsureInitialized(nameof(GetLoop)))
            {
                Log.Warning($"GetLoop: возвращаем false для {key} - система не инициализирована", CONTEXT_NAME);
                return false;
            }

            if (string.IsNullOrEmpty(key)) 
            {
                Log.Warning("GetLoop: возвращаем false - ключ пустой", CONTEXT_NAME);
                return false;
            }

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetLoop(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"GetLoop: ошибка получения loop для {key}: {ex.Message}", CONTEXT_NAME);
            }
            return false;
        }

        public static float GetVolume(string key)
        {
            if (!EnsureInitialized(nameof(GetVolume)))
            {
                Log.Warning($"GetVolume: возвращаем 0f для {key} - система не инициализирована", CONTEXT_NAME);
                return 0f;
            }

            if (string.IsNullOrEmpty(key)) 
            {
                Log.Warning($"GetVolume: возвращаем 0f - ключ пустой", CONTEXT_NAME);
                return 0f;
            }

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_GetVolume(key) / globalVolume;
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"GetVolume: ошибка получения громкости для {key}: {ex.Message}", CONTEXT_NAME);
            }
            return 0f;
        }

        public static bool IsSoundLoaded(string key)
        {
            if (!EnsureInitialized(nameof(IsSoundLoaded)))
            {
                Log.Warning($"IsSoundLoaded: возвращаем false для {key} - система не инициализирована", CONTEXT_NAME);
                return false;
            }

            if (string.IsNullOrEmpty(key)) 
            {
                Log.Warning("IsSoundLoaded: возвращаем false - ключ пустой", CONTEXT_NAME);
                return false;
            }

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                return WebAudio_IsSoundLoaded(key);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"IsSoundLoaded: ошибка проверки загрузки звука {key}: {ex.Message}", CONTEXT_NAME);
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
            if (!EnsureInitialized(nameof(SetPosition)))
                return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetPosition(key, position.x, position.y, position.z);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки позиции звука {key}: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetListenerPosition(Vector3 position)
        {
            if (!EnsureInitialized(nameof(SetListenerPosition)))
                return;

            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetListenerPosition(position.x, position.y, position.z);
                #endif
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка установки позиции слушателя: {ex.Message}", CONTEXT_NAME);
            }
        }

        public static void SetListenerOrientation(float forwardX, float forwardY, float forwardZ, float upX = 0f, float upY = 1f, float upZ = 0f)
        {
            if (!EnsureInitialized(nameof(SetListenerOrientation)))
                return;

            try
            {
                // Проверяем, что все значения являются конечными числами
                if (!float.IsFinite(forwardX) || !float.IsFinite(forwardY) || !float.IsFinite(forwardZ) ||
                    !float.IsFinite(upX) || !float.IsFinite(upY) || !float.IsFinite(upZ))
                {
                    Log.Warning($"SetListenerOrientation: получены некорректные значения: forward({forwardX}, {forwardY}, {forwardZ}), up({upX}, {upY}, {upZ})", CONTEXT_NAME);
                    return;
                }

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebAudio_SetListenerOrientation(forwardX, forwardY, forwardZ, upX, upY, upZ);
                #endif
            }
            catch (Exception e)
            {
                Log.Error($"Error in SetListenerOrientation: {e}", CONTEXT_NAME);
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
                Log.Error($"Error in HandleSoundLoadedCallback: {e}", CONTEXT_NAME);
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
                Log.Error($"Error in HandleSoundErrorCallback: {e}", CONTEXT_NAME);
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
                Log.Error($"Error in HandleSoundCompleteCallback: {e}", CONTEXT_NAME);
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
                Log.Error($"Error in HandleMuteChangedCallback: {e}", CONTEXT_NAME);
            }
        }
    }
} 