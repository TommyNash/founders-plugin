using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Синглтон для глобального управления аудио системой
    /// </summary>
    public class AudioManagement : MonoBehaviour
    {
        private static AudioManagement instance;
        private static bool isQuitting = false;

        public static AudioManagement Instance
        {
            get
            {
                if (isQuitting) return null;
                
                if (instance == null)
                {
                    var go = new GameObject("AudioManagement");
                    instance = go.AddComponent<AudioManagement>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                StopAllSounds();
                instance = null;
            }
        }

        private float globalVolume = 1f;
        private readonly List<SourceAudio> activeSources = new List<SourceAudio>();
        private AudioDatabase database;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadDatabase();
            PreloadSounds();
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null)
            {
                Debug.LogWarning("AudioDatabase не найден в Resources. Создайте его через меню Assets/Create/FoundersPlugin/Audio/Database");
            }
        }

        private void PreloadSounds()
        {
            if (database == null) return;

            foreach (var clipData in database.GetPreloadClips())
            {
                try
                {
                    AudioModule.Instance.LoadAudioClip(clipData.Key);
                    Debug.Log($"Предзагружен звук: {clipData.Key}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка при предзагрузке звука {clipData.Key}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Устанавливает глобальную громкость для всех звуков
        /// </summary>
        public void SetVolume(float value)
        {
            globalVolume = Mathf.Clamp01(value);
            
            // Обновляем громкость для всех активных источников
            foreach (var source in activeSources.ToArray())
            {
                if (source != null)
                {
                    float currentVolume = source.Volume;
                    source.Volume = currentVolume;
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.SetGlobalVolume(globalVolume);
            #endif
        }

        /// <summary>
        /// Получает текущую глобальную громкость
        /// </summary>
        public float GetVolume()
        {
            return globalVolume;
        }

        /// <summary>
        /// Проверяет, проигрывается ли звук в данный момент
        /// </summary>
        public bool IsAudioPlaying(string key)
        {
            return activeSources.Any(source => 
                source != null && 
                source.IsPlaying && 
                source.CurrentKey == key);
        }

        /// <summary>
        /// Получает все активные источники звука для указанного ключа
        /// </summary>
        public IEnumerable<SourceAudio> GetActiveSources(string key)
        {
            return activeSources.Where(source => 
                source != null && 
                source.CurrentKey == key);
        }

        /// <summary>
        /// Останавливает все звуки с указанным ключом
        /// </summary>
        public void StopSound(string key)
        {
            foreach (var source in GetActiveSources(key).ToList())
            {
                source.Stop();
            }
        }

        /// <summary>
        /// Останавливает все звуки
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var source in activeSources.ToList())
            {
                if (source != null)
                {
                    source.Stop();
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.StopAll();
            #endif
        }

        /// <summary>
        /// Ставит все звуки на паузу
        /// </summary>
        public void PauseAllSounds()
        {
            foreach (var source in activeSources.ToArray())
            {
                if (source != null)
                {
                    source.Pause();
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.PauseAll();
            #endif
        }

        /// <summary>
        /// Снимает все звуки с паузы
        /// </summary>
        public void UnpauseAllSounds()
        {
            foreach (var source in activeSources.ToArray())
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLAudioBridge.UnpauseAll();
            #endif
        }

        /// <summary>
        /// Регистрирует источник звука для управления
        /// </summary>
        public void RegisterSource(SourceAudio source)
        {
            if (source != null && !activeSources.Contains(source))
            {
                activeSources.Add(source);
            }
        }

        /// <summary>
        /// Отменяет регистрацию источника звука
        /// </summary>
        public void UnregisterSource(SourceAudio source)
        {
            if (source != null)
            {
                activeSources.Remove(source);
        }
        }
    }
} 