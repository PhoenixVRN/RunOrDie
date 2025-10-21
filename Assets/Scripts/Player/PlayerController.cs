using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Контроллер игрока в стиле Lode Runner
/// Поддержка: ходьба, лазание по лестницам, веревкам, копание ям
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Скорость движения")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float ropeSpeed = 4f;
    
    [Header("Настройки лестницы")]
    [SerializeField] private bool centerOnLadder = true;
    [SerializeField] private float ladderCenterSpeed = 10f;
    
    [Header("Настройки падения")]
    [SerializeField] private bool alignOnFalling = true;
    [Tooltip("Скорость выравнивания к оси при падении")]
    [SerializeField] private float fallingAlignSpeed = 3f;

    [Header("Проверка земли и объектов")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private LayerMask ropeLayer;
    [SerializeField] private LayerMask diggableLayer;

    [Header("Копание ям")]
    [SerializeField] private float digRange = 1f;
    [SerializeField] private float digCooldown = 0.5f;
    [SerializeField] private float holeRestoreTime = 5f;

    [Header("Настройки")]
    [SerializeField] private bool canMove = true;
    
    [Header("Смерть и возрождение")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform respawnPoint;

    // Компоненты
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Input Actions
    private InputAction moveAction;
    private InputAction digLeftAction;
    private InputAction digRightAction;

    // Состояния
    private PlayerState currentState = PlayerState.Walking;
    private bool isGrounded;
    private bool isOnLadder;
    private bool isOnRope;
    private bool isFacingRight = true;
    private bool isDead = false;
    
    // Ссылки на объекты окружения
    private Collider2D currentLadder;
    private Collider2D currentRope;
    private float ladderCenterX;

    // Ввод
    private Vector2 moveInput;
    private float horizontalInput;
    private float verticalInput;
    private bool digRequest;

    // Копание
    private float lastDigTime;

    // Константы для анимации (опционально)
    private readonly int AnimMoveX = Animator.StringToHash("MoveX");
    private readonly int AnimMoveY = Animator.StringToHash("MoveY");
    private readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
    private readonly int AnimIsOnRope = Animator.StringToHash("IsOnRope");
    private readonly int AnimDig = Animator.StringToHash("Dig");

    /// <summary>
    /// Состояния персонажа
    /// </summary>
    public enum PlayerState
    {
        Walking,    // Ходьба по земле
        Climbing,   // Лазание по лестнице
        OnRope,     // На веревке
        Digging,    // Копает яму
        Falling     // Падает
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Настройка Rigidbody2D для платформера
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Инициализация Input Actions
        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move");
                digLeftAction = playerMap.FindAction("DigLeft");
                digRightAction = playerMap.FindAction("DigRight");
            }
            else
            {
                Debug.LogError("Action Map 'Player' не найдена в InputActionAsset!");
            }
        }
        else
        {
            Debug.LogWarning("InputActionAsset не назначен в PlayerController! Назначьте InputSystem_Actions в инспекторе.");
        }
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    private void Update()
    {
        if (!canMove || isDead) return;

        // Проверка критичных компонентов
        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck не назначен! Выберите Player → ПКМ → Lode Runner → Настроить как игрока", this);
            return;
        }

        // Проверка Input Actions
        if (inputActions == null || moveAction == null)
        {
            Debug.LogWarning("InputActions не назначен! Движение не будет работать. Назначьте InputSystem_Actions в Inspector.", this);
        }

        // Получение ввода
        GetInput();

        // Проверка окружения
        CheckEnvironment();

        // Обработка состояний
        HandleState();

        // Обработка копания
        HandleDigging();

        // Обновление анимации
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!canMove || isDead) return;

        // Применение движения в зависимости от состояния
        ApplyMovement();
    }

    // Отладочный метод для диагностики (можно удалить потом)
    private void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"State: {currentState}");
            GUI.Label(new Rect(10, 30, 300, 20), $"IsGrounded: {isGrounded}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Input: ({horizontalInput:F2}, {verticalInput:F2})");
            GUI.Label(new Rect(10, 70, 300, 20), $"Velocity: {rb.linearVelocity}");
        }
    }

    /// <summary>
    /// Получение ввода от игрока через новую Input System
    /// </summary>
    private void GetInput()
    {
        // Получаем ввод из Input System
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
            horizontalInput = moveInput.x;
            verticalInput = moveInput.y;
        }
        else
        {
            // Fallback на старую систему если Input Actions не настроены
            horizontalInput = 0f;
            verticalInput = 0f;
        }

        // Копание: пробел (Input System)
        digRequest = UnityEngine.InputSystem.Keyboard.current != null &&
                     UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    /// <summary>
    /// Проверка окружения (земля, лестницы, веревки)
    /// </summary>
    private void CheckEnvironment()
    {
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
            // Сохраняем центр лестницы по оси X
            ladderCenterX = ladderCollider.bounds.center.x;
        }
        else
        {
            isOnLadder = false;
            currentLadder = null;
        }

        // Проверка веревки
        Collider2D ropeCollider = Physics2D.OverlapBox(
            transform.position, 
            new Vector2(0.5f, 0.3f), 
            0f, 
            ropeLayer
        );
        
        if (ropeCollider != null)
        {
            isOnRope = true;
            currentRope = ropeCollider;
        }
        else
        {
            isOnRope = false;
            currentRope = null;
        }
    }

    /// <summary>
    /// Обработка состояния персонажа
    /// </summary>
    private void HandleState()
    {
        // Определение текущего состояния
        if (currentState == PlayerState.Digging)
        {
            // Во время копания не переключаемся
            return;
        }

        // Лазание по лестнице
        if (isOnLadder && Mathf.Abs(verticalInput) > 0.1f)
        {
            // Начинаем лазать когда нажата ↑/↓
            currentState = PlayerState.Climbing;
            rb.gravityScale = 0f; // Отключаем гравитацию на лестнице
        }
        else if (currentState == PlayerState.Climbing && isOnLadder)
        {
            // Продолжаем находиться на лестнице, даже если не нажимаем кнопки
            // Гравитация уже отключена, персонаж просто стоит на лестнице
            rb.gravityScale = 0f;
            
            // Если на земле и не нажимаем ↑/↓ - можем перейти в ходьбу
            if (isGrounded && Mathf.Abs(verticalInput) < 0.1f && Mathf.Abs(horizontalInput) > 0.1f)
            {
                currentState = PlayerState.Walking;
                rb.gravityScale = 3f;
            }
        }
        // Если просто находимся на лестнице (зашли сбоку) - не падаем!
        else if (isOnLadder && currentState != PlayerState.Climbing)
        {
            // Если на земле - переходим в ходьбу
            if (isGrounded)
            {
                currentState = PlayerState.Walking;
                rb.gravityScale = 3f;
            }
            else
            {
                // Если в воздухе на лестнице - останавливаемся
                rb.gravityScale = 0f;
                // Останавливаем вертикальное движение
                Vector2 vel = rb.linearVelocity;
                vel.y = 0f;
                rb.linearVelocity = vel;
            }
        }
        // Веревка
        else if (isOnRope && !isGrounded)
        {
            currentState = PlayerState.OnRope;
            rb.gravityScale = 0f; // Отключаем гравитацию на веревке
        }
        // Ходьба
        else if (isGrounded)
        {
            currentState = PlayerState.Walking;
            rb.gravityScale = 3f;
        }
        // Падение
        else
        {
            currentState = PlayerState.Falling;
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
            case PlayerState.Walking:
                // Горизонтальное движение
                velocity.x = horizontalInput * walkSpeed;
                // Вертикальная скорость сохраняется (гравитация)
                break;

            case PlayerState.Falling:
                // При падении - падаем вниз с выравниванием к ближайшей оси
                velocity.x = 0f;
                
                // Автоматическое выравнивание к ближайшей оси (0.5, 1.5, 2.5...)
                if (alignOnFalling)
                {
                    float currentX = transform.position.x;
                    // Вычисляем ближайшую точку N.5
                    float targetX = Mathf.Round(currentX - 0.5f) + 0.5f;
                    
                    // Плавно выравниваем к целевой позиции
                    if (Mathf.Abs(currentX - targetX) > 0.01f)
                    {
                        float newX = Mathf.MoveTowards(currentX, targetX, fallingAlignSpeed * Time.fixedDeltaTime);
                        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                    }
                }
                
                // Вертикальная скорость сохраняется (гравитация)
                break;

            case PlayerState.Climbing:
                // Движение по лестнице
                
                // Если нажата кнопка вверх или вниз - двигаемся и центрируемся
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    velocity.y = verticalInput * climbSpeed;
                    
                    // Автоматическое центрирование только при движении вверх/вниз
                    if (centerOnLadder && currentLadder != null)
                    {
                        float currentX = transform.position.x;
                        float targetX = ladderCenterX;
                        
                        // Плавно перемещаем игрока к центру лестницы
                        if (Mathf.Abs(currentX - targetX) > 0.01f)
                        {
                            float newX = Mathf.MoveTowards(currentX, targetX, ladderCenterSpeed * Time.fixedDeltaTime);
                            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                        }
                    }
                    
                    // Минимальное горизонтальное движение при лазании
                    velocity.x = horizontalInput * walkSpeed * 0.1f;
                }
                else
                {
                    // Если не нажимаем вверх/вниз - останавливаемся по вертикали
                    velocity.y = 0f;
                    
                    // Можно свободно двигаться влево-вправо для спрыгивания
                    velocity.x = horizontalInput * walkSpeed * 0.5f;
                }
                break;

            case PlayerState.OnRope:
                // Движение по веревке (только горизонтально)
                velocity.x = horizontalInput * ropeSpeed;
                velocity.y = 0f; // Фиксированная высота на веревке
                
                // Можно спрыгнуть вниз
                if (verticalInput < -0.1f)
                {
                    velocity.y = -2f;
                    currentState = PlayerState.Falling;
                    rb.gravityScale = 3f;
                }
                break;

            case PlayerState.Digging:
                // Во время копания не двигаемся
                velocity = Vector2.zero;
                break;
        }

        rb.linearVelocity = velocity;

        // Поворот персонажа
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
    /// Обработка копания ям
    /// </summary>
    private void HandleDigging()
    {
        // Проверка кулдауна
        if (Time.time < lastDigTime + digCooldown) return;

        // Можно копать только стоя на земле, но НЕ в выкопанной яме
        if (!isGrounded || IsInDugHole()) return;

        Vector2 digPosition = Vector2.zero;
        bool shouldDig = false;

        // Копание по направлению взгляда по пробелу
        if (digRequest)
        {
            float dir = isFacingRight ? 1f : -1f;
            digPosition = new Vector2(transform.position.x + dir * digRange, transform.position.y - 0.9f);
            shouldDig = true;
        }

        if (shouldDig)
        {
            DigHole(digPosition);
            lastDigTime = Time.time;
        }
    }

    /// <summary>
    /// Копает яму в указанной позиции
    /// </summary>
    private void DigHole(Vector2 position)
    {
        // Проверяем, есть ли копаемый блок
        Collider2D diggableBlock = Physics2D.OverlapCircle(position, 0.2f, diggableLayer);

        if (diggableBlock != null)
        {
            // Получаем компонент копаемого блока
            DiggableBlock block = diggableBlock.GetComponent<DiggableBlock>();
            if (block != null)
            {
                block.Dig(holeRestoreTime);
                
                // Запускаем анимацию копания
                if (animator != null)
                {
                    animator.SetTrigger(AnimDig);
                }

                Debug.Log($"Яма выкопана в позиции {position}");
            }
        }
    }

    /// <summary>
    /// Поворот персонажа
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    /// <summary>
    /// Обновление анимации
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat(AnimMoveX, Mathf.Abs(horizontalInput));
        animator.SetFloat(AnimMoveY, verticalInput);
        animator.SetBool(AnimIsGrounded, isGrounded);
        animator.SetBool(AnimIsClimbing, currentState == PlayerState.Climbing);
        animator.SetBool(AnimIsOnRope, currentState == PlayerState.OnRope);
    }

    /// <summary>
    /// Отрисовка Gizmos для отладки
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Проверка земли
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Зона копания слева
        Gizmos.color = Color.yellow;
        Vector3 digLeft = transform.position + new Vector3(-digRange, -0.9f, 0f);
        Gizmos.DrawWireSphere(digLeft, 0.3f);

        // Зона копания справа
        Vector3 digRight = transform.position + new Vector3(digRange, -0.9f, 0f);
        Gizmos.DrawWireSphere(digRight, 0.3f);

        // Зона проверки лестницы
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 1f, 0f));

        // Зона проверки веревки
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.3f, 0f));
    }

    /// <summary>
    /// Постоянная визуализация области детектирования diggableBlock
    /// </summary>
    private void OnDrawGizmos()
    {
        // Показываем активную зону детектирования копаемого блока
        float dir = isFacingRight ? 1f : -1f;
        Vector3 digPosition = transform.position + new Vector3(dir * digRange, -0.9f, 0f);
        
        // Проверяем, есть ли блок в этой позиции
        Collider2D diggableBlock = Physics2D.OverlapCircle(digPosition, 0.2f, diggableLayer);
        
        // Меняем цвет в зависимости от того, найден ли блок
        if (diggableBlock != null)
        {
            DiggableBlock block = diggableBlock.GetComponent<DiggableBlock>();
            if (block != null && !block.IsDug())
            {
                // Зеленый - можно копать
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                #if UNITY_EDITOR
                Handles.color = Color.green;
                #endif
            }
            else
            {
                // Красный - блок уже выкопан
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                #if UNITY_EDITOR
                Handles.color = Color.red;
                #endif
            }
        }
        else
        {
            // Серый - нет блока для копания
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            #if UNITY_EDITOR
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            #endif
        }
        
        // Рисуем круг области детектирования (радиус 0.2f)
        Gizmos.DrawWireSphere(digPosition, 0.2f);
        Gizmos.DrawSphere(digPosition, 0.2f);
        
        // Рисуем надпись с информацией
        #if UNITY_EDITOR
        string layerInfo = GetLayerMaskName(diggableLayer);
        Handles.Label(
            digPosition + Vector3.up * 0.5f, 
            $"Детект: r=0.2f\n{layerInfo}\nБлок: {(diggableBlock != null ? "Да" : "Нет")}"
        );
        #endif
    }

    // Публичные методы для внешнего управления
    public void SetCanMove(bool value) => canMove = value;
    public PlayerState GetState() => currentState;
    public bool IsGrounded() => isGrounded;
    public bool IsDead() => isDead;
    
    /// <summary>
    /// Получает читаемое имя слоев из LayerMask
    /// </summary>
    private string GetLayerMaskName(LayerMask layerMask)
    {
        if (layerMask.value == 0)
            return "Слой: Нет";
        
        string result = "Слой: ";
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask.value & (1 << i)) != 0)
            {
                if (result != "Слой: ")
                    result += ", ";
                result += LayerMask.LayerToName(i);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Смерть игрока (вызывается из DiggableBlock при схлопывании блока)
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
        
        // Визуальный эффект смерти (опционально - можно добавить анимацию)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 0.5f); // Красноватый полупрозрачный
        }
        
        Debug.Log("Игрок умер - раздавлен блоком!");
        
        // Запускаем возрождение
        StartCoroutine(RespawnAfterDelay());
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
    /// Возрождение игрока на точке возрождения
    /// </summary>
    private void Respawn()
    {
        isDead = false;
        
        // Восстанавливаем позицию
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            // Если нет точки возрождения - возрождаем на текущей позиции немного выше
            transform.position = new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z);
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
        currentState = PlayerState.Falling;
        
        Debug.Log("Игрок возродился");
    }
    
    /// <summary>
    /// Проверка: находится ли игрок в выкопанной яме
    /// </summary>
    private bool IsInDugHole()
    {
        // Проверяем область под ногами игрока на наличие выкопанных блоков
        Vector2 checkPos = new Vector2(transform.position.x, transform.position.y - 0.8f);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPos, 0.3f);
        
        foreach (Collider2D col in colliders)
        {
            DiggableBlock block = col.GetComponent<DiggableBlock>();
            if (block != null && block.IsDug())
            {
                return true;
            }
        }
        return false;
    }
}

