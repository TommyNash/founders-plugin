using UnityEngine;
using FoundersKit.Modules;
using FoundersKit.Logging;

/// <summary>
/// Обработчик покупок - отдельный скрипт для настройки логики покупок
/// Пользователи могут редактировать этот файл без доступа к основному модулю
/// </summary>
public class PurchaseHandler : MonoBehaviour
{
    public const string CONTEXT_NAME = "PurchaseHandler";

    private static bool isInitialized = false;

    private void Start()
    {
        // Подписываемся на события покупок
        PaymentsModule.PurchaseSuccessEvent += HandlePurchaseSuccess;
        PaymentsModule.PurchaseFailedEvent += HandlePurchaseFailed;
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        PaymentsModule.PurchaseSuccessEvent -= HandlePurchaseSuccess;
        PaymentsModule.PurchaseFailedEvent -= HandlePurchaseFailed;
    }


    /// <summary>
    /// Инициализация обработчика покупок
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized) return;

        // Подписываемся на события покупок
        PaymentsModule.PurchaseSuccessEvent += HandlePurchaseSuccess;
        PaymentsModule.PurchaseFailedEvent += HandlePurchaseFailed;

        isInitialized = true;
    }

    /// <summary>
    /// Обработка успешной покупки
    /// </summary>
    private static void HandlePurchaseSuccess(string itemId)
    {
        Log.Success("Processing successful purchase", CONTEXT_NAME);

        // Здесь настраивайте логику для каждой покупки
        switch (itemId)
        {
            // Пример:

            case "coins_100":
                Debug.Log("Successful add 100 money");
                break;

            case "coins_500":
                Debug.Log("Successful add 500 money");
                break;

            case "premium_pass":
                Debug.Log("Successful purchase Premium Pass");
                break;

            case "disable_ad":
                Debug.Log("Successful purchase Disable Ad");
                break;

            default:
                Log.Warning($"Unknown purchase item: {itemId}", CONTEXT_NAME);
                break;
        }
    }

    /// <summary>
    /// Обработка неудачной покупки
    /// </summary>
    private static void HandlePurchaseFailed(string itemId)
    {
        Log.Error($"Purchase failed for item: {itemId}", CONTEXT_NAME);
    }
}