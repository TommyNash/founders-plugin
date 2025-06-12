using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace FoundersPlugin.Modules
{
    public static class WebAudioTemplate
    {
        private const string AUDIO_SCRIPT_TAG = @"
    <!-- FoundersPlugin Audio System -->
    <script src=""FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin/WebAudio.js""></script>";

        public static void InjectAudioScripts()
        {
            #if UNITY_EDITOR
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME, "index.html");
            
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"WebGL шаблон не найден: {templatePath}");
                return;
            }

            string content = File.ReadAllText(templatePath);
            
            // Проверяем, не добавлены ли уже скрипты
            if (content.Contains("FoundersPlugin Audio System"))
            {
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
            PlayerSettings.WebGL.template = AudioSettings.WEBGL_TEMPLATE_NAME;
            #endif
        }

        public static void CopyAudioFiles()
        {
            #if UNITY_EDITOR
            string sourceDir = Path.Combine(Application.dataPath, "FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin");
            string targetDir = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME, "FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin");

            Debug.Log($"Copying audio files from {sourceDir} to {targetDir}");

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError($"Исходная директория не найдена: {sourceDir}");
                return;
            }

            // Создаем целевую директорию, если её нет
            if (!Directory.Exists(targetDir))
            {
                Debug.Log($"Creating target directory: {targetDir}");
                Directory.CreateDirectory(targetDir);
            }

            // Копируем файлы
            string[] filesToCopy = new[] { AudioSettings.WEBAUDIO_JS_PATH };
            foreach (string file in filesToCopy)
            {
                string sourcePath = Path.Combine(sourceDir, file);
                string targetPath = Path.Combine(targetDir, file);

                Debug.Log($"Copying file: {file}");
                Debug.Log($"Source: {sourcePath}");
                Debug.Log($"Target: {targetPath}");

                if (File.Exists(sourcePath))
                {
                    try
                    {
                        File.Copy(sourcePath, targetPath, true);
                        Debug.Log($"Successfully copied {file}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error copying {file}: {e.Message}");
                    }
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

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            #if UNITY_EDITOR
            // Проверяем наличие WebGL шаблона при загрузке редактора
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME);
            if (Directory.Exists(templatePath))
            {
                InjectAudioScripts();
                CopyAudioFiles();
            }
            #endif
        }
    }
} 