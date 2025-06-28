using FoundersKit.Modules;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif
using UnityEngine.UI;
using TMPro;

public class DataModuleExample : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI outputText;
    public TMP_InputField keyInput;
    public TMP_InputField valueInput;
    public Button saveButton;
    public Button loadButton;
    public Button deleteButton;
    public Button addCustomButton;
    public Button showFileButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        saveButton.onClick.AddListener(OnSave);
        loadButton.onClick.AddListener(OnLoad);
        deleteButton.onClick.AddListener(OnDelete);
        addCustomButton.onClick.AddListener(OnAddCustom);
        showFileButton.onClick.AddListener(OnShowFile);
        
        UpdateOutput();
    }

    private void OnSave()
    {
        DataModule.SaveProgress();
        UpdateOutput();
    }

    private void OnLoad()
    {
        DataModule.LoadProgress();
        UpdateOutput();
    }

    private void OnDelete()
    {
        DataModule.DeleteAllData();
        UpdateOutput();
    }

    private void OnAddCustom()
    {
        string key = keyInput.text;
        string value = valueInput.text;
        if (!string.IsNullOrEmpty(key))
        {
            DataModule.SetCustomValue(key, value);
            UpdateOutput();
        }
    }

    private void OnShowFile()
    {
#if UNITY_EDITOR
        string path = Application.dataPath + "/FoundersPlugin/Modules/DataModule/WorkingData/gameData.json";
        if (File.Exists(path))
        {
            string fileContent = File.ReadAllText(path);
            string prettyJson = PrettyPrintJson(fileContent);
            outputText.text = "gameData.json:\n" + prettyJson;
        }
        else
        {
            outputText.text = "Файл gameData.json не найден.";
        }
#else
        outputText.text = "Просмотр файла доступен только в редакторе Unity.";
#endif
    }

    // Форматирование JSON для удобного чтения
    private string PrettyPrintJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "(пусто)";
        int indent = 0;
        bool quoted = false;
        var sb = new StringBuilder();
        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];
            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', ++indent * 2));
                    }
                    break;
                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', --indent * 2));
                    }
                    sb.Append(ch);
                    break;
                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;
                case ':':
                    sb.Append(ch);
                    if (!quoted) sb.Append(' ');
                    break;
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    int index = i;
                    while (index > 0 && json[--index] == '\\') escaped = !escaped;
                    if (!escaped) quoted = !quoted;
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }

    private void UpdateOutput()
    {
        var data = DataModule.savesData;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Текущие данные:</b>");
        sb.AppendLine("customData:");
        if (data.customData != null && data.customData.Count > 0)
        {
            foreach (var pair in data.customData)
            {
                sb.AppendLine($"  {pair.key}: {pair.value}");
            }
        }
        else
        {
            sb.AppendLine("  (пусто)");
        }
        outputText.text = sb.ToString();
    }
}