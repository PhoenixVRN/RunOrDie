using UnityEngine;
using UnityEditor;

/// <summary>
/// Быстрая настройка объектов Lode Runner через контекстное меню
/// </summary>
public class LodeRunnerQuickSetup
{
    // ==================== СОЗДАНИЕ ОБЪЕКТОВ ====================

    [MenuItem("GameObject/Lode Runner/Создать игрока", false, 0)]
    private static void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        
        // Добавляем компоненты
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 1f);
        
        var controller = player.AddComponent<PlayerController>();
        
        // Создаем GroundCheck
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
        
        // Назначаем GroundCheck в PlayerController
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty groundCheckProperty = serializedController.FindProperty("groundCheck");
        if (groundCheckProperty != null)
        {
            groundCheckProperty.objectReferenceValue = groundCheck.transform;
            serializedController.ApplyModifiedProperties();
        }
        
        // Пытаемся найти и назначить InputActionAsset
        string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            UnityEngine.InputSystem.InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
            if (inputAsset != null)
            {
                SerializedProperty inputActionsProperty = serializedController.FindProperty("inputActions");
                if (inputActionsProperty != null)
                {
                    inputActionsProperty.objectReferenceValue = inputAsset;
                    serializedController.ApplyModifiedProperties();
                    Debug.Log("✓ InputSystem_Actions автоматически назначен!");
                }
            }
        }
        
        // Добавляем временный спрайт
        var sr = player.AddComponent<SpriteRenderer>();
        sr.color = Color.green;
        sr.sprite = CreateTempSprite();
        
        Selection.activeGameObject = player;
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        
        Debug.Log("✓ Игрок создан! GroundCheck и InputActions назначены автоматически!");
    }

    [MenuItem("GameObject/Lode Runner/Создать лестницу", false, 1)]
    private static void CreateLadder()
    {
        GameObject ladder = new GameObject("Ladder");
        ladder.tag = "Ladder";
        ladder.layer = LayerMask.NameToLayer("Ladder");
        
        // Добавляем компоненты
        var col = ladder.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 3f);
        
        ladder.AddComponent<Ladder>();
        
        // Добавляем временный спрайт
        var sr = ladder.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.6f, 0.4f, 0.2f);
        sr.sprite = CreateTempSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(0.5f, 3f);
        
        Selection.activeGameObject = ladder;
        Undo.RegisterCreatedObjectUndo(ladder, "Create Ladder");
        
        Debug.Log("✓ Лестница создана!");
    }

    [MenuItem("GameObject/Lode Runner/Создать веревку", false, 2)]
    private static void CreateRope()
    {
        GameObject rope = new GameObject("Rope");
        rope.tag = "Rope";
        rope.layer = LayerMask.NameToLayer("Rope");
        
        // Добавляем компоненты
        var col = rope.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3f, 0.3f);
        
        rope.AddComponent<Rope>();
        
        // Добавляем временный спрайт
        var sr = rope.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.8f, 0.6f, 0.3f);
        sr.sprite = CreateTempSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(3f, 0.3f);
        
        Selection.activeGameObject = rope;
        Undo.RegisterCreatedObjectUndo(rope, "Create Rope");
        
        Debug.Log("✓ Веревка создана!");
    }

    [MenuItem("GameObject/Lode Runner/Создать копаемый блок", false, 3)]
    private static void CreateDiggableBlock()
    {
        GameObject block = new GameObject("Diggable Block");
        block.tag = "Diggable";
        block.layer = LayerMask.NameToLayer("Diggable");
        
        // Добавляем компоненты
        var col = block.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        
        block.AddComponent<DiggableBlock>();
        
        // Добавляем временный спрайт
        var sr = block.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.5f, 0.3f, 0.1f);
        sr.sprite = CreateTempSprite();
        
        Selection.activeGameObject = block;
        Undo.RegisterCreatedObjectUndo(block, "Create Diggable Block");
        
        Debug.Log("✓ Копаемый блок создан!");
    }

    [MenuItem("GameObject/Lode Runner/Создать землю", false, 4)]
    private static void CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.tag = "Ground";
        ground.layer = LayerMask.NameToLayer("Ground");
        
        // Добавляем компоненты
        var col = ground.AddComponent<BoxCollider2D>();
        col.size = new Vector2(5f, 1f);
        
        // Добавляем временный спрайт
        var sr = ground.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.5f, 0.3f);
        sr.sprite = CreateTempSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(5f, 1f);
        
        Selection.activeGameObject = ground;
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        
        Debug.Log("✓ Земля создана!");
    }

    // ==================== НАСТРОЙКА ВЫБРАННЫХ ====================

    [MenuItem("GameObject/Lode Runner/Настроить как игрока", true)]
    private static bool ValidateSetupAsPlayer()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Lode Runner/Настроить как игрока", false, 10)]
    private static void SetupAsPlayer()
    {
        GameObject obj = Selection.activeGameObject;
        Undo.RecordObject(obj, "Setup as Player");
        
        obj.tag = "Player";
        
        if (obj.GetComponent<Rigidbody2D>() == null)
        {
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        if (obj.GetComponent<CapsuleCollider2D>() == null)
        {
            var col = obj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.5f, 1f);
        }
        
        var controller = obj.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = obj.AddComponent<PlayerController>();
        }
        
        // Создаем GroundCheck если его нет
        Transform groundCheck = obj.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(obj.transform);
            gc.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = gc.transform;
        }
        
        // Назначаем GroundCheck в PlayerController
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty groundCheckProperty = serializedController.FindProperty("groundCheck");
        if (groundCheckProperty != null && groundCheckProperty.objectReferenceValue == null)
        {
            groundCheckProperty.objectReferenceValue = groundCheck;
            serializedController.ApplyModifiedProperties();
        }
        
        // Пытаемся найти и назначить InputActionAsset
        SerializedProperty inputActionsProperty = serializedController.FindProperty("inputActions");
        if (inputActionsProperty != null && inputActionsProperty.objectReferenceValue == null)
        {
            string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UnityEngine.InputSystem.InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                if (inputAsset != null)
                {
                    inputActionsProperty.objectReferenceValue = inputAsset;
                    serializedController.ApplyModifiedProperties();
                    Debug.Log("✓ InputSystem_Actions автоматически назначен!");
                }
            }
        }
        
        EditorUtility.SetDirty(obj);
        Debug.Log($"✓ '{obj.name}' настроен как игрок! GroundCheck и InputActions назначены.");
    }

    [MenuItem("GameObject/Lode Runner/Настроить как лестницу", true)]
    private static bool ValidateSetupAsLadder()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Lode Runner/Настроить как лестницу", false, 11)]
    private static void SetupAsLadder()
    {
        GameObject obj = Selection.activeGameObject;
        Undo.RecordObject(obj, "Setup as Ladder");
        
        obj.tag = "Ladder";
        obj.layer = LayerMask.NameToLayer("Ladder");
        
        var col = obj.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = obj.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        if (obj.GetComponent<Ladder>() == null)
        {
            obj.AddComponent<Ladder>();
        }
        
        EditorUtility.SetDirty(obj);
        Debug.Log($"✓ '{obj.name}' настроен как лестница!");
    }

    [MenuItem("GameObject/Lode Runner/Настроить как веревку", true)]
    private static bool ValidateSetupAsRope()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Lode Runner/Настроить как веревку", false, 12)]
    private static void SetupAsRope()
    {
        GameObject obj = Selection.activeGameObject;
        Undo.RecordObject(obj, "Setup as Rope");
        
        obj.tag = "Rope";
        obj.layer = LayerMask.NameToLayer("Rope");
        
        var col = obj.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = obj.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        if (obj.GetComponent<Rope>() == null)
        {
            obj.AddComponent<Rope>();
        }
        
        EditorUtility.SetDirty(obj);
        Debug.Log($"✓ '{obj.name}' настроен как веревка!");
    }

    [MenuItem("GameObject/Lode Runner/Настроить как копаемый блок", true)]
    private static bool ValidateSetupAsDiggable()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Lode Runner/Настроить как копаемый блок", false, 13)]
    private static void SetupAsDiggable()
    {
        GameObject obj = Selection.activeGameObject;
        Undo.RecordObject(obj, "Setup as Diggable");
        
        obj.tag = "Diggable";
        obj.layer = LayerMask.NameToLayer("Diggable");
        
        if (obj.GetComponent<BoxCollider2D>() == null)
        {
            obj.AddComponent<BoxCollider2D>();
        }
        
        if (obj.GetComponent<DiggableBlock>() == null)
        {
            obj.AddComponent<DiggableBlock>();
        }
        
        EditorUtility.SetDirty(obj);
        Debug.Log($"✓ '{obj.name}' настроен как копаемый блок!");
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

    private static Sprite CreateTempSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}

