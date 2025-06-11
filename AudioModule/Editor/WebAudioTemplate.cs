using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace FoundersPlugin.Modules
{
    public static class WebAudioTemplate
    {
        private const string TEMPLATE_NAME = "FoundersPluginYG";
        private const string AUDIO_SCRIPT_TAG = @"
    <!-- FoundersPlugin Audio System -->
    <script src=""FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin/WebAudio.js""></script>";

        public static void InjectAudioScripts()
        {
            #if UNITY_EDITOR
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", TEMPLATE_NAME, "index.html");
            
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"WebGL шаблон не найден: {templatePath}");
                return;
            }

            string content = File.ReadAllText(templatePath);
            
            // Проверяем, не добавлены ли уже скрипты
            if (content.Contains("FoundersPlugin Audio System"))
            {
                Debug.Log("Аудио скрипты уже добавлены в WebGL шаблон");
                return;
            }

            // Ищем место для вставки скриптов (перед закрывающим тегом head)
            int headEndIndex = content.IndexOf("</head>");
            if (headEndIndex == -1)
            {
                Debug.LogError("Не найден закрывающий тег head в шаблоне");
                return;
            }

            // Вставляем скрипты
            content = content.Insert(headEndIndex, AUDIO_SCRIPT_TAG);
            
            // Сохраняем изменения
            File.WriteAllText(templatePath, content);
            
            Debug.Log("Аудио скрипты успешно добавлены в WebGL шаблон");

            // Обновляем настройки WebGL
            PlayerSettings.WebGL.template = TEMPLATE_NAME;
            #endif
        }

        public static void RemoveAudioScripts()
        {
            #if UNITY_EDITOR
            // Сначала удаляем скрипты из HTML
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", TEMPLATE_NAME, "index.html");
            
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"WebGL шаблон не найден: {templatePath}");
                return;
            }

            string content = File.ReadAllText(templatePath);
            
            // Проверяем, есть ли скрипты для удаления
            if (!content.Contains("FoundersPlugin Audio System"))
            {
                Debug.Log("Аудио скрипты не найдены в WebGL шаблоне");
                return;
            }

            // Удаляем скрипты
            content = content.Replace(AUDIO_SCRIPT_TAG, "");
            
            // Сохраняем изменения
            File.WriteAllText(templatePath, content);
            
            Debug.Log("Аудио скрипты успешно удалены из WebGL шаблона");

            // Затем удаляем директорию AudioModule
            string audioModuleDirRelative = "Assets/WebGLTemplates/" + TEMPLATE_NAME + "/FoundersPlugin/Modules/AudioModule";
            string audioModuleDirFull = Path.Combine(Application.dataPath, "WebGLTemplates", TEMPLATE_NAME, "FoundersPlugin/Modules/AudioModule");

            if (Directory.Exists(audioModuleDirFull))
            {
                try
                {
                    // Удаляем директорию через AssetDatabase
                    bool success = AssetDatabase.DeleteAsset(audioModuleDirRelative);
                    
                    if (success)
                    {
                        Debug.Log("Аудио файлы успешно удалены");
                    }
                    else
                    {
                        Debug.LogWarning("Не удалось удалить некоторые файлы через AssetDatabase");
                        
                        // Если не удалось удалить через AssetDatabase, пробуем удалить напрямую
                        if (Directory.Exists(audioModuleDirFull))
                        {
                            Directory.Delete(audioModuleDirFull, true);
                            Debug.Log("Аудио файлы удалены через прямое удаление директории");
                        }
                    }

                    // Проверяем и удаляем пустые родительские директории
                    CleanupEmptyParentDirectories(audioModuleDirFull);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Ошибка при удалении файлов: {ex.Message}");
                }
                finally
                {
                    AssetDatabase.Refresh();
                }
            }
            #endif
        }

        private static void CleanupEmptyParentDirectories(string startPath)
        {
            #if UNITY_EDITOR
            var dir = new DirectoryInfo(startPath).Parent;
            var assetsPath = Application.dataPath;
            var modulesPath = Path.Combine(assetsPath, "WebGLTemplates", TEMPLATE_NAME, "FoundersPlugin/Modules");

            // Проходим по родительским директориям до Modules
            while (dir != null && 
                   dir.FullName.StartsWith(assetsPath) && 
                   dir.FullName.Length >= modulesPath.Length)
            {
                // Если дошли до директории Modules, останавливаемся
                if (dir.FullName.Equals(modulesPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var relativePath = "Assets" + dir.FullName.Substring(assetsPath.Length).Replace('\\', '/');
                
                // Проверяем, есть ли файлы или директории в текущей директории
                if (dir.GetFileSystemInfos().Length == 0)
                {
                    // Удаляем пустую директорию через AssetDatabase
                    AssetDatabase.DeleteAsset(relativePath);
                    dir = dir.Parent;
                }
                else
                {
                    break;
                }
            }
            #endif
        }

        public static void CopyAudioFiles()
        {
            #if UNITY_EDITOR
            string sourceDir = Path.Combine(Application.dataPath, "FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin");
            string targetDir = Path.Combine(Application.dataPath, "WebGLTemplates", TEMPLATE_NAME, "FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin");

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError($"Исходная директория не найдена: {sourceDir}");
                return;
            }

            // Создаем целевую директорию, если её нет
            Directory.CreateDirectory(targetDir);

            // Копируем файлы
            string[] filesToCopy = new[] { "WebAudio.js" };
            foreach (string file in filesToCopy)
            {
                string sourcePath = Path.Combine(sourceDir, file);
                string targetPath = Path.Combine(targetDir, file);

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, true);
                }
                else
                {
                    Debug.LogError($"Файл не найден: {sourcePath}");
                }
            }

            Debug.Log("Аудио файлы успешно скопированы в WebGL шаблон");
            AssetDatabase.Refresh();
            #endif
        }
    }
} 