#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using FoundersKit.Core;

namespace FoundersKit.Modules
{
    public class WebAudioWindow : EditorWindow
    {
        private const string SECTION_TITLE = AudioSettings.EDITOR_PANEL_TITLE_WEBAUDIO;

        private GUIStyle headerStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle buttonStyle;
        private GUIStyle statusStyle;
        private bool isSetup = false;

        [MenuItem(FoundersKitConstants.MENU_ROOT + AudioSettings.EDITOR_PANEL_NAME_AUDIO + SECTION_TITLE)]
        public static void ShowWindow()
        {
            var window = GetWindow<WebAudioWindow>(SECTION_TITLE);
            window.minSize = new Vector2(500, 500);
            window.maxSize = new Vector2(500, 500);
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }

            if (descriptionStyle == null)
            {
                descriptionStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontSize = 12,
                    margin = new RectOffset(10, 10, 5, 5),
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(20, 20, 5, 5)
                };
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 5, 5),
                    normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
                };
            }
        }

        private void OnEnable()
        {
            CheckSetupStatus();
        }

        private void CheckSetupStatus()
        {
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME, "index.html");
            if (File.Exists(templatePath))
            {
                string content = File.ReadAllText(templatePath);
                isSetup = content.Contains("FoundersPlugin Audio System");
            }
        }

        void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.Space(10);

            // Заголовок
            GUILayout.Label("Web Audio Setup", headerStyle);
            EditorGUILayout.Space(5);

            // Описание
            EditorGUILayout.LabelField(
                "Это окно позволяет управлять настройками Web Audio для вашего проекта. " +
                "Используйте кнопки ниже для установки или удаления компонентов Web Audio.",
                descriptionStyle
            );

            EditorGUILayout.Space(15);

            // Статус
            string statusText = isSetup ? "Web Audio установлен" : "Web Audio не установлен";
            Color originalColor = GUI.color;
            GUI.color = isSetup ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            EditorGUILayout.LabelField(statusText, statusStyle);
            GUI.color = originalColor;

            EditorGUILayout.Space(20);

            // Кнопки
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (!isSetup)
            {
                if (GUILayout.Button("Setup Web Audio", buttonStyle, GUILayout.Width(200)))
                {
                    SetupWebAudio();
                }
            }
            else
            {
                if (GUILayout.Button("Remove Web Audio", buttonStyle, GUILayout.Width(200)))
                {
                    if (EditorUtility.DisplayDialog("Подтверждение",
                        "Вы уверены, что хотите удалить Web Audio компоненты?",
                        "Да, удалить", "Отмена"))
                    {
                        RemoveWebAudio();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // Информация о шаблоне
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Текущий WebGL шаблон:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AudioSettings.WEBGL_TEMPLATE_NAME);
            EditorGUILayout.EndVertical();
        }

        private void SetupWebAudio()
        {
            WebAudioTemplate.InjectAudioScripts();
            WebAudioTemplate.CopyAudioFiles();
            isSetup = true;
            EditorUtility.DisplayDialog("Success", "Web Audio setup completed successfully!", "OK");
            Repaint();
        }

        private void RemoveWebAudio()
        {
            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME, "index.html");
            
            if (!File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("Error", "WebGL template not found!", "OK");
                return;
            }

            string content = File.ReadAllText(templatePath);
            bool madeChanges = false;
            
            // Используем регулярное выражение для поиска и удаления скриптов
            string pattern = @"(\s)*<!--\s*FoundersPlugin Audio System[^>]*-->(\s)*<script[^>]*WebAudio\.js[^>]*></script>";
            string newContent = Regex.Replace(content, pattern, "", RegexOptions.Singleline);
            
            if (newContent != content)
            {
                File.WriteAllText(templatePath, newContent);
                madeChanges = true;
            }

            // Удаляем скопированные файлы
            string audioFilesPath = Path.Combine(Application.dataPath, "WebGLTemplates", AudioSettings.WEBGL_TEMPLATE_NAME, 
                "FoundersPlugin/Modules/AudioModule/Plugins/WebAudioPlugin");
            if (Directory.Exists(audioFilesPath))
            {
                Directory.Delete(audioFilesPath, true);
                madeChanges = true;
            }

            AssetDatabase.Refresh();
            
            if (madeChanges)
            {
                isSetup = false;
                EditorUtility.DisplayDialog("Success", "Web Audio components removed successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No Web Audio components found to remove.", "OK");
            }
            
            Repaint();
        }
    }
}
#endif