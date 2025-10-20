using UnityEngine;

/// <summary>
/// Компонент для выравнивания пропсов при составлении уровней
/// X кратно 0.5, Y кратно 1
/// </summary>
public class LevelPropSnap : MonoBehaviour
{
    [Header("Настройки сетки")]
    [Tooltip("Шаг сетки по оси X")]
    public float gridStepX = 0.5f;
    
    [Tooltip("Шаг сетки по оси Y")]
    public float gridStepY = 1f;
    
    [Tooltip("Шаг сетки по оси Z")]
    public float gridStepZ = 0.5f;

    [Header("Оси для выравнивания")]
    [Tooltip("Выравнивать по оси X")]
    public bool snapX = true;
    
    [Tooltip("Выравнивать по оси Y")]
    public bool snapY = true;
    
    [Tooltip("Выравнивать по оси Z")]
    public bool snapZ = false;

    /// <summary>
    /// Выравнивает позицию объекта по сетке
    /// X выравнивается к значениям 0.5, 1.5, 2.5, 3.5 и т.д.
    /// Y выравнивается к значениям 0.5, 1.5, 2.5, 3.5 и т.д.
    /// </summary>
    public void SnapToGrid()
    {
        Vector3 currentPosition = transform.position;
        Vector3 snappedPosition = currentPosition;

        if (snapX)
        {
            // Выравнивание X к 0.5, 1.5, 2.5, 3.5 и т.д.
            snappedPosition.x = Mathf.Round(currentPosition.x - 0.5f) + 0.5f;
        }

        if (snapY)
        {
            // Выравнивание Y к 0.5, 1.5, 2.5, 3.5 и т.д.
            snappedPosition.y = Mathf.Round(currentPosition.y - 0.5f) + 0.5f;
        }

        if (snapZ)
        {
            // Z выравнивается стандартно (по умолчанию выключен)
            snappedPosition.z = Mathf.Round(currentPosition.z / gridStepZ) * gridStepZ;
        }

        transform.position = snappedPosition;
        
        Debug.Log($"Объект '{gameObject.name}' выровнен: {currentPosition:F2} → {snappedPosition:F2}");
    }

    /// <summary>
    /// Округляет значение до ближайшего кратного
    /// </summary>
    private float SnapValue(float value, float gridStep)
    {
        return Mathf.Round(value / gridStep) * gridStep;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Отрисовка сетки в редакторе (опционально)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = transform.position;
        
        // Рисуем маленький крест для визуализации текущей позиции
        float size = 0.2f;
        Gizmos.DrawLine(pos + Vector3.left * size, pos + Vector3.right * size);
        Gizmos.DrawLine(pos + Vector3.down * size, pos + Vector3.up * size);
        Gizmos.DrawLine(pos + Vector3.back * size, pos + Vector3.forward * size);
    }
#endif
}

