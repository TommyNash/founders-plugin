using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Константы и настройки для аудио модуля
    /// </summary>
    public static class AudioSettings
    {
        // Звук
        public const float DEFAULT_VOLUME = 1.0f;
        public const float MIN_PITCH = -0.5f;
        public const float MAX_PITCH = 3.0f;
        public const float DEFAULT_PITCH = 1.0f;

        // Пути
        public const string WEBGL_PLUGIN_PATH = "WebAudioPlugin";
        public const string WEBAUDIO_JS_PATH = "WebAudio.js";

        // WebGL
        public const string WEBGL_TEMPLATE_NAME = "FoundersPluginYG";

        // Форматы файлов
        public static readonly string[] SUPPORTED_FORMATS = new string[] { ".mp3", ".wav", ".ogg" };
        public const string RECOMMENDED_FORMAT = ".mp3";

        // Проверка формата файла
        public static bool IsFormatSupported(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            
            foreach (var format in SUPPORTED_FORMATS)
            {
                if (path.ToLower().EndsWith(format))
                    return true;
            }
            return false;
        }

        // Проверка рекомендуемого формата
        public static bool IsRecommendedFormat(string path)
        {
            return !string.IsNullOrEmpty(path) && path.ToLower().EndsWith(RECOMMENDED_FORMAT);
        }

        // Получение размера файла в мегабайтах
        public static float GetFileSizeMB(AudioClip clip)
        {
            if (clip == null) return 0f;
            
            float sizeBytes = clip.samples * clip.channels * 2f; // 16 bit = 2 bytes
            return sizeBytes / (1024f * 1024f);
        }

        // Проверка размера файла
        public static bool IsFileSizeAcceptable(AudioClip clip, float maxSizeMB = 5f)
        {
            if (clip == null) return false;
            return GetFileSizeMB(clip) <= maxSizeMB;
        }
    }
} 