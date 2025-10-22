using UnityEngine;

/// <summary>
/// Выход из уровня - игрок должен дойти сюда после сбора всего необходимого
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Настройки")] [SerializeField] private bool isActive = true;

    [Header("Визуальные эффекты")]
    [Tooltip("Спрайт открытой двери (дочерний объект) - показывается когда выход разблокирован")]
    [SerializeField]
    private GameObject openDoorSprite;

    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Звук")] [SerializeField] private AudioClip exitSound;
    [SerializeField] private AudioClip lockedSound;

    private GameManager gameManager;
    private Vector3 originalScale;
    private bool playerInTrigger = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        UpdateVisuals();
    }

    private void Update()
    {
        // Обновляем визуал в зависимости от условий
        UpdateVisuals();

        // Анимация пульсации для разблокированного выхода
        if (enablePulseAnimation && CanExit())
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
            transform.localScale = originalScale * pulse;
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Обновление визуала в зависимости от состояния
    /// </summary>
    private void UpdateVisuals()
    {
        if (openDoorSprite == null) return;

        // Показываем спрайт открытой двери только когда выход разблокирован
        bool canExit = CanExit();
        if (canExit)
        {
            openDoorSprite.SetActive(false);
        }
        else
        {
            openDoorSprite.SetActive(true);
        }
    }

    /// <summary>
    /// Проверка, можно ли выйти из уровня
    /// </summary>
    private bool CanExit()
    {
        if (!isActive) return false;
        if (gameManager == null) return false;

        return gameManager.CanCompleteLevel();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            playerInTrigger = true;
            TryExit();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            playerInTrigger = false;
        }
    }

    /// <summary>
    /// Попытка выйти из уровня
    /// </summary>
    private void TryExit()
    {
        if (!playerInTrigger) return;

        if (CanExit())
        {
            // Выход разблокирован - завершаем уровень
            Debug.Log("🎉 Игрок вошел в выход! Уровень завершен!");

            if (exitSound != null)
            {
                AudioSource.PlayClipAtPoint(exitSound, transform.position);
            }

            if (gameManager != null)
            {
                gameManager.CompleteLevel();
            }
        }
        else
        {
            // Выход заблокирован - показываем сообщение
            string message = GetBlockedMessage();
            Debug.LogWarning($"⚠️ Выход заблокирован! {message}");

            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }
        }
    }

    /// <summary>
    /// Получить сообщение о том, что блокирует выход
    /// </summary>
    private string GetBlockedMessage()
    {
        if (gameManager == null) return "GameManager не найден!";

        int goldCollected = gameManager.GetGoldCollected();
        int totalGold = gameManager.GetTotalGold();
        bool hasKey = gameManager.HasKey();

        if (goldCollected < totalGold && !hasKey)
        {
            return $"Соберите все золото ({goldCollected}/{totalGold}) и найдите ключ!";
        }
        else if (goldCollected < totalGold)
        {
            return $"Соберите все золото! ({goldCollected}/{totalGold})";
        }
        else if (!hasKey)
        {
            return "Найдите ключ!";
        }

        return "Условия не выполнены";
    }

    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, "EXIT");
#endif
    }
}