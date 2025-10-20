using UnityEngine;

/// <summary>
/// Компонент лестницы для лазания
/// </summary>
public class Ladder : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Можно ли использовать эту лестницу")]
    [SerializeField] private bool isActive = true;

    [Header("Визуальная отладка")]
    [SerializeField] private Color gizmoColor = Color.green;

    private BoxCollider2D ladderCollider;

    private void Awake()
    {
        ladderCollider = GetComponent<BoxCollider2D>();
        
        if (ladderCollider == null)
        {
            ladderCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Настройка коллайдера как триггера
        ladderCollider.isTrigger = true;

        // Убедимся, что у объекта правильный слой
        if (gameObject.layer == 0) // Default layer
        {
            Debug.LogWarning($"Лестница '{gameObject.name}' находится на Default слое. Установите слой 'Ladder' в инспекторе!");
        }
    }

    public bool IsActive() => isActive;
    public void SetActive(bool value) => isActive = value;

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? gizmoColor : Color.gray;
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}

