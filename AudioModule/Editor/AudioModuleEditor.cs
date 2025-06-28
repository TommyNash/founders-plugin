#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FoundersKit.Core;

namespace FoundersKit.Modules
{
    public class AudioModuleEditor : EditorWindow
    {
        private const string SECTION_TITLE = "Audio Management";

        private AudioDatabase database;
        private Vector2 scrollPosition;
        private string searchQuery = "";
        private bool showPreloadOnly = false;
        private AudioClip newClip;
        private string newKey = "";
        private bool newPreload = false;
        private string newTag = "";
        private List<string> commonTags = new List<string> { "Music", "SFX", "UI", "Ambient", "Voice", "Background" };
        private const string DEFAULT_DATABASE_PATH = "Assets/Resources/AudioDatabase.asset";
        
        // Состояния фолдаутов для каждого клипа
        private Dictionary<string, ClipFoldoutState> foldoutStates = new Dictionary<string, ClipFoldoutState>();

        private class ClipFoldoutState
        {
            public bool showMainSettings = true;
            public bool showPlaybackSettings = false;
            public bool showTags = false;
        }

        // Стили
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle audioItemStyle;
        private GUIStyle dragDropStyle;
        private GUIStyle tagStyle;
        private Texture2D audioIcon;
        private float dragAreaHeight = 100f;

        [MenuItem(FoundersKitConstants.MENU_ROOT + FoundersKitConstants.EDITOR_PANEL_NAME_AUDIO + SECTION_TITLE)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioModuleEditor>(SECTION_TITLE);
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabase();
            InitializeStyles();
            audioIcon = EditorGUIUtility.FindTexture("AudioSource Icon");
        }

        private void InitializeStyles()
            {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(5, 5, 5, 5)
            };

            sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            audioItemStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                padding = new RectOffset(5, 5, 5, 5),
                margin = new RectOffset(0, 0, 2, 2)
            };

            dragDropStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };

            tagStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(0, 2, 0, 0),
                fontSize = 10
            };
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null)
            {
            database = CreateInstance<AudioDatabase>();
                var path = "Assets/Resources/AudioDatabase.asset";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawDragAndDropArea();
            DrawAddClipSection();
            DrawClipsList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Поиск
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)) && !string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            showPreloadOnly = EditorGUILayout.ToggleLeft("Только предзагружаемые", showPreloadOnly, GUILayout.Width(150));
            
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                LoadDatabase();
            }

            if (GUILayout.Button("Остановить все", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                AudioUtility.StopClip();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDragAndDropArea()
        {
            var dragArea = GUILayoutUtility.GetRect(0.0f, dragAreaHeight, GUILayout.ExpandWidth(true));
            GUI.Box(dragArea, "Перетащите аудио файлы сюда\n(поддерживаются .mp3, .wav, .ogg)", dragDropStyle);

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dragArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var path in DragAndDrop.paths)
                        {
                            if (AudioSettings.IsFormatSupported(path))
                            {
                                AddAudioFile(path);
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private void AddAudioFile(string path)
        {
            // Получаем относительный путь
            string relativePath = Path.GetRelativePath(Application.dataPath, path).Replace("\\", "/");
            if (!relativePath.StartsWith("Resources/"))
            {
                // Копируем файл в Resources, если он не там
                string fileName = Path.GetFileName(path);
                string resourcesPath = Path.Combine(Application.dataPath, "Resources", "Sounds");
                Directory.CreateDirectory(resourcesPath);
                string newPath = Path.Combine(resourcesPath, fileName);
                File.Copy(path, newPath, true);
                relativePath = $"Sounds/{Path.GetFileNameWithoutExtension(fileName)}";
            }
            else
            {
                // Убираем "Resources/" из пути и расширение
                relativePath = relativePath.Substring("Resources/".Length);
                relativePath = Path.ChangeExtension(relativePath, null);
            }

            // Создаем ключ из имени файла
            string key = Path.GetFileNameWithoutExtension(path)
                            .Replace(" ", "_")
                            .Replace("-", "_")
                            .ToLower();

            // Добавляем в базу данных
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                database.AddClip(key, clip, false);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void DrawAddClipSection()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Добавить новый звук", headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            newClip = (AudioClip)EditorGUILayout.ObjectField("Клип", newClip, typeof(AudioClip), false);
            if (newClip != null && string.IsNullOrEmpty(newKey))
            {
                newKey = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(newClip))
                            .Replace(" ", "_")
                            .Replace("-", "_")
                            .ToLower();
            }
            EditorGUILayout.EndHorizontal();

            newKey = EditorGUILayout.TextField("Ключ", newKey);
            newPreload = EditorGUILayout.Toggle("Предзагрузка", newPreload);

            GUI.enabled = newClip != null && !string.IsNullOrEmpty(newKey);
            if (GUILayout.Button("Добавить", GUILayout.Height(25)))
            {
                if (database.AddClip(newKey, newClip, newPreload))
                {
                    EditorUtility.SetDirty(database);
                    AssetDatabase.SaveAssets();
                    newClip = null;
                    newKey = "";
                    newPreload = false;
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawClipsList()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Звуки в базе", headerStyle);

            var clips = database.GetAllKeys()
                .Where(k => string.IsNullOrEmpty(searchQuery) || k.ToLower().Contains(searchQuery.ToLower()))
                .Select(k => database.GetClipData(k))
                .Where(c => !showPreloadOnly || c.PreloadOnStart)
                .ToList();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var clipData in clips)
            {
                if (!foldoutStates.ContainsKey(clipData.Key))
                {
                    foldoutStates[clipData.Key] = new ClipFoldoutState();
                }
                var state = foldoutStates[clipData.Key];

                EditorGUILayout.BeginVertical(audioItemStyle);
                
                // Заголовок клипа с основной информацией
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(audioIcon, GUILayout.Width(20), GUILayout.Height(20));
                
                // Кнопки управления воспроизведением
                if (GUILayout.Button("▶", GUILayout.Width(25)))
                {
                    if (clipData.Clip != null) AudioUtility.PlayClip(clipData.Clip);
                }
                if (GUILayout.Button("■", GUILayout.Width(25)))
                {
                    AudioUtility.StopClip();
                }

                // Ключ и клип в одной строке
                EditorGUILayout.LabelField("Ключ:", GUILayout.Width(40));
                string newKey = EditorGUILayout.TextField(clipData.Key, GUILayout.Width(100));
                if (newKey != clipData.Key && !string.IsNullOrEmpty(newKey))
                {
                    database.UpdateKey(clipData.Key, newKey);
                    EditorUtility.SetDirty(database);
                }

                var newClip = (AudioClip)EditorGUILayout.ObjectField(clipData.Clip, typeof(AudioClip), false);
                if (newClip != clipData.Clip)
                {
                    clipData.Clip = newClip;
                    clipData.Path = AssetDatabase.GetAssetPath(newClip);
                    EditorUtility.SetDirty(database);
                }

                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Подтверждение",
                        $"Вы уверены, что хотите удалить звук {clipData.Key}?", "Да", "Отмена"))
                    {
                        // Закрываем все открытые GUI блоки перед удалением
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndVertical();
                        
                        // Удаляем клип
                        database.RemoveClip(clipData.Key);
                        foldoutStates.Remove(clipData.Key);
                        EditorUtility.SetDirty(database);
                        GUIUtility.ExitGUI();
                        return;
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Основные настройки
                state.showMainSettings = EditorGUILayout.Foldout(state.showMainSettings, "Основные настройки", true);
                if (state.showMainSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    clipData.PreloadOnStart = EditorGUILayout.Toggle("Предзагрузка", clipData.PreloadOnStart);
                    clipData.Loop = EditorGUILayout.Toggle("Зацикливание", clipData.Loop);
                    EditorGUILayout.EndHorizontal();
                    
                    clipData.LoadPriority = EditorGUILayout.IntSlider("Приоритет загрузки", clipData.LoadPriority, 0, 100);
                    EditorGUI.indentLevel--;
                }

                // Настройки воспроизведения
                state.showPlaybackSettings = EditorGUILayout.Foldout(state.showPlaybackSettings, "Настройки воспроизведения", true);
                if (state.showPlaybackSettings)
                {
                    EditorGUI.indentLevel++;
                    clipData.DefaultVolume = EditorGUILayout.Slider("Громкость", clipData.DefaultVolume, 0f, 1f);
                    clipData.Pitch = EditorGUILayout.Slider("Pitch", clipData.Pitch, 0.5f, 2f);
                    EditorGUI.indentLevel--;
                }

                // Теги
                state.showTags = EditorGUILayout.Foldout(state.showTags, "Теги", true);
                if (state.showTags)
                {
                    EditorGUI.indentLevel++;
                    if (clipData.Tags == null) clipData.Tags = new List<string>();
                    
                    // Отображение текущих тегов
                    EditorGUILayout.BeginHorizontal();
                    foreach (var tag in clipData.Tags.ToList())
                    {
                        EditorGUILayout.BeginHorizontal(tagStyle, GUILayout.Width(80));
                        GUILayout.Label(tag);
                        if (GUILayout.Button("×", GUILayout.Width(20)))
                        {
                            clipData.Tags.Remove(tag);
                            EditorUtility.SetDirty(database);
                        }
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(2);
                    }
                    EditorGUILayout.EndHorizontal();

                    // Добавление тегов
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        var menu = new GenericMenu();
                        foreach (var tag in commonTags.Where(t => !clipData.Tags.Contains(t)))
                        {
                            menu.AddItem(new GUIContent(tag), false, () => {
                                clipData.Tags.Add(tag);
                                EditorUtility.SetDirty(database);
                            });
                }
                        menu.ShowAsContext();
                    }
                    newTag = EditorGUILayout.TextField(newTag, GUILayout.Width(100));
                    if (GUILayout.Button("Добавить", GUILayout.Width(70)) && !string.IsNullOrEmpty(newTag))
                {
                        if (!clipData.Tags.Contains(newTag))
                        {
                            clipData.Tags.Add(newTag);
                            if (!commonTags.Contains(newTag))
                            {
                                commonTags.Add(newTag);
                            }
                            EditorUtility.SetDirty(database);
                }
                        newTag = "";
                    }
                EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    public static class AudioUtility
    {
        private static AudioClip currentPlayingClip;

        public static void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            StopClip();
            
            try
            {
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                if (audioUtilClass == null)
                {
                    Debug.LogError("Не удалось найти класс UnityEditor.AudioUtil");
                    return;
                }

                var playClipMethod = audioUtilClass.GetMethod(
                    "PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );

                if (playClipMethod == null)
                {
                    Debug.LogError("Не удалось найти метод PlayPreviewClip");
                    return;
                }

                playClipMethod.Invoke(null, new object[] { clip, 0, false });
                currentPlayingClip = clip;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка при проигрывании клипа: {ex.Message}");
            }
        }

        public static void StopClip()
        {
            try
            {
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                if (audioUtilClass == null)
                {
                    Debug.LogError("Не удалось найти класс UnityEditor.AudioUtil");
                    return;
                }

                var stopClipMethod = audioUtilClass.GetMethod(
                    "StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new System.Type[] { },
                    null
                );

                if (stopClipMethod == null)
                {
                    Debug.LogError("Не удалось найти метод StopAllPreviewClips");
                    return;
                }

                stopClipMethod.Invoke(null, new object[] { });
                currentPlayingClip = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка при остановке клипа: {ex.Message}");
            }
        }
    }
}
#endif 