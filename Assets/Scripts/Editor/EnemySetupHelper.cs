using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper для настройки системы врагов
/// </summary>
public class EnemySetupHelper : EditorWindow
{
    [MenuItem("Lode Runner/Настройка врагов (Enemy Setup)")]
    public static void ShowWindow()
    {
        EnemySetupHelper window = GetWindow<EnemySetupHelper>("Enemy Setup");
        window.minSize = new Vector2(450, 650);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("🤖 НАСТРОЙКА СИСТЕМЫ ВРАГОВ", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Шаг 1: Создание префаба
        DrawSectionHeader("Шаг 1: Создание префаба врага");
        
        EditorGUILayout.HelpBox("Создайте базовый префаб врага с необходимыми компонентами", MessageType.Info);
        
        if (GUILayout.Button("🤖 Создать префаб Enemy", GUILayout.Height(30)))
        {
            CreateEnemyPrefab();
        }
        
        GUILayout.Space(10);
        
        // Шаг 2: Создание спавнера
        DrawSectionHeader("Шаг 2: Создание спавнера врагов");
        
        if (GUILayout.Button("📍 Создать EnemySpawner в сцене", GUILayout.Height(30)))
        {
            CreateEnemySpawner();
        }
        
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("EnemySpawner создает врагов в указанных точках при старте уровня", MessageType.None);
        
        GUILayout.Space(10);
        
        // Шаг 3: Инструкции
        DrawSectionHeader("📖 Инструкция по настройке");
        
        GUILayout.Label("1️⃣ Создайте префаб Enemy", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2️⃣ Замените спрайт на спрайт врага", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3️⃣ Создайте EnemySpawner в сцене", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4️⃣ Назначьте Enemy Prefab в EnemySpawner", EditorStyles.wordWrappedLabel);
        GUILayout.Label("5️⃣ Создайте точки спавна (Spawn Points)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("6️⃣ Запустите игру!", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        DrawSectionHeader("⚙️ Параметры EnemyController");
        
        GUILayout.Label("• Walk Speed (3) - скорость ходьбы", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Climb Speed (2) - скорость лазания", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Detection Range (15) - дальность обнаружения", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Update Path Interval (0.5) - частота обновления пути", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Stuck In Hole Time (3) - время в яме", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        DrawSectionHeader("🎯 Как работают враги");
        
        GUILayout.Label("ПРЕСЛЕДОВАНИЕ:", EditorStyles.boldLabel);
        GUILayout.Label("• Враги ищут игрока в радиусе Detection Range", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Идут к игроку по кратчайшему пути", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Могут лазать по лестницам", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(5);
        
        GUILayout.Label("ВЗАИМОДЕЙСТВИЕ:", EditorStyles.boldLabel);
        GUILayout.Label("• При касании игрока → игрок умирает", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Если упали в яму → застревают на 3 секунды", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• После выхода из ямы → возрождаются", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        DrawSectionHeader("🎨 Визуализация в Scene View");
        
        GUILayout.Label("Выделите Enemy:", EditorStyles.boldLabel);
        GUILayout.Label("🔴 Красная сфера - зона обнаружения", EditorStyles.wordWrappedLabel);
        GUILayout.Label("🟢 Зеленый круг - groundCheck (на земле)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("🟡 Желтая линия - путь к игроку", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("💡 Совет: Разместите точки спавна врагов в разных частях уровня для баланса сложности", MessageType.Info);
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }

    private void CreateEnemyPrefab()
    {
        // Создаем GameObject
        GameObject enemyObject = new GameObject("Enemy");
        
        // Добавляем SpriteRenderer
        SpriteRenderer sr = enemyObject.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = Color.red;
        
        // Добавляем Rigidbody2D
        Rigidbody2D rb = enemyObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Добавляем CapsuleCollider2D
        CapsuleCollider2D collider = enemyObject.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.5f, 1f);
        collider.direction = CapsuleDirection2D.Vertical;
        
        // Создаем GroundCheck
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.parent = enemyObject.transform;
        groundCheckObj.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        
        // Добавляем EnemyController
        EnemyController enemy = enemyObject.AddComponent<EnemyController>();
        
        // Устанавливаем параметры через SerializedObject
        SerializedObject so = new SerializedObject(enemy);
        
        so.FindProperty("walkSpeed").floatValue = 3f;
        so.FindProperty("climbSpeed").floatValue = 2f;
        so.FindProperty("groundCheck").objectReferenceValue = groundCheckObj.transform;
        so.FindProperty("groundCheckRadius").floatValue = 0.2f;
        so.FindProperty("detectionRange").floatValue = 15f;
        so.FindProperty("updatePathInterval").floatValue = 0.5f;
        so.FindProperty("stuckInHoleTime").floatValue = 3f;
        so.FindProperty("respawnDelay").floatValue = 2f;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        
        // Устанавливаем слои (нужно сделать через код позже)
        so.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground", "GroundDig");
        so.FindProperty("ladderLayer").intValue = LayerMask.GetMask("Ladder");
        so.FindProperty("playerLayer").intValue = LayerMask.GetMask("Default");
        
        so.ApplyModifiedProperties();
        
        // Создаем папку для префабов если нет
        string folderPath = "Assets/GamePrefabs/Enemies";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/GamePrefabs", "Enemies");
        }
        
        // Сохраняем как префаб
        string prefabPath = $"{folderPath}/Enemy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, prefabPath);
        
        // Удаляем из сцены
        DestroyImmediate(enemyObject);
        
        // Выделяем префаб в Project
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        EditorUtility.DisplayDialog("Готово!", 
            $"Префаб Enemy создан!\n" +
            $"Путь: {prefabPath}\n\n" +
            $"Настройте:\n" +
            $"1. Замените спрайт на спрайт врага\n" +
            $"2. Настройте параметры в Inspector\n" +
            $"3. Используйте в EnemySpawner", "OK");
    }

    private void CreateEnemySpawner()
    {
        // Создаем GameObject
        GameObject spawnerObject = new GameObject("EnemySpawner");
        
        // Добавляем компонент
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        
        // Создаем несколько точек спавна
        Transform[] spawnPoints = new Transform[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
            spawnPoint.transform.parent = spawnerObject.transform;
            spawnPoint.transform.position = new Vector3(i * 5f, 0f, 0f);
            spawnPoints[i] = spawnPoint.transform;
        }
        
        // Устанавливаем параметры
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty spawnPointsProp = so.FindProperty("spawnPoints");
        spawnPointsProp.arraySize = 3;
        
        for (int i = 0; i < 3; i++)
        {
            spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
        }
        
        so.FindProperty("spawnOnStart").boolValue = true;
        so.FindProperty("maxEnemies").intValue = 5;
        so.FindProperty("enemyWalkSpeed").floatValue = 3f;
        so.FindProperty("enemyClimbSpeed").floatValue = 2f;
        so.FindProperty("detectionRange").floatValue = 15f;
        
        so.ApplyModifiedProperties();
        
        // Выделяем в иерархии
        Selection.activeGameObject = spawnerObject;
        EditorGUIUtility.PingObject(spawnerObject);
        
        EditorUtility.DisplayDialog("Готово!", 
            "EnemySpawner создан в сцене!\n\n" +
            "Настройте:\n" +
            "1. Назначьте Enemy Prefab\n" +
            "2. Разместите точки спавна в нужных местах\n" +
            "3. Настройте параметры врагов\n\n" +
            "Объект выделен в Hierarchy.", "OK");
    }
}

