using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Сеточный поиск пути для врагов в стиле Lode Runner
/// Использует BFS (Breadth-First Search) для поиска кратчайшего пути
/// </summary>
public class GridPathfinder : MonoBehaviour
{
    [Header("Настройки сетки")]
    [SerializeField] private float gridStepX = 0.5f;
    [SerializeField] private float gridStepY = 1f;
    [SerializeField] private int maxPathLength = 50; // Максимальная длина пути
    
    [Header("Слои")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private LayerMask ropeLayer;
    
    [Header("Визуализация")]
    [SerializeField] private bool drawDebug = true;
    
    // Кэш для оптимизации
    private Dictionary<Vector2Int, TileType> tileCache = new Dictionary<Vector2Int, TileType>();
    private float nextCacheUpdate = 0f;
    private const float CACHE_UPDATE_INTERVAL = 0.5f;
    
    // Последний найденный путь
    private List<Vector2Int> currentPath = new List<Vector2Int>();
    
    /// <summary>
    /// Типы клеток в игре
    /// </summary>
    public enum TileType
    {
        Empty,      // Пустота (можно падать)
        Ground,     // Земля (можно стоять)
        Ladder,     // Лестница (можно лазать)
        Rope,       // Веревка (можно ползать)
        Obstacle    // Препятствие (нельзя пройти)
    }
    
    /// <summary>
    /// Узел для поиска пути
    /// </summary>
    private class PathNode
    {
        public Vector2Int position;
        public PathNode parent;
        public int cost; // Стоимость пути от начала
        
        public PathNode(Vector2Int pos, PathNode parent, int cost)
        {
            this.position = pos;
            this.parent = parent;
            this.cost = cost;
        }
    }
    
    private void Update()
    {
        // Периодически обновляем кэш тайлов
        if (Time.time >= nextCacheUpdate)
        {
            tileCache.Clear();
            nextCacheUpdate = Time.time + CACHE_UPDATE_INTERVAL;
        }
    }
    
    /// <summary>
    /// Найти путь от start к target используя BFS
    /// </summary>
    public List<Vector2Int> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        Vector2Int start = WorldToGrid(startWorld);
        Vector2Int target = WorldToGrid(targetWorld);
        
        currentPath.Clear();
        
        // BFS поиск пути
        Queue<PathNode> queue = new Queue<PathNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        PathNode startNode = new PathNode(start, null, 0);
        queue.Enqueue(startNode);
        visited.Add(start);
        
        PathNode targetNode = null;
        int iterations = 0;
        
        while (queue.Count > 0 && iterations < maxPathLength * 4)
        {
            iterations++;
            PathNode current = queue.Dequeue();
            
            // Достигли цели
            if (current.position == target)
            {
                targetNode = current;
                break;
            }
            
            // Проверяем все возможные направления
            List<Vector2Int> neighbors = GetValidNeighbors(current.position);
            
            foreach (Vector2Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    int moveCost = GetMoveCost(current.position, neighbor);
                    PathNode neighborNode = new PathNode(neighbor, current, current.cost + moveCost);
                    queue.Enqueue(neighborNode);
                }
            }
        }
        
        // Восстанавливаем путь
        if (targetNode != null)
        {
            PathNode node = targetNode;
            while (node != null)
            {
                currentPath.Insert(0, node.position);
                node = node.parent;
            }
        }
        
