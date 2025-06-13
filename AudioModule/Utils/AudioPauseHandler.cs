using UnityEngine;

namespace FoundersKit.Modules
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
                wasPlaying = AudioManager.Instance.GetVolume() > 0;
                if (wasPlaying)
                {
                    AudioManager.Instance.PauseAll();
                }
            }
            else
            {
                if (wasPlaying)
                {
                    AudioManager.Instance.UnpauseAll();
                }
            }
        }

        /// <summary>
        /// Вызывается перед показом рекламы
        /// </summary>
        public void OnAdStarted()
        {
            wasPlaying = AudioManager.Instance.GetVolume() > 0;
            if (wasPlaying)
            {
                AudioManager.Instance.PauseAll();
            }
        }

        /// <summary>
        /// Вызывается после закрытия рекламы
        /// </summary>
        public void OnAdFinished()
        {
            if (wasPlaying)
            {
                AudioManager.Instance.UnpauseAll();
            }
        }
    }
} 