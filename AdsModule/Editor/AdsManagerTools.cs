
#if UNITY_EDITOR
using FoundersKit.Core;
using UnityEditor;
using UnityEngine;

public class AdsManagerTools : MonoBehaviour
{
    #region Variables

    private const string SECTION_TITLE = "Add Timer Ads Canvas to Scene";
    private static string prefabName = "TimerAdsCanvas.prefab";
    private static string adsCanvasPrefabPath = $"Assets/FoundersPlugin/Modules/AdsModule/{prefabName}";

    #endregion

    // Window
    [MenuItem(FoundersKitConstants.MENU_ROOT + FoundersKitConstants.EDITOR_PANEL_NAME_ADS + SECTION_TITLE)]
    public static void AddAdsCanvasToScene()
    {
        AddPrefabToScene(adsCanvasPrefabPath, "TimerAdsCanvas");
    }

    // Метод для добавления префаба на сцену
    private static void AddPrefabToScene(string prefabPath, string prefabName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Префаб не найден по пути: {prefabPath}");
            return;
        }

        if (GameObject.Find(prefabName) != null)
        {
            Debug.LogWarning($"{prefabName} уже существует на сцене.");
            return;
        }

        PrefabUtility.InstantiatePrefab(prefab);
        Debug.Log($"{prefabName} был добавлен на сцену.");
    }
}
#endif
