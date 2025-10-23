using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Классический AI врага в стиле Lode Runner (1983)
/// Использует простую сеточную логику с приоритетами движения
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class LodeRunnerEnemyAI : MonoBehaviour
{
    [Header("Скорость движения")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float climbSpeed = 2f;
    
    [Header("AI настройки")]
    [Tooltip("Простой режим (как в оригинале) или с поиском пути")]
    [SerializeField] private bool useSimpleAI = true;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float updatePathInterval = 0.5f;
    [SerializeField] private float alignmentThreshold = 0.3f; // Порог выравнивания с игроком
    
    [Header("Проверка окружения")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private LayerMask ropeLayer;
    
    [Header("Разделение врагов")]
    [SerializeField] private bool enableEnemySeparation = true;
    [SerializeField] private float separationRadius = 0.6f;
    [SerializeField] private float separationForce = 2f;
    
    [Header("Яма и возрождение")]
    [SerializeField] private float stuckInHoleTime = 3f;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform respawnPoint;
    
    // Компоненты
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private GridPathfinder pathfinder;
    
    // Состояния
    private EnemyState currentState = EnemyState.Idle;
    private bool isGrounded;
    private bool isOnLadder;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isStuckInHole = false;
    
    // Ссылки
    private Transform player;
    private Collider2D currentLadder;
    private float ladderCenterX;
    
    // AI - простой режим
    private Vector2 moveDirection;
    private float nextPathUpdate;
    
    // AI - режим поиска пути
    private List<Vector2Int> currentPath;
    private int currentPathIndex = 0;
    
    // Таймеры
    private float stuckTimer;
    
    public enum EnemyState
    {
        Idle,           // Стоит на месте
        Walking,        // Ходит по земле
        Climbing,       // Лазает по лестнице
        Falling,        // Падает
        StuckInHole,    // Застрял в яме
        Dead            // Мертв
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        pathfinder = GetComponent<GridPathfinder>();
        
        // Настройка физики
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Сохраняем точку появления
        if (respawnPoint == null)
        {
            GameObject spawnObj = new GameObject($"{name}_SpawnPoint");
            spawnObj.transform.position = transform.position;
            respawnPoint = spawnObj.transform;
        }
    }
    
    private void Start()
    {
        // Находим игрока
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        else
        {
            Debug.LogWarning($"Enemy {name}: Player не найден в сцене!");
        }
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Проверка окружения
        CheckEnvironment();
        
        // Обработка застревания в яме
        if (isStuckInHole)
        {
            HandleStuckInHole();
            return;
        }
        
        // AI поведение
        UpdateAI();
        
        // Обработка состояния
        UpdateState();
    }
    
    private void FixedUpdate()
    {
        if (isDead || isStuckInHole) return;
        
        // Разделение от других врагов
        if (enableEnemySeparation)
        {
            ApplyEnemySeparation();
        }
        
        // Применение движения
        ApplyMovement();
    }
    
    /// <summary>
    /// Проверка окружения
    /// </summary>
    private void CheckEnvironment()
    {
        if (groundCheck == null) return;
        
        // Проверка земли
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Проверка лестницы
        Collider2D ladderCollider = Physics2D.OverlapBox(
            transform.position,
            new Vector2(0.5f, 1f),
            0f,
            ladderLayer
        );
        
        if (ladderCollider != null)
        {
            isOnLadder = true;
            currentLadder = ladderCollider;
            ladderCenterX = ladderCollider.bounds.center.x;
        }
        else
        {
            isOnLadder = false;
            currentLadder = null;
        }
    }
    
    /// <summary>
    /// Обновление AI
    /// </summary>
    private void UpdateAI()
    {
        if (player == null)
        {
            moveDirection = Vector2.zero;
            return;
        }
        
        // Проверяем расстояние до игрока
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer > detectionRange)
        {
            // Игрок далеко - стоим на месте (как в оригинале)
            moveDirection = Vector2.zero;
            currentState = EnemyState.Idle;
            return;
        }
        
        // Обновляем направление движения через интервал
        if (Time.time >= nextPathUpdate)
        {
            if (useSimpleAI)
            {
                CalculateSimpleDirection();
            }
            else
            {
                CalculatePathfindingDirection();
            }
            
            nextPathUpdate = Time.time + updatePathInterval;
        }
    }
    
    /// <summary>
    /// ПРОСТАЯ ЛОГИКА (как в оригинальном Lode Runner)
    /// Жадный алгоритм с приоритетами
    /// </summary>
    private void CalculateSimpleDirection()
    {
        if (player == null) return;
        
        Vector2 dirToPlayer = player.position - transform.position;
        float deltaX = dirToPlayer.x;
        float deltaY = dirToPlayer.y;
        
        // Сбрасываем направление
        moveDirection = Vector2.zero;
        
        // ═══════════════════════════════════════════════════
        // ПРИОРИТЕТ 1: Если враг на лестнице
        // ═══════════════════════════════════════════════════
        if (isOnLadder)
        {
            // Если игрок выше - лезем вверх
            if (deltaY > alignmentThreshold)
            {
                moveDirection.y = 1f;
                Debug.Log($"{name}: На лестнице, лезу ВВЕРХ к игроку");
            }
            // Если игрок ниже - спускаемся вниз
            else if (deltaY < -alignmentThreshold)
            {
                moveDirection.y = -1f;
                Debug.Log($"{name}: На лестнице, спускаюсь ВНИЗ к игроку");
            }
            // Если примерно на той же высоте - идем горизонтально
            else
            {
                if (Mathf.Abs(deltaX) > alignmentThreshold)
                {
                    moveDirection.x = Mathf.Sign(deltaX);
                    Debug.Log($"{name}: На лестнице, иду ГОРИЗОНТАЛЬНО к игроку");
                }
            }
            
            return;
        }
        
        // ═══════════════════════════════════════════════════
        // ПРИОРИТЕТ 2: Если игрок находится выше/ниже
        // Ищем лестницу для вертикального сближения
        // ═══════════════════════════════════════════════════
        if (Mathf.Abs(deltaY) > 2f && isGrounded)
        {
            // Нужна лестница!
            Collider2D nearestLadder = FindNearestLadder(5f);
            
            if (nearestLadder != null)
            {
                float ladderX = nearestLadder.bounds.center.x;
                float distToLadder = Mathf.Abs(transform.position.x - ladderX);
                
                if (distToLadder > alignmentThreshold)
                {
                    // Идем к лестнице
                    moveDirection.x = Mathf.Sign(ladderX - transform.position.x);
                    Debug.Log($"{name}: Иду к лестнице для сближения по высоте");
                    return;
                }
                else
                {
                    // Мы у лестницы - поднимаемся/спускаемся
                    if (deltaY > 0)
                        moveDirection.y = 1f;
                    else
                        moveDirection.y = -1f;
                    
                    Debug.Log($"{name}: У лестницы, начинаю подъем/спуск");
                    return;
                }
            }
        }
        
        // ═══════════════════════════════════════════════════
        // ПРИОРИТЕТ 3: Если примерно на той же высоте
        // Идем горизонтально к игроку
        // ═══════════════════════════════════════════════════
        if (Mathf.Abs(deltaX) > alignmentThreshold)
        {
            moveDirection.x = Mathf.Sign(deltaX);
            Debug.Log($"{name}: Иду ГОРИЗОНТАЛЬНО к игроку");
        }
        else
        {
            // Враг почти поймал игрока по X
            moveDirection = Vector2.zero;
        }
    }
    
    /// <summary>
    /// УМНАЯ ЛОГИКА (с поиском пути через BFS)
    /// </summary>
    private void CalculatePathfindingDirection()
    {
        if (player == null || pathfinder == null) return;
        
        // Ищем путь к игроку
        currentPath = pathfinder.FindPath(transform.position, player.position);
        
        if (currentPath != null && currentPath.Count > 1)
        {
            // Есть путь - следуем по нему
            currentPathIndex = 1; // 0 - текущая позиция, 1 - следующий шаг
            
            Vector2Int nextStep = currentPath[currentPathIndex];
            Vector3 nextWorldPos = pathfinder.GridToWorld(nextStep);
            
            Vector2 dirToNext = (nextWorldPos - transform.position).normalized;
            
            moveDirection.x = Mathf.Abs(dirToNext.x) > 0.1f ? Mathf.Sign(dirToNext.x) : 0f;
            moveDirection.y = Mathf.Abs(dirToNext.y) > 0.1f ? Mathf.Sign(dirToNext.y) : 0f;
            
            Debug.Log($"{name}: Следую по пути (шаг {currentPathIndex}/{currentPath.Count})");
        }
        else
        {
            // Нет пути - используем простую логику
            CalculateSimpleDirection();
        }
    }
    
    /// <summary>
    /// Поиск ближайшей лестницы
    /// </summary>
    private Collider2D FindNearestLadder(float radius)
    {
        Collider2D[] ladders = Physics2D.OverlapCircleAll(transform.position, radius, ladderLayer);
        
        if (ladders.Length == 0) return null;
        
        Collider2D nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Collider2D ladder in ladders)
        {
            float distance = Vector2.Distance(transform.position, ladder.bounds.center);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = ladder;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Обновление состояния
    /// </summary>
    private void UpdateState()
    {
        // Лазание
        if (isOnLadder && Mathf.Abs(moveDirection.y) > 0.1f)
        {
            currentState = EnemyState.Climbing;
            rb.gravityScale = 0f;
        }
        else if (currentState == EnemyState.Climbing && isOnLadder)
        {
            rb.gravityScale = 0f;
            
            if (isGrounded && Mathf.Abs(moveDirection.y) < 0.1f)
            {
                currentState = EnemyState.Walking;
                rb.gravityScale = 3f;
            }
        }
        else if (isOnLadder && currentState != EnemyState.Climbing)
        {
            if (isGrounded)
            {
                currentState = EnemyState.Walking;
                rb.gravityScale = 3f;
            }
            else
            {
                rb.gravityScale = 0f;
                Vector2 vel = rb.linearVelocity;
                vel.y = 0f;
                rb.linearVelocity = vel;
            }
        }
        // Ходьба
        else if (isGrounded)
        {
            if (Mathf.Abs(moveDirection.x) > 0.1f)
                currentState = EnemyState.Walking;
            else
                currentState = EnemyState.Idle;
            
            rb.gravityScale = 3f;
        }
        // Падение
        else
        {
            currentState = EnemyState.Falling;
            rb.gravityScale = 3f;
        }
    }
    
    /// <summary>
    /// Разделение от других врагов (чтобы не слипались)
    /// </summary>
    private void ApplyEnemySeparation()
    {
        // Ищем всех врагов рядом
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        
        Vector2 separationVector = Vector2.zero;
        int enemyCount = 0;
        
        foreach (Collider2D col in nearbyEnemies)
        {
            // Пропускаем самого себя
            if (col.gameObject == gameObject) continue;
            
            // Проверяем, является ли объект врагом
            LodeRunnerEnemyAI otherEnemy = col.GetComponent<LodeRunnerEnemyAI>();
            EnemyController oldEnemy = col.GetComponent<EnemyController>();
            
            if (otherEnemy != null || oldEnemy != null)
            {
                // Вычисляем вектор отталкивания
                Vector2 directionAway = (Vector2)(transform.position - col.transform.position);
                float distance = directionAway.magnitude;
                
                if (distance > 0.01f && distance < separationRadius)
                {
                    // Чем ближе враг, тем сильнее отталкивание
                    float strength = 1f - (distance / separationRadius);
                    separationVector += directionAway.normalized * strength;
                    enemyCount++;
                }
            }
        }
        
        // Применяем силу разделения
        if (enemyCount > 0)
        {
            separationVector /= enemyCount;
            separationVector *= separationForce;
            
            // Применяем как небольшое смещение позиции
            Vector3 newPosition = transform.position + (Vector3)separationVector * Time.fixedDeltaTime;
            transform.position = newPosition;
        }
    }
    
    /// <summary>
    /// Применение движения
    /// </summary>
    private void ApplyMovement()
    {
        Vector2 velocity = rb.linearVelocity;
        
        switch (currentState)
        {
            case EnemyState.Idle:
                velocity.x = 0f;
                break;
                
            case EnemyState.Walking:
                velocity.x = moveDirection.x * walkSpeed;
                break;
                
            case EnemyState.Climbing:
                if (Mathf.Abs(moveDirection.y) > 0.1f)
                {
                    velocity.y = moveDirection.y * climbSpeed;
                    
                    // Центрирование на лестнице
                    if (currentLadder != null)
                    {
                        float currentX = transform.position.x;
                        float targetX = ladderCenterX;
                        
                        if (Mathf.Abs(currentX - targetX) > 0.01f)
                        {
                            float newX = Mathf.MoveTowards(currentX, targetX, 10f * Time.fixedDeltaTime);
                            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                        }
                    }
                    
                    velocity.x = moveDirection.x * walkSpeed * 0.1f;
                }
                else
                {
                    velocity.y = 0f;
                    velocity.x = moveDirection.x * walkSpeed * 0.5f;
                }
                break;
                
            case EnemyState.Falling:
                // При падении не контролируем X (как в оригинале)
                velocity.x = 0f;
                break;
        }
        
        rb.linearVelocity = velocity;
        
        // Поворот
        if (moveDirection.x > 0.1f && !isFacingRight)
        {
            Flip();
        }
        else if (moveDirection.x < -0.1f && isFacingRight)
        {
            Flip();
        }
    }
    
    /// <summary>
    /// Поворот врага
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    /// <summary>
    /// Обработка застревания в яме
    /// </summary>
    private void HandleStuckInHole()
    {
        stuckTimer += Time.deltaTime;
        
        if (stuckTimer >= stuckInHoleTime)
        {
            Respawn();
        }
    }
    
    /// <summary>
    /// Враг застрял в яме
    /// </summary>
    public void StuckInHole()
    {
        if (isStuckInHole) return;
        
        isStuckInHole = true;
        stuckTimer = 0f;
        currentState = EnemyState.StuckInHole;
        
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        Debug.Log($"Enemy {name} застрял в яме!");
    }
    
    /// <summary>
    /// Смерть врага
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        if (capsuleCollider != null)
            capsuleCollider.enabled = false;
        
        Debug.Log($"Enemy {name} умер!");
        
        StartCoroutine(RespawnAfterDelay());
    }
    
    /// <summary>
    /// Возрождение врага
    /// </summary>
    private void Respawn()
    {
        isDead = false;
        isStuckInHole = false;
        stuckTimer = 0f;
        
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        
        rb.gravityScale = 3f;
        
        if (capsuleCollider != null)
            capsuleCollider.enabled = true;
        
        currentState = EnemyState.Idle;
        
        Debug.Log($"Enemy {name} возродился!");
    }
    
    /// <summary>
    /// Корутина возрождения
    /// </summary>
    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }
    
    /// <summary>
    /// Столкновение с игроком
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isStuckInHole) return;
        
        PlayerController playerCtrl = collision.gameObject.GetComponent<PlayerController>();
        if (playerCtrl != null && !playerCtrl.IsDead())
        {
            playerCtrl.Die();
            Debug.Log("Враг поймал игрока!");
        }
    }
    
    /// <summary>
    /// Gizmos для отладки
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Зона обнаружения
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Проверка земли
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Направление движения
        if (moveDirection != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
        
        // Линия к игроку
        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
            
            #if UNITY_EDITOR
            string stateText = $"{currentState}\nSimple AI: {useSimpleAI}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, stateText);
            #endif
        }
    }
    
    // Публичные методы
    public EnemyState GetState() => currentState;
    public bool IsDead() => isDead;
    public bool IsStuckInHole() => isStuckInHole;
}