        return currentPath;
    }
    
    /// <summary>
    /// Получить список доступных соседних клеток
    /// </summary>
    private List<Vector2Int> GetValidNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        TileType currentType = GetTileType(pos);
        
        // Горизонтальные движения (влево, вправо)
        Vector2Int left = new Vector2Int(pos.x - 1, pos.y);
        Vector2Int right = new Vector2Int(pos.x + 1, pos.y);
        
        // Вертикальные движения (вверх, вниз)
        Vector2Int up = new Vector2Int(pos.x, pos.y + 1);
        Vector2Int down = new Vector2Int(pos.x, pos.y - 1);
        
        // Проверяем возможность движения в каждом направлении
        
        // ВЛЕВО/ВПРАВО
        if (CanMoveHorizontally(pos, left))
            neighbors.Add(left);
        if (CanMoveHorizontally(pos, right))
            neighbors.Add(right);
        
        // ВВЕРХ (только на лестнице)
        if (currentType == TileType.Ladder)
        {
            TileType upType = GetTileType(up);
            if (upType == TileType.Ladder || upType == TileType.Empty)
                neighbors.Add(up);
        }
        
        // ВНИЗ (на лестнице или падение)
        if (currentType == TileType.Ladder)
        {
            TileType downType = GetTileType(down);
            if (downType == TileType.Ladder || downType == TileType.Empty || downType == TileType.Ground)
                neighbors.Add(down);
        }
        else if (currentType != TileType.Ground)
        {
            // Падение вниз (если не на земле)
            neighbors.Add(down);
        }
        
        return neighbors;
    }
    
    /// <summary>
    /// Проверка возможности горизонтального движения
    /// </summary>
    private bool CanMoveHorizontally(Vector2Int from, Vector2Int to)
    {
        TileType fromType = GetTileType(from);
        TileType toType = GetTileType(to);
        
        // Нельзя идти в препятствие
        if (toType == TileType.Obstacle)
            return false;
        
        // На земле можно идти на землю, лестницу или веревку
        if (fromType == TileType.Ground)
        {
            // Проверяем что под целевой клеткой есть земля или лестница
            Vector2Int below = new Vector2Int(to.x, to.y - 1);
            TileType belowType = GetTileType(below);
            
            return toType == TileType.Ground || 
                   toType == TileType.Ladder || 
                   toType == TileType.Rope ||
                   belowType == TileType.Ground ||
                   belowType == TileType.Ladder;
        }
        
        // На лестнице можно идти горизонтально на любую проходимую клетку
        if (fromType == TileType.Ladder)
        {
            return toType != TileType.Obstacle;
        }
        
        // На веревке можно ползти по веревке
        if (fromType == TileType.Rope)
        {
            return toType == TileType.Rope || toType == TileType.Ladder;
        }
        
        return false;
    }
    
    /// <summary>
    /// Получить стоимость движения между клетками
    /// </summary>
    private int GetMoveCost(Vector2Int from, Vector2Int to)
    {
        TileType fromType = GetTileType(from);
        TileType toType = GetTileType(to);
        
        // Падение вниз - дорого (чтобы враг не прыгал в ямы)
        if (to.y < from.y)
            return 3;
        
        // Лазание по лестнице
        if (fromType == TileType.Ladder || toType == TileType.Ladder)
            return 1;
        
        // Ходьба по земле
        if (fromType == TileType.Ground && toType == TileType.Ground)
            return 1;
        
        // По умолчанию
        return 2;
    }
    
    /// <summary>
    /// Определить тип клетки в позиции
    /// </summary>
    public TileType GetTileType(Vector2Int gridPos)
    {
        // Проверяем кэш
        if (tileCache.TryGetValue(gridPos, out TileType cachedType))
            return cachedType;
        
        Vector3 worldPos = GridToWorld(gridPos);
        TileType type = TileType.Empty;
        
        // Проверяем наличие земли
        Collider2D groundHit = Physics2D.OverlapBox(
            worldPos,
            new Vector2(gridStepX * 0.8f, gridStepY * 0.8f),
            0f,
            groundLayer
        );
        
        if (groundHit != null)
        {
            type = TileType.Ground;
        }
        else
        {
            // Проверяем лестницу
            Collider2D ladderHit = Physics2D.OverlapBox(
                worldPos,
                new Vector2(gridStepX * 0.8f, gridStepY * 0.8f),
                0f,
                ladderLayer
            );
            
            if (ladderHit != null)
            {
                type = TileType.Ladder;
            }
            else
            {
                // Проверяем веревку
                Collider2D ropeHit = Physics2D.OverlapBox(
                    worldPos,
                    new Vector2(gridStepX * 0.8f, gridStepY * 0.8f),
                    0f,
                    ropeLayer
                );
                
                if (ropeHit != null)
                {
                    type = TileType.Rope;
                }
            }
        }
        
        // Сохраняем в кэш
        tileCache[gridPos] = type;
        
        return type;
    }
    
    /// <summary>
    /// Конвертация мировых координат в сеточные
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridStepX);
        int y = Mathf.RoundToInt(worldPos.y / gridStepY);
        return new Vector2Int(x, y);
    }
    
    /// <summary>
    /// Конвертация сеточных координат в мировые
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = gridPos.x * gridStepX;
        float y = gridPos.y * gridStepY;
        return new Vector3(x, y, 0f);
    }
    
    /// <summary>
    /// Получить последний найденный путь
    /// </summary>
    public List<Vector2Int> GetCurrentPath()
    {
        return currentPath;
    }
    
    /// <summary>
    /// Визуализация для отладки
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawDebug || currentPath == null || currentPath.Count == 0)
            return;
        
        // Рисуем путь
        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 from = GridToWorld(currentPath[i]);
            Vector3 to = GridToWorld(currentPath[i + 1]);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.2f);
        }
        
        if (currentPath.Count > 0)
        {
            Vector3 lastPos = GridToWorld(currentPath[currentPath.Count - 1]);
            Gizmos.DrawWireSphere(lastPos, 0.2f);
        }
    }
}

