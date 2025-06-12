using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace FoundersPlugin.Modules
{
    /// <summary>
    /// Атрибут для отображения выпадающего списка со звуками в инспекторе
    /// </summary>
    public class AudioKeyAttribute : PropertyAttribute
    {
        public bool AllowEmpty { get; private set; }

        public AudioKeyAttribute(bool allowEmpty = true)
        {
            AllowEmpty = allowEmpty;
        }
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AudioKeyAttribute))]
    public class AudioKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Используйте AudioKey с string");
                return;
            }

            var database = Resources.Load<AudioDatabase>("AudioDatabase");
            if (database == null)
            {
                EditorGUI.LabelField(position, label.text, "AudioDatabase не найден");
                return;
            }

            var attr = attribute as AudioKeyAttribute;
            var keys = database.GetAllKeys().ToList();
            var options = new string[attr.AllowEmpty ? keys.Count + 1 : keys.Count];
            
            int currentIndex = 0;
            if (attr.AllowEmpty)
            {
                options[0] = "None";
                currentIndex = 1;
                
                foreach (var key in keys)
                {
                    options[currentIndex] = key;
                    if (property.stringValue == key)
                    {
                        break;
                    }
                    currentIndex++;
                }
                
                if (string.IsNullOrEmpty(property.stringValue))
                {
                    currentIndex = 0;
                }
            }
            else
            {
                currentIndex = 0;
                foreach (var key in keys)
                {
                    options[currentIndex] = key;
                    if (property.stringValue == key)
                    {
                        break;
                    }
                    currentIndex++;
                }
            }

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (attr.AllowEmpty && newIndex == 0)
                {
                    property.stringValue = "";
                }
                else
                {
                    property.stringValue = options[newIndex];
                }
            }
        }
    }
    #endif
} 