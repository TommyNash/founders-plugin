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
        // Кэширование
        public const int DEFAULT_CACHE_LIFETIME_SECONDS = 300;
        public const int DEFAULT_MAX_CACHE_ITEMS = 100;
        public const int MIN_CACHE_LIFETIME_SECONDS = 60;
        public const int MAX_CACHE_LIFETIME_SECONDS = 3600;

        // Звук
        public const float DEFAULT_VOLUME = 1.0f;
        public const float MIN_PITCH = -0.5f;
        public const float MAX_PITCH = 3.0f;
        public const float DEFAULT_PITCH = 1.0f;

        // Пути
        public const string DATABASE_PATH = "AudioDatabase";
        public const string WEBGL_PLUGIN_PATH = "Web Audio Plugin";
        public const string HOWLER_JS_PATH = "howler.min.js";
        public const string WEBAUDIO_JS_PATH = "WebAudio.js";

        // Валидация
        public const string KEY_VALIDATION_PATTERN = @"^[a-zA-Z0-9_]+$";
        public const string PATH_VALIDATION_PATTERN = @"^[a-zA-Z0-9_/.-]+$";
        public const string INVALID_KEY_MESSAGE = "Используйте только латинские буквы, цифры и подчеркивания";
        public const string INVALID_PATH_MESSAGE = "Путь содержит недопустимые символы";
        public const string RESOURCES_PATH_ERROR = "Аудио файлы не должны находиться в папке Resources";

        // События
        public const string ON_SOUND_LOADED = "OnSoundLoaded";
        public const string ON_SOUND_FAILED = "OnSoundFailed";
        public const string ON_SOUND_PLAYED = "OnSoundPlayed";
        public const string ON_SOUND_STOPPED = "OnSoundStopped";

        // WebGL
        public const string WEBGL_TEMPLATE_NAME = "Web Audio";
        public const string WEBGL_HEAD_SCRIPT_TAG = @"
<script type=""text/javascript"" src=""./Web Audio Plugin/howler.min.js""></script>
<script type=""text/javascript"" src=""./Web Audio Plugin/WebAudio.js""></script>";

        // Редактор
        public const string MENU_PATH = "Tools/FoundersPlugin/Audio Management";
        public const string CREATE_DATABASE_TITLE = "Создать AudioDatabase";
        public const string CREATE_DATABASE_MESSAGE = "Выберите место для сохранения базы данных";
        public const string DATABASE_NOT_FOUND_MESSAGE = "AudioDatabase не найден. Создайте его через меню Assets/Create/FoundersPlugin/Audio/Database";
        public const string DELETE_SOUND_TITLE = "Подтверждение";
        public const string DELETE_SOUND_MESSAGE = "Вы уверены, что хотите удалить звук {0}?";

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

        // Получение рекомендаций по оптимизации
        public static string GetOptimizationRecommendations(AudioClip clip)
        {
            if (clip == null) return "Клип не выбран";

            #if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(clip);
            #else
            var path = "";
            #endif

            var recommendations = new System.Text.StringBuilder();

            if (!IsFormatSupported(path))
                recommendations.AppendLine("- Используйте поддерживаемый формат файла");
            
            if (!IsRecommendedFormat(path))
                recommendations.AppendLine($"- Рекомендуется использовать формат {RECOMMENDED_FORMAT}");

            if (!IsFileSizeAcceptable(clip))
                recommendations.AppendLine($"- Размер файла ({GetFileSizeMB(clip):F2}MB) превышает рекомендуемый (5MB)");

            if (path.Contains("/Resources/"))
                recommendations.AppendLine("- Не храните аудио файлы в папке Resources");

            return recommendations.Length > 0 ? recommendations.ToString() : "Оптимизация не требуется";
        }
    }
} 