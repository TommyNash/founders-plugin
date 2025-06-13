using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FoundersKit.Modules
{
    /// <summary>
    /// Настройки аудио системы
    /// </summary>
    public static class AudioSettings
    {
        // Константы для WebGL
        public const string WEBGL_TEMPLATE_NAME = "FoundersPluginYG";
        public const string WEBAUDIO_JS_PATH = "WebAudio.js";

        // Константы для меню
        public const string EDITOR_PANEL_NAME_AUDIO = "Audio/";
        public const string EDITOR_PANEL_TITLE_AUDIO = "Audio Management";
        public const string EDITOR_PANEL_TITLE_WEBAUDIO = "Web Audio Setup";

        // Настройки по умолчанию
        public const float DEFAULT_VOLUME = 1.0f;
        public const float DEFAULT_PITCH = 1.0f;
        public const float MIN_VOLUME = 0.0f;
        public const float MAX_VOLUME = 1.0f;
        public const float MIN_PITCH = 0.5f;
        public const float MAX_PITCH = 2.0f;

        // Пути к ресурсам
        public const string DATABASE_PATH = "AudioDatabase";
        public const string DEFAULT_DATABASE_PATH = "Assets/Resources/AudioDatabase.asset";

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