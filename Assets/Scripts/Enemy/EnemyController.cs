using UnityEngine;

/// <summary>
/// Контроллер врага в стиле Lode Runner
/// Враг преследует игрока, лазает по лестницам, падает в ямы
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Скорость движения")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float climbSpeed = 2f;
    
    [Header("Проверка окружения")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("AI настройки")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float updatePathInterval = 0.5f;
    [SerializeField] private float stuckCheckInterval = 1f;
    [SerializeField] private float minStuckDistance = 0.2f;
    [SerializeField] private float wanderMinTime = 2f;
    [SerializeField] private float wanderMaxTime = 4f;
    [SerializeField] private bool alwaysMoving = true; // Враг всегда в движении
    [SerializeField] private float verticalDistanceThreshold = 2f; // Если игрок выше/ниже на эту дистанцию - искать лестницу
    
    [Header("Яма и возрождение")]
    [SerializeField] private float stuckInHoleTime = 3f;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform respawnPoint;
    
    [Header("Визуал")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // Компоненты
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    
    // Состояния
    private EnemyState currentState = EnemyState.Patrolling;
    private bool isGrounded;
    private bool isOnLadder;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isStuckInHole = false;
    
    // Ссылки
    private Transform player;
    private Collider2D currentLadder;
    private float ladderCenterX;
    
    // AI
    private Vector2 targetPosition;
    private float nextPathUpdate;
    private float stuckTimer;
    
    // Обнаружение застревания
    private Vector3 lastPositionCheck;
    private float nextStuckCheck;
    private bool isWandering = false;
    private float wanderEndTime;
    private Vector2 wanderDirection;
    
    // Патрулирование
    private float patrolDirection = 1f; // 1 = вправо, -1 = влево
    
    // Движение
    private float horizontalInput;
    private float verticalInput;
    
    /// <summary>
    /// Состояния врага
    /// </summary>
    public enum EnemyState
    {
        Patrolling,     // Патрулирует (когда игрок далеко)
        Walking,        // Ходит по земле
        Climbing,       // Лазает по лестнице
        Chasing,        // Преследует игрока
        Wandering,      // Блуждает (застрял, ищет путь)
        Falling,        // Падает
        StuckInHole,    // Застрял в яме
        Dead            // Мертв (временно)
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Настройка физики
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Сохраняем точку появления
        if (respawnPoint == null)
        {
            respawnPoint = transform;
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
        
        // Инициализация отслеживания застревания
        lastPositionCheck = transform.position;
        nextStuckCheck = Time.time + stuckCheckInterval;
        
        // Случайное начальное направление патрулирования
        patrolDirection = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
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
        HandleState();
    }

    private void FixedUpdate()
    {
        if (isDead || isStuckInHole) return;
        
        // Применение движения
        ApplyMovement();
    }

    /// <summary>
    /// Проверка окружения (земля, лестницы)
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
            // Если нет игрока - патрулируем
            if (alwaysMoving)
            {
                Patrol();
            }
            return;
        }
        
        // Проверка на блуждание (если застрял)
        if (isWandering)
        {
            HandleWandering();
            return;
        }
        
        // Проверяем расстояние до игрока
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            // Игрок в зоне обнаружения - преследуем
            currentState = EnemyState.Chasing;
            
            // Проверка застревания
            CheckIfStuck();
            
            // Обновляем путь через интервал
            if (Time.time >= nextPathUpdate)
            {
                CalculateSmartPath();
                nextPathUpdate = Time.time + updatePathInterval;
            }
        }
        else
        {
            // Игрок далеко - патрулируем (никогда не стоим)
            if (alwaysMoving)
            {
                currentState = EnemyState.Patrolling;
                Patrol();
            }
            else
            {
                horizontalInput = 0f;
                verticalInput = 0f;
            }
        }
    }
    
    /// <summary>
    /// Патрулирование (постоянное движение)
    /// </summary>
    private void Patrol()
    {
        // Двигаемся в текущем направлении
        horizontalInput = patrolDirection;
        verticalInput = 0f;
        
        // Проверка края платформы или стены
        if (ShouldTurnAround())
        {
            patrolDirection *= -1f; // Разворачиваемся
        }
    }
    
    /// <summary>
    /// Проверка нужно ли развернуться при патрулировании
    /// </summary>
    private bool ShouldTurnAround()
    {
        // Проверяем стену впереди
        Vector2 wallCheckPos = new Vector2(
            transform.position.x + patrolDirection * 0.6f,
            transform.position.y
        );
        
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallCheckPos,
            Vector2.down,
            1f,
            groundLayer
        );
        
        // Если нет земли впереди - разворачиваемся (край платформы)
        if (!wallHit.collider && isGrounded)
        {
            return true;
        }
        
        // Проверяем стену сбоку
        RaycastHit2D sideHit = Physics2D.Raycast(
            transform.position,
            Vector2.right * patrolDirection,
            0.6f,
            groundLayer
        );
        
        // Если есть стена - разворачиваемся
        if (sideHit.collider)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Проверка, застрял ли враг (не двигается долгое время)
    /// </summary>
    private void CheckIfStuck()
    {
        // Проверяем позицию через интервал
        if (Time.time >= nextStuckCheck)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPositionCheck);
            
            // Если почти не сдвинулся - считаем застрявшим
            if (distanceMoved < minStuckDistance)
            {
                Debug.Log($"Enemy {name} застрял! Начинаю блуждание...");
                StartWandering();
            }
            
            // Обновляем проверку
            lastPositionCheck = transform.position;
            nextStuckCheck = Time.time + stuckCheckInterval;
        }
    }
    
    /// <summary>
    /// Начать блуждание (случайное движение)
    /// </summary>
    private void StartWandering()
    {
        isWandering = true;
        currentState = EnemyState.Wandering;
        
        // Случайное время блуждания (2-4 секунды)
        float wanderDuration = Random.Range(wanderMinTime, wanderMaxTime);
        wanderEndTime = Time.time + wanderDuration;
        
        // Случайное направление: влево или вправо
        wanderDirection = new Vector2(Random.Range(-1f, 1f) > 0 ? 1f : -1f, 0f);
        
        Debug.Log($"Enemy {name} блуждает {wanderDuration:F1} секунд в направлении {wanderDirection.x}");
    }
    
    /// <summary>
    /// Обработка блуждания
    /// </summary>
    private void HandleWandering()
    {
        // Двигаемся в случайном направлении
        horizontalInput = wanderDirection.x;
        verticalInput = 0f;
        
        // Проверяем, закончилось ли время блуждания
        if (Time.time >= wanderEndTime)
        {
            isWandering = false;
            lastPositionCheck = transform.position; // Сброс проверки застревания
            nextStuckCheck = Time.time + stuckCheckInterval;
            Debug.Log($"Enemy {name} закончил блуждание, возвращаюсь к преследованию");
        }
    }

    /// <summary>
    /// Умное вычисление пути к игроку с активным использованием лестниц
    /// </summary>
    private void CalculateSmartPath()
    {
        if (player == null) return;
        
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = player.position.y - transform.position.y;
        
        // ПРИОРИТЕТ: Если игрок на другой высоте и есть лестница - используем её!
        if (Mathf.Abs(verticalDistance) > verticalDistanceThreshold)
        {
            // Игрок выше или ниже - нужно использовать лестницу
            
            if (isOnLadder)
            {
                // Мы УЖЕ на лестнице - лезем вверх/вниз к игроку
                verticalInput = verticalDistance > 0 ? 1f : -1f;
                
                // Небольшое горизонтальное движение для центрирования
                if (horizontalDistance > 0.5f)
                {
                    horizontalInput = directionToPlayer.x > 0 ? 0.2f : -0.2f;
                }
                else
                {
                    horizontalInput = 0f;
                }
                
                Debug.Log($"Enemy {name}: На лестнице, двигаюсь к игроку (вертикаль)");
                return;
            }
            else if (!isOnLadder && isGrounded)
            {
                // Мы НЕ на лестнице - ищем лестницу поблизости
                Collider2D nearbyLadder = FindNearestLadder();
                
                if (nearbyLadder != null)
                {
                    // Нашли лестницу - идем к ней
                    float ladderX = nearbyLadder.bounds.center.x;
                    float distanceToLadder = Mathf.Abs(transform.position.x - ladderX);
                    
                    if (distanceToLadder > 0.3f)
                    {
                        // Идем к лестнице
                        horizontalInput = (ladderX - transform.position.x) > 0 ? 1f : -1f;
                        verticalInput = 0f;
                        Debug.Log($"Enemy {name}: Иду к лестнице для сближения с игроком");
                        return;
                    }
                    else
                    {
                        // Мы рядом с лестницей - поднимаемся
                        verticalInput = verticalDistance > 0 ? 1f : -1f;
                        horizontalInput = 0f;
                        return;
                    }
                }
            }
        }
        
        // СТАНДАРТНАЯ ЛОГИКА: Если игрок примерно на той же высоте
        
        // Горизонтальное движение (приоритет)
        if (horizontalDistance > 0.5f)
        {
            horizontalInput = directionToPlayer.x > 0 ? 1f : -1f;
        }
        else
        {
            horizontalInput = 0f;
        }
        
        // Вертикальное движение (только если на лестнице)
        if (isOnLadder && Mathf.Abs(verticalDistance) > 0.5f)
        {
            verticalInput = verticalDistance > 0 ? 1f : -1f;
        }
        else
        {
            verticalInput = 0f;
        }
    }
    
    /// <summary>
    /// Поиск ближайшей лестницы в радиусе
    /// </summary>
    private Collider2D FindNearestLadder()
    {
        Collider2D[] ladders = Physics2D.OverlapCircleAll(transform.position, 5f, ladderLayer);
        
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
    /// Обработка состояния
    /// </summary>
    private void HandleState()
    {
        // Лазание по лестнице
        if (isOnLadder && Mathf.Abs(verticalInput) > 0.1f)
        {
            currentState = EnemyState.Climbing;
            rb.gravityScale = 0f;
        }
        else if (currentState == EnemyState.Climbing && isOnLadder)
        {
            // Продолжаем быть на лестнице
            rb.gravityScale = 0f;
            
            if (isGrounded && Mathf.Abs(verticalInput) < 0.1f)
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
            currentState = EnemyState.Walking;
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
    /// Применение движения
    /// </summary>
    private void ApplyMovement()
    {
        Vector2 velocity = rb.linearVelocity;
        
        switch (currentState)
        {
            case EnemyState.Patrolling:
            case EnemyState.Walking:
            case EnemyState.Chasing:
            case EnemyState.Wandering:
                velocity.x = horizontalInput * walkSpeed;
                break;
                
            case EnemyState.Climbing:
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    velocity.y = verticalInput * climbSpeed;
                    
                    // Центрирование на лестнице
                    float currentX = transform.position.x;
                    float targetX = ladderCenterX;
                    
                    if (Mathf.Abs(currentX - targetX) > 0.01f)
                    {
                        float newX = Mathf.MoveTowards(currentX, targetX, 10f * Time.fixedDeltaTime);
                        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                    }
                    
                    velocity.x = horizontalInput * walkSpeed * 0.1f;
                }
                else
                {
                    velocity.y = 0f;
                    velocity.x = horizontalInput * walkSpeed * 0.5f;
                }
                break;
                
            case EnemyState.Falling:
                velocity.x = 0f;
                break;
        }
        
        rb.linearVelocity = velocity;
        
        // Поворот
        if (horizontalInput > 0.1f && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < -0.1f && isFacingRight)
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
            // Время истекло - возрождаемся
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
        
        // Останавливаем движение
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
        
        // Останавливаем движение
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Отключаем коллизии
        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }
        
        // Визуальный эффект
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        }
        
        Debug.Log($"Enemy {name} умер!");
        
        // Возрождение через задержку
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
        
        // Сбрасываем блуждание
        isWandering = false;
        lastPositionCheck = transform.position;
        nextStuckCheck = Time.time + stuckCheckInterval;
        
        // Восстанавливаем позицию
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        
        // Восстанавливаем физику
        rb.gravityScale = 3f;
        
        // Включаем коллизии
        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = true;
        }
        
        // Восстанавливаем визуал
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Сбрасываем состояние
        currentState = EnemyState.Patrolling;
        
        // Новое случайное направление патрулирования
        patrolDirection = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        
        Debug.Log($"Enemy {name} возродился!");
    }

    /// <summary>
    /// Корутина возрождения с задержкой
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
        
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && !player.IsDead())
        {
            // Убиваем игрока
            player.Die();
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
        
        // Линия к игроку (при преследовании)
        if (player != null && currentState == EnemyState.Chasing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
        
        // Индикатор блуждания
        if (isWandering)
        {
            Gizmos.color = Color.magenta;
            Vector3 wanderTarget = transform.position + new Vector3(wanderDirection.x * 2f, 0f, 0f);
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawWireSphere(wanderTarget, 0.3f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "WANDERING");
            #endif
        }
        
        // Индикатор патрулирования
        if (currentState == EnemyState.Patrolling)
        {
            Gizmos.color = Color.cyan;
            Vector3 patrolTarget = transform.position + new Vector3(patrolDirection * 1.5f, 0f, 0f);
            Gizmos.DrawLine(transform.position, patrolTarget);
            Gizmos.DrawWireSphere(patrolTarget, 0.2f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "PATROL");
            #endif
        }
        
        // Визуализация поиска лестниц
        if (player != null && Mathf.Abs(player.position.y - transform.position.y) > verticalDistanceThreshold)
        {
            // Показываем радиус поиска лестниц
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 5f);
            
            // Показываем ближайшую лестницу если есть
            Collider2D nearestLadder = FindNearestLadder();
            if (nearestLadder != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, nearestLadder.bounds.center);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(nearestLadder.bounds.center, "Target Ladder");
                #endif
            }
        }
    }

    // Публичные методы
    public EnemyState GetState() => currentState;
    public bool IsDead() => isDead;
    public bool IsStuckInHole() => isStuckInHole;
}

