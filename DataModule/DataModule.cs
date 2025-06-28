using System;
using System.Collections;
using System.IO;
using FoundersKit.Logging;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FoundersKit.Modules
{
    public class DataModule : MonoBehaviour
    {
        public const string CONTEXT_NAME = "DataModule";

        #region Inspector Variables
        // Сохранение данных автоматически по интервалу и сам интервал сохранений
        public bool saveDataAutomatically = false;
        public float saveInterval = 5f;
        #endregion

        #region Private Variables
        private static bool isInitialized = false;
        // Данные игры
        public static GameData savesData = new GameData();

        // Пути для локальных и редакторских сохранений
        private static string localSaveFilePath => Path.Combine(Application.persistentDataPath, "saveData.json");
        private static string editorSaveFilePath => Path.Combine(Application.dataPath, "FoundersPlugin", "Modules", "DataModule", "WorkingData", "gameData.json");
        #endregion

        #region Singleton
        private static DataModule instance;
        public static DataModule Instance
        {
            get
            {
                if (instance == null)
                {
                    // Попробовать найти существующий объект в сцене
                    instance = FindFirstObjectByType<DataModule>();
                    if (instance == null)
                    {
                        var go = new GameObject("DataModule");
                        instance = go.AddComponent<DataModule>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!isInitialized)
            {
                Initialize();
            }


            LoadLocalData();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Запуск корутины для периодической отправки данных
            if (saveDataAutomatically) StartCoroutine(SaveDataPeriodically());
        }

        private void Initialize()
        {
            try
            {
                isInitialized = true;
                Log.Success("Module initialized", CONTEXT_NAME);
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing {CONTEXT_NAME}: {ex.Message}", CONTEXT_NAME);
            }
        }
        #endregion

        #region Custom Data

        public static void SetCustomValue<T>(string key, T value)
        {
            string json = JsonUtility.ToJson(new Wrapper<T> { value = value });
            var pair = savesData.customData.Find(x => x.key == key);
            if (pair != null)
            {
                pair.value = json;
            }
            else
            {
                savesData.customData.Add(new StringKeyValue { key = key, value = json });
            }
            SaveProgress();
        }

        public static T GetCustomValue<T>(string key, T defaultValue = default)
        {
            var pair = savesData.customData.Find(x => x.key == key);
            if (pair != null)
            {
                return JsonUtility.FromJson<Wrapper<T>>(pair.value).value;
            }
            return defaultValue;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T value;
        }

        #endregion

        #region Load Data

        // Корутина для ожидания инициализации SDK
        private void LoadLocalData()
        {
#if UNITY_EDITOR
            LoadProgress();
            Log.Info("Local data loaded successfully", CONTEXT_NAME, scope: LogScope.Local);
#endif
        }


        // Универсальный метод для загрузки прогресса
        public static void LoadProgress()
        {
#if UNITY_EDITOR
            string jsonData = LoadFromLocalEditorFile();
            if (!string.IsNullOrEmpty(jsonData))
            {
                savesData = JsonUtility.FromJson<GameData>(jsonData);
                Log.Success("Data loaded and applied from local editor file.", CONTEXT_NAME, scope: LogScope.Local);
            }
#else
            LoadFromYandex();
#endif
        }

        #endregion

        #region Progress Data

        private static float lastSaveTime = 0f;

        // Храним последнее сохранённое состояние данных
        private static string lastSavedJson = null;

        private IEnumerator SaveDataPeriodically()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f); // Проверяем каждую секунду

                // Пытаемся сохранить прогресс (данные будут отправлены только если прошло 5 секунд)
                SaveProgress();
            }
        }

        // Универсальный метод для сохранения прогресса
        public static void SaveProgress()
        {
            float currentTime = Time.time;
            string currentJson = JsonUtility.ToJson(savesData);

            // Проверяем, изменились ли данные
            if (lastSavedJson != null && currentJson == lastSavedJson)
            {
                // Данные не изменились — пропускаем сохранение
                return;
            }

            // Проверяем, прошло ли достаточно времени с последнего сохранения
            if (currentTime - lastSaveTime >= Instance.saveInterval || lastSavedJson == null)
            {
#if UNITY_EDITOR
                SaveToLocalEditorFile(currentJson);
#else
                SaveToYandex(currentJson);
#endif
                lastSaveTime = currentTime;
                lastSavedJson = currentJson;
            }
        }



        // Метод для полного удаления данных
        public static void DeleteAllData()
        {
#if UNITY_EDITOR
            DeleteLocalEditorFile();
#else
            DeleteDataFromYandex();
            DeleteLocalFile();
#endif
            // Очистить текущие данные в памяти
            savesData = new GameData();

            Log.Success("All data has been deleted.", CONTEXT_NAME, scope: LogScope.Local);
        }

        // Удаление локального файла (для редактора Unity)
        private static void DeleteLocalEditorFile()
        {
#if UNITY_EDITOR
            try
            {
                if (File.Exists(editorSaveFilePath))
                {
                    File.Delete(editorSaveFilePath);
                    Log.Success("Local editor save file deleted.", CONTEXT_NAME, scope: LogScope.Local);
                }
                else
                {
                    Log.Warning("No local editor save file found to delete.", CONTEXT_NAME, scope: LogScope.Local);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error deleting local editor save file: {ex.Message}", CONTEXT_NAME, scope: LogScope.Local);
            }
#endif
        }

        // Удаление локального файла (для устройства пользователя)
        private static void DeleteLocalFile()
        {
#if !UNITY_EDITOR
            try
            {
                if (File.Exists(localSaveFilePath))
                {
                    File.Delete(localSaveFilePath);
                    Log.Success("Local save file deleted.", CONTEXT_NAME);
                }
                else
                {
                    Log.Warning("No local save file found to delete.", CONTEXT_NAME);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error deleting local save file: {ex.Message}", CONTEXT_NAME);
            }
#endif
        }

        // Удаление данных с сервера Яндекса
        private static void DeleteDataFromYandex()
        {
#if !UNITY_EDITOR
            YandexDeleteData();
            Log.Info("Delete data request sent to Yandex server.", CONTEXT_NAME);
#endif
        }

        // Save progress to local file (for Unity Editor testing)
        private static void SaveToLocalEditorFile(string jsonData)
        {
#if UNITY_EDITOR
            try
            {
                // Создаем директорию, если она не существует
                string directoryPath = Path.GetDirectoryName(editorSaveFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Log.Info($"Created directory: {directoryPath}", CONTEXT_NAME, scope: LogScope.Local);
                }
                
                File.WriteAllText(editorSaveFilePath, jsonData);
                Log.Success("Data saved to local editor file.", CONTEXT_NAME, scope: LogScope.Local);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error saving data to local editor file: {ex.Message}", CONTEXT_NAME);
            }
#endif
        }

        // Load progress from local file (for Unity Editor testing)
        private static string LoadFromLocalEditorFile()
        {
#if UNITY_EDITOR
            try
            {
                if (File.Exists(editorSaveFilePath))
                {
                    string jsonData = File.ReadAllText(editorSaveFilePath);

                    Log.Success("Data loaded from local editor file.", CONTEXT_NAME, scope: LogScope.Local);
                    return jsonData;
                }
                else
                {
                    Log.Warning("No local editor save file found.", CONTEXT_NAME, scope: LogScope.Local);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error loading data from local editor file: {ex.Message}", CONTEXT_NAME, scope: LogScope.Local);
                return null;
            }
#else
            return null;
#endif
        }

        // Save progress to local file (for runtime use)
        private static void SaveToLocalFile(string jsonData)
        {
#if !UNITY_EDITOR
            try
            {
                File.WriteAllText(localSaveFilePath, jsonData);
                Log.Success("Data saved to local file.", CONTEXT_NAME, scope: LogScope.Local);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error saving data to local file: {ex.Message}", CONTEXT_NAME, scope: LogScope.Local);
            }
#endif
        }

        // Load progress from local file (for runtime use)
        private static string LoadFromLocalFile()
        {
#if !UNITY_EDITOR
            try
            {
                if (File.Exists(localSaveFilePath))
                {
                    string jsonData = File.ReadAllText(localSaveFilePath);
                    Log.Success("Data loaded from local file.", CONTEXT_NAME, scope: LogScope.Local);
                    return jsonData;
                }
                else
                {
                    Log.Warning("No local save file found.", CONTEXT_NAME, scope: LogScope.Local);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error loading data from local file: {ex.Message}", CONTEXT_NAME, scope: LogScope.Local);
                return null;
            }
#else
            return null;
#endif
        }

        public void OnYandexDataDeleted()
        {
            // Очистите локальные данные или выполните другие необходимые действия после удаления данных
            savesData = new GameData();
        }

        // Save progress to Yandex server
        private static void SaveToYandex(string jsonData)
        {
#if !UNITY_EDITOR
            YandexSaveData(jsonData);
#endif
            Log.Info("Save data to Yandex server requested.", CONTEXT_NAME);
        }

        // Load progress from Yandex server
        private static void LoadFromYandex()
        {
#if !UNITY_EDITOR
            YandexLoadData();
#endif
            Log.Info("Load data from Yandex server requested.", CONTEXT_NAME);
        }

        // Метод, вызываемый из JavaScript при успешной загрузке данных
        public void OnYandexDataLoaded(string jsonData)
        {
            savesData = JsonUtility.FromJson<GameData>(jsonData);
        }
        #endregion

        #region Native JS Interfaces
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void YandexSaveData(string json);

        [DllImport("__Internal")]
        private static extern void YandexLoadData();

        [DllImport("__Internal")]
        private static extern void YandexDeleteData();
#endif
        #endregion
    }
}