using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FoundersKit.Core;

namespace FoundersKit.Modules
{
    /// <summary>
    /// База данных для хранения информации об аудио клипах.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = FoundersKitConstants.MENU_ROOT + "Audio/Database")]
    public class AudioDatabase : ScriptableObject
    {
        [System.Serializable]
        public class AudioClipData
        {
            public string Key;
            public string Path;
            public AudioClip Clip;
            public bool PreloadOnStart;
            
            // Дополнительные параметры
            public float DefaultVolume = 1f;
            public bool Loop = false;
            public float Pitch = 1f;
            public List<string> Tags = new List<string>();
            public int LoadPriority = 0;
            public bool Spatial; // Поддержка пространственного звука
            public float MinDistance = 1f; // Минимальная дистанция для пространственного звука
            public float MaxDistance = 500f; // Максимальная дистанция для пространственного звука
            public float RolloffFactor = 1f; // Фактор затухания звука с расстоянием
        }

        [SerializeField] private List<AudioClipData> clips = new List<AudioClipData>();
        
        // Регулярное выражение для проверки ключа (только латинские буквы, цифры и подчеркивания)
        private static readonly Regex keyValidationRegex = new Regex(@"^[a-zA-Z0-9_]+$");

        /// <summary>
        /// Добавляет новый клип в базу данных.
        /// </summary>
        /// <param name="key">Уникальный ключ клипа</param>
        /// <param name="clip">Аудио клип</param>
        /// <param name="preloadOnStart">Нужно ли предзагружать клип при старте</param>
        /// <returns>true если клип успешно добавлен, false если ключ некорректен или уже существует</returns>
        public bool AddClip(string key, AudioClip clip, bool preloadOnStart = false)
        {
            if (!ValidateKey(key) || clip == null)
                return false;

            if (clips.Any(c => c.Key == key))
                return false;

            var clipData = new AudioClipData
            {
                Key = key,
                Clip = clip,
                #if UNITY_EDITOR
                Path = AssetDatabase.GetAssetPath(clip),
                #else
                Path = "",
                #endif
                PreloadOnStart = preloadOnStart
            };

            clips.Add(clipData);
            return true;
        }

        /// <summary>
        /// Обновляет ключ для существующего клипа.
        /// </summary>
        /// <param name="oldKey">Старый ключ</param>
        /// <param name="newKey">Новый ключ</param>
        /// <returns>true если ключ успешно обновлен, false если новый ключ некорректен или уже существует</returns>
        public bool UpdateKey(string oldKey, string newKey)
        {
            if (!ValidateKey(newKey))
                return false;

            if (clips.Any(c => c.Key == newKey))
                return false;

            var clip = clips.FirstOrDefault(c => c.Key == oldKey);
            if (clip != null)
            {
                clip.Key = newKey;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Получает данные о клипе по ключу.
        /// </summary>
        /// <param name="key">Ключ клипа</param>
        /// <returns>Данные о клипе или null если клип не найден</returns>
        public AudioClipData GetClipData(string key)
        {
            return clips.FirstOrDefault(c => c.Key == key);
        }

        /// <summary>
        /// Получает все клипы, которые нужно предзагрузить.
        /// </summary>
        public IEnumerable<AudioClipData> GetPreloadClips()
        {
            return clips.Where(c => c.PreloadOnStart);
        }

        /// <summary>
        /// Удаляет клип из базы данных.
        /// </summary>
        /// <param name="key">Ключ клипа</param>
        /// <returns>true если клип успешно удален</returns>
        public bool RemoveClip(string key)
        {
            var clip = clips.FirstOrDefault(c => c.Key == key);
            if (clip != null)
            {
                return clips.Remove(clip);
            }
            return false;
        }

        /// <summary>
        /// Проверяет корректность ключа.
        /// </summary>
        private bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return keyValidationRegex.IsMatch(key);
        }

        /// <summary>
        /// Проверяет корректность пути к файлу.
        /// </summary>
        public bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Проверяем, что путь не содержит папку Resources
            if (path.Contains("/Resources/"))
            {
                Debug.LogError("Аудио файлы не должны находиться в папке Resources");
                return false;
            }

            // Проверяем, что путь содержит только латинские буквы, цифры и допустимые символы
            var pathRegex = new Regex(@"^[a-zA-Z0-9_/.-]+$");
            return pathRegex.IsMatch(path);
        }

        /// <summary>
        /// Обновляет путь к клипу.
        /// </summary>
        public void UpdateClipPath(string key, string newPath)
        {
            var clip = clips.FirstOrDefault(c => c.Key == key);
            if (clip != null && ValidatePath(newPath))
            {
                clip.Path = newPath;
            }
        }

        /// <summary>
        /// Получает все ключи из базы данных.
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return clips.Select(c => c.Key);
        }

        /// <summary>
        /// Проверяет существование клипа в базе.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return clips.Any(c => c.Key == key);
        }

        /// <summary>
        /// Получает все клипы с указанным тегом
        /// </summary>
        public IEnumerable<AudioClipData> GetClipsByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return Enumerable.Empty<AudioClipData>();
                
            return clips.Where(c => c.Tags != null && c.Tags.Contains(tag));
        }

        /// <summary>
        /// Добавляет тег к клипу
        /// </summary>
        public bool AddTag(string key, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;

            var clip = clips.FirstOrDefault(c => c.Key == key);
            if (clip != null)
            {
                if (clip.Tags == null)
                    clip.Tags = new List<string>();
                    
                if (!clip.Tags.Contains(tag))
                {
                    clip.Tags.Add(tag);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Удаляет тег у клипа
        /// </summary>
        public bool RemoveTag(string key, string tag)
        {
            var clip = clips.FirstOrDefault(c => c.Key == key);
            if (clip != null && clip.Tags != null)
            {
                return clip.Tags.Remove(tag);
            }
            return false;
        }

        /// <summary>
        /// Получает все теги клипа
        /// </summary>
        public IEnumerable<string> GetClipTags(string key)
        {
            var clip = clips.FirstOrDefault(c => c.Key == key);
            return clip?.Tags ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Получает все уникальные теги в базе данных
        /// </summary>
        public IEnumerable<string> GetAllTags()
        {
            return clips
                .Where(c => c.Tags != null)
                .SelectMany(c => c.Tags)
                .Distinct();
        }
    }
} 