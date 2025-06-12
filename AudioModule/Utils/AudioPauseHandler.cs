using UnityEngine;

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Обработчик паузы для аудио системы.
    /// Автоматически ставит звуки на паузу при сворачивании приложения или показе рекламы.
    /// </summary>
    public class AudioPauseHandler : MonoBehaviour
    {
        private static AudioPauseHandler instance;
        private bool wasPlaying;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            HandlePause(!hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            HandlePause(pauseStatus);
        }

        private void HandlePause(bool pause)
        {
            if (pause)
            {
                wasPlaying = AudioManagement.Instance.GetVolume() > 0;
                if (wasPlaying)
                {
                    AudioManagement.Instance.PauseAllSounds();
                }
            }
            else
            {
                if (wasPlaying)
                {
                    AudioManagement.Instance.UnpauseAllSounds();
                }
            }
        }

        /// <summary>
        /// Вызывается перед показом рекламы
        /// </summary>
        public void OnAdStarted()
        {
            wasPlaying = AudioManagement.Instance.GetVolume() > 0;
            if (wasPlaying)
            {
                AudioManagement.Instance.PauseAllSounds();
            }
        }

        /// <summary>
        /// Вызывается после закрытия рекламы
        /// </summary>
        public void OnAdFinished()
        {
            if (wasPlaying)
            {
                AudioManagement.Instance.UnpauseAllSounds();
            }
        }
    }
} 