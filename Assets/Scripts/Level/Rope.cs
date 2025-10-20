using UnityEngine;

/// <summary>
/// Компонент горизонтальной веревки для движения
/// </summary>
public class Rope : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Можно ли использовать эту веревку")]
    [SerializeField] private bool isActive = true;

    [Header("Визуальная отладка")]
    [SerializeField] private Color gizmoColor = Color.cyan;

    private BoxCollider2D ropeCollider;

    private void Awake()
    {
        ropeCollider = GetComponent<BoxCollider2D>();
        
        if (ropeCollider == null)
        {
            ropeCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Настройка коллайдера как триггера
        ropeCollider.isTrigger = true;

        // Убедимся, что у объекта правильный слой
        if (gameObject.layer == 0) // Default layer
        {
            Debug.LogWarning($"Веревка '{gameObject.name}' находится на Default слое. Установите слой 'Rope' в инспекторе!");
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
            Gizmos.DrawWireCube(transform.position, new Vector3(2f, 0.3f, 0f));
        }
    }
}

