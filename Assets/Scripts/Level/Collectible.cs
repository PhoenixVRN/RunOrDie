using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Базовый класс для предметов, которые можно подобрать (золото, ключи, бонусы)
/// </summary>
public class Collectible : MonoBehaviour
{
    [Header("Настройки предмета")]
    [SerializeField] private CollectibleType type = CollectibleType.Gold;
    [SerializeField] private int scoreValue = 100;
    
    [Header("Визуальные эффекты")]
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.2f;
    
    [Header("Звук")]
    [SerializeField] private AudioClip collectSound;
    
    [Header("События")]
    public UnityEvent OnCollected;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private bool isCollected = false;
    
    public enum CollectibleType
    {
        Gold,       // Золото - очки
        Key,        // Ключ - для выхода
        Bonus       // Бонус - дополнительные очки
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (isCollected) return;
        
        // Анимация пульсации
        if (enablePulseAnimation)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f) * 0.1f;
            transform.localScale = originalScale * pulse;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;
        
        // Проверяем, что это игрок
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Collect(player);
        }
    }

    /// <summary>
    /// Подбор предмета игроком
    /// </summary>
    private void Collect(PlayerController player)
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Уведомляем GameManager
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            switch (type)
            {
                case CollectibleType.Gold:
                    gameManager.AddScore(scoreValue);
                    gameManager.CollectGold();
                    Debug.Log($"Подобрано золото! +{scoreValue} очков");
                    break;
                    
                case CollectibleType.Key:
                    gameManager.CollectKey();
                    Debug.Log("Подобран ключ! Теперь можно выйти из уровня!");
                    break;
                    
                case CollectibleType.Bonus:
                    gameManager.AddScore(scoreValue);
                    Debug.Log($"Подобран бонус! +{scoreValue} очков");
                    break;
            }
        }
        else
        {
            Debug.LogWarning("GameManager не найден в сцене!");
        }
        
        // Воспроизводим звук
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Вызываем событие
        OnCollected?.Invoke();
        
        // Визуальный эффект сбора (можно добавить частицы)
        StartCoroutine(CollectAnimation());
    }

    /// <summary>
    /// Анимация сбора предмета
    /// </summary>
    private System.Collections.IEnumerator CollectAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Уменьшение размера и движение вверх
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Изменение прозрачности
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - t;
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        // Уничтожаем объект
        Destroy(gameObject);
    }

    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmos()
    {
        Color gizmoColor = type switch
        {
            CollectibleType.Gold => Color.yellow,
            CollectibleType.Key => Color.cyan,
            CollectibleType.Bonus => Color.magenta,
            _ => Color.white
        };
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    // Публичные методы
    public CollectibleType GetCollectibleType() => type;
    public int GetScoreValue() => scoreValue;
    public bool IsCollected() => isCollected;
}

