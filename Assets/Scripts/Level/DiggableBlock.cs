using UnityEngine;
using System.Collections;

/// <summary>
/// Блок, который можно выкопать (в стиле Lode Runner)
/// После копания блок исчезает и восстанавливается через время
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DiggableBlock : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float defaultRestoreTime = 5f;
    [SerializeField] private bool canBeDigged = true;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject brokenEffect;
    [SerializeField] private GameObject restoreEffect;

    [Header("Восстановление")]
    [SerializeField] private bool showRestoreProgress = true;
    [SerializeField] private Color warningColor = Color.yellow;

    public SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;
    private Color originalColor;
    private bool isDug = false;
    private Coroutine restoreCoroutine;

    private void Awake()
    {
        //spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// Выкопать блок
    /// </summary>
    /// <param name="restoreTime">Время до восстановления</param>
    public void Dig(float restoreTime = -1f)
    {
        if (!canBeDigged || isDug) return;

        if (restoreTime < 0)
        {
            restoreTime = defaultRestoreTime;
        }

        isDug = true;

        // Отключаем коллизию и визуал
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Эффект разрушения
        if (brokenEffect != null)
        {
            Instantiate(brokenEffect, transform.position, Quaternion.identity);
        }

        // Запускаем восстановление
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
        }
        restoreCoroutine = StartCoroutine(RestoreAfterTime(restoreTime));

        Debug.Log($"Блок '{gameObject.name}' выкопан. Восстановится через {restoreTime}с");
    }

    /// <summary>
    /// Корутина восстановления блока
    /// </summary>
    private IEnumerator RestoreAfterTime(float time)
    {
        float elapsed = 0f;
        float warningTimeStart = time * 0.7f; // Предупреждение за 30% до восстановления

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;

            // Предупреждение о восстановлении
            if (showRestoreProgress && elapsed >= warningTimeStart && spriteRenderer != null)
            {
                // Мигание перед восстановлением
                float progress = (elapsed - warningTimeStart) / (time - warningTimeStart);
                float blinkSpeed = Mathf.Lerp(2f, 8f, progress);
                
                spriteRenderer.enabled = Mathf.Sin(elapsed * blinkSpeed) > 0;
                spriteRenderer.color = warningColor;
            }

            yield return null;
        }

        Restore();
    }

    /// <summary>
    /// Восстановить блок
    /// </summary>
    public void Restore()
    {
        isDug = false;

        // Включаем коллизию и визуал
        if (blockCollider != null)
        {
            blockCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        // Эффект восстановления
        if (restoreEffect != null)
        {
            Instantiate(restoreEffect, transform.position, Quaternion.identity);
        }

        // Проверка, не застрял ли игрок внутри блока
        CheckForPlayerInside();

        Debug.Log($"Блок '{gameObject.name}' восстановлен");
    }

    /// <summary>
    /// Проверка, не находится ли игрок или враги внутри блока при восстановлении
    /// В оригинальной игре Lode Runner - раздавливает!
    /// </summary>
    private void CheckForPlayerInside()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position,
            transform.localScale * 0.9f,
            0f
        );

        foreach (var col in colliders)
        {
            // Проверка игрока
            if (col.CompareTag("Player"))
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null && !player.IsDead())
                {
                    player.Die();
                    Debug.Log("Игрок раздавлен восстанавливающимся блоком!");
                }
            }
            
            // Проверка врагов (старая система)
            EnemyController oldEnemy = col.GetComponent<EnemyController>();
            if (oldEnemy != null && !oldEnemy.IsDead())
            {
                oldEnemy.Die();
                Debug.Log($"Враг {oldEnemy.name} раздавлен восстанавливающимся блоком!");
            }
            
            // Проверка врагов (новая система Lode Runner AI)
            LodeRunnerEnemyAI newEnemy = col.GetComponent<LodeRunnerEnemyAI>();
            if (newEnemy != null && !newEnemy.IsDead())
            {
                newEnemy.Die();
                Debug.Log($"Враг {newEnemy.name} раздавлен восстанавливающимся блоком!");
            }
        }
    }
    
    /// <summary>
    /// Определение когда враг падает в выкопанную яму
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем только если блок выкопан и это триггер-коллайдер
        if (!isDug) return;
        
        // Проверяем врагов (старая система)
        EnemyController oldEnemy = collision.GetComponent<EnemyController>();
        if (oldEnemy != null && !oldEnemy.IsStuckInHole() && !oldEnemy.IsDead())
        {
            // Враг упал в яму - застревает
            oldEnemy.StuckInHole();
            Debug.Log($"Враг {oldEnemy.name} застрял в яме!");
        }
        
        // Проверяем врагов (новая система Lode Runner AI)
        LodeRunnerEnemyAI newEnemy = collision.GetComponent<LodeRunnerEnemyAI>();
        if (newEnemy != null && !newEnemy.IsStuckInHole() && !newEnemy.IsDead())
        {
            // Враг упал в яму - застревает
            newEnemy.StuckInHole();
            Debug.Log($"Враг {newEnemy.name} застрял в яме! (новая система)");
        }
    }

    /// <summary>
    /// Мгновенно восстановить блок (для редактора/читов)
    /// </summary>
    public void InstantRestore()
    {
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
        }
        Restore();
    }

    /// <summary>
    /// Установить возможность копания
    /// </summary>
    public void SetDiggable(bool value)
    {
        canBeDigged = value;
    }

    public bool IsDug() => isDug;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = canBeDigged ? Color.yellow : Color.gray;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}

