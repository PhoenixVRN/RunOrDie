using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Менеджер игры - управление счетом, золотом, ключами, уровнями
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Счет и прогресс")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int goldCollected = 0;
    [SerializeField] private int totalGoldInLevel = 0;
    [SerializeField] private bool hasKey = false;
    
    [Header("Настройки уровня")]
    [SerializeField] private bool requireAllGold = true; // Нужно ли собрать все золото для выхода
    [SerializeField] private bool requireKey = true;     // Нужен ли ключ для выхода
    
    [Header("UI События")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int, int> OnGoldChanged; // (собрано, всего)
    public UnityEvent OnKeyCollected;
    public UnityEvent OnLevelComplete;
    
    private static GameManager instance;
    
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // Подсчитываем общее количество золота в уровне
        CountTotalGold();
    }

    private void Start()
    {
        // Инициализация UI
        UpdateUI();
    }

    /// <summary>
    /// Подсчет общего количества золота в уровне
    /// </summary>
    private void CountTotalGold()
    {
        Collectible[] collectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        totalGoldInLevel = 0;
        
        foreach (Collectible collectible in collectibles)
        {
            if (collectible.GetCollectibleType() == Collectible.CollectibleType.Gold)
            {
                totalGoldInLevel++;
            }
        }
        
        Debug.Log($"Всего золота в уровне: {totalGoldInLevel}");
    }

    /// <summary>
    /// Добавить очки
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"Счет: {currentScore}");
    }

    /// <summary>
    /// Собрано золото
    /// </summary>
    public void CollectGold()
    {
        goldCollected++;
        OnGoldChanged?.Invoke(goldCollected, totalGoldInLevel);
        
        Debug.Log($"Золото: {goldCollected}/{totalGoldInLevel}");
        
        // Проверяем, собрано ли все золото
        if (goldCollected >= totalGoldInLevel)
        {
            Debug.Log("✅ Все золото собрано!");
            CheckLevelComplete();
        }
    }

    /// <summary>
    /// Собран ключ
    /// </summary>
    public void CollectKey()
    {
        hasKey = true;
        OnKeyCollected?.Invoke();
        
        Debug.Log("🔑 Ключ получен!");
        CheckLevelComplete();
    }

    /// <summary>
    /// Проверка возможности завершить уровень
    /// </summary>
    public bool CanCompleteLevel()
    {
        bool goldCondition = !requireAllGold || (goldCollected >= totalGoldInLevel);
        bool keyCondition = !requireKey || hasKey;
        
        return goldCondition && keyCondition;
    }

    /// <summary>
    /// Проверка завершения уровня
    /// </summary>
    private void CheckLevelComplete()
    {
        if (CanCompleteLevel())
        {
            Debug.Log("✨ Уровень можно завершить! Идите к выходу!");
        }
    }

    /// <summary>
    /// Завершить уровень
    /// </summary>
    public void CompleteLevel()
    {
        if (!CanCompleteLevel())
        {
            Debug.LogWarning("Уровень нельзя завершить! Соберите все необходимое!");
            return;
        }
        
        Debug.Log("🎉 УРОВЕНЬ ЗАВЕРШЕН!");
        OnLevelComplete?.Invoke();
        
        // Здесь можно добавить переход на следующий уровень
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Обновление UI
    /// </summary>
    private void UpdateUI()
    {
        OnScoreChanged?.Invoke(currentScore);
        OnGoldChanged?.Invoke(goldCollected, totalGoldInLevel);
    }

    /// <summary>
    /// Сброс уровня
    /// </summary>
    public void ResetLevel()
    {
        currentScore = 0;
        goldCollected = 0;
        hasKey = false;
        CountTotalGold();
        UpdateUI();
    }

    // Геттеры
    public int GetScore() => currentScore;
    public int GetGoldCollected() => goldCollected;
    public int GetTotalGold() => totalGoldInLevel;
    public bool HasKey() => hasKey;

    // Отладка
    private void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, 90, 300, 20), $"Score: {currentScore}");
            GUI.Label(new Rect(10, 110, 300, 20), $"Gold: {goldCollected}/{totalGoldInLevel}");
            GUI.Label(new Rect(10, 130, 300, 20), $"Key: {(hasKey ? "✓" : "✗")}");
            GUI.Label(new Rect(10, 150, 300, 20), $"Can Exit: {(CanCompleteLevel() ? "✓" : "✗")}");
        }
    }
}

