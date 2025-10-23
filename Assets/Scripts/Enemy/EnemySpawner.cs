using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнер врагов для уровня
/// Создает врагов в заданных точках при старте
/// Поддерживает обе системы: EnemyController и LodeRunnerEnemyAI
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private int maxEnemies = 5;
    
    [Header("Настройки врагов")]
    [SerializeField] private float enemyWalkSpeed = 3f;
    [SerializeField] private float enemyClimbSpeed = 2f;
    [SerializeField] private float detectionRange = 15f;
    
    // Списки для хранения врагов обеих систем
    private List<GameObject> spawnedEnemyObjects = new List<GameObject>();
    private EnemySystemType enemyType = EnemySystemType.Unknown;
    
    public enum EnemySystemType
    {
        Unknown,
        OldSystem,      // EnemyController
        NewSystem       // LodeRunnerEnemyAI
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllEnemies();
        }
    }

    /// <summary>
    /// Создать всех врагов в точках спавна
    /// </summary>
    public void SpawnAllEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy Prefab не назначен!");
            return;
        }
        
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: Нет точек спавна!");
            return;
        }
        
        // Определяем тип врага из префаба
        DetectEnemyType();
        
        // Ограничиваем количество врагов
        int enemiesToSpawn = Mathf.Min(spawnPoints.Length, maxEnemies);
        spawnedEnemyObjects.Clear();
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (spawnPoints[i] != null)
            {
                SpawnEnemyAt(spawnPoints[i], i);
            }
        }
        
        string systemName = enemyType == EnemySystemType.OldSystem ? "старая система (EnemyController)" : 
                           enemyType == EnemySystemType.NewSystem ? "новая система (LodeRunnerEnemyAI)" : "неизвестная";
        Debug.Log($"EnemySpawner: Создано {spawnedEnemyObjects.Count} врагов ({systemName})");
    }
    
    /// <summary>
    /// Определить тип врага (старая или новая система)
    /// </summary>
    private void DetectEnemyType()
    {
        if (enemyPrefab == null)
        {
            enemyType = EnemySystemType.Unknown;
            return;
        }
        
        // Проверяем старую систему
        if (enemyPrefab.GetComponent<EnemyController>() != null)
        {
            enemyType = EnemySystemType.OldSystem;
            Debug.Log("EnemySpawner: Обнаружена старая система врагов (EnemyController)");
            return;
        }
        
        // Проверяем новую систему
        if (enemyPrefab.GetComponent<LodeRunnerEnemyAI>() != null)
        {
            enemyType = EnemySystemType.NewSystem;
            Debug.Log("EnemySpawner: Обнаружена новая система врагов (LodeRunnerEnemyAI)");
            return;
        }
        
        enemyType = EnemySystemType.Unknown;
        Debug.LogWarning("EnemySpawner: У префаба нет ни EnemyController, ни LodeRunnerEnemyAI!");
    }

    /// <summary>
    /// Создать врага в конкретной точке
    /// </summary>
    private void SpawnEnemyAt(Transform spawnPoint, int index)
    {
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemyObject.name = $"Enemy_{index + 1}";
        enemyObject.transform.parent = transform; // Делаем дочерним объектом спавнера
        
        spawnedEnemyObjects.Add(enemyObject);
        
        // В зависимости от типа системы - разная логика
        if (enemyType == EnemySystemType.OldSystem)
        {
            EnemyController oldEnemy = enemyObject.GetComponent<EnemyController>();
            if (oldEnemy != null)
            {
                Debug.Log($"Создан враг {enemyObject.name} (старая система)");
            }
        }
        else if (enemyType == EnemySystemType.NewSystem)
        {
            LodeRunnerEnemyAI newEnemy = enemyObject.GetComponent<LodeRunnerEnemyAI>();
            if (newEnemy != null)
            {
                Debug.Log($"Создан враг {enemyObject.name} (новая система)");
            }
        }
    }

    /// <summary>
    /// Получить всех созданных врагов (GameObject)
    /// </summary>
    public List<GameObject> GetSpawnedEnemies()
    {
        return spawnedEnemyObjects;
    }

    /// <summary>
    /// Подсчет живых врагов (работает с обеими системами)
    /// </summary>
    public int GetAliveEnemiesCount()
    {
        if (spawnedEnemyObjects == null || spawnedEnemyObjects.Count == 0) return 0;
        
        int count = 0;
        
        foreach (GameObject enemyObj in spawnedEnemyObjects)
        {
            if (enemyObj == null) continue;
            
            // Проверяем старую систему
            EnemyController oldEnemy = enemyObj.GetComponent<EnemyController>();
            if (oldEnemy != null)
            {
                if (!oldEnemy.IsDead())
                    count++;
                continue;
            }
            
            // Проверяем новую систему
            LodeRunnerEnemyAI newEnemy = enemyObj.GetComponent<LodeRunnerEnemyAI>();
            if (newEnemy != null)
            {
                if (!newEnemy.IsDead())
                    count++;
                continue;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// Получить тип используемой системы врагов
    /// </summary>
    public EnemySystemType GetEnemySystemType()
    {
        return enemyType;
    }

    /// <summary>
    /// Визуализация точек спавна
    /// </summary>
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        
        Gizmos.color = Color.red;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 0.7f, $"Spawn {i + 1}");
                #endif
            }
        }
    }
}

