using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper для настройки системы сбора предметов (золото, ключи)
/// </summary>
public class CollectiblesSetupHelper : EditorWindow
{
    [MenuItem("Lode Runner/Настройка системы сбора предметов")]
    public static void ShowWindow()
    {
        CollectiblesSetupHelper window = GetWindow<CollectiblesSetupHelper>("Collectibles Setup");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("🏆 НАСТРОЙКА СИСТЕМЫ СБОРА ПРЕДМЕТОВ", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Шаг 1: GameManager
        DrawSectionHeader("Шаг 1: GameManager");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        
        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("❌ GameManager не найден в сцене!", MessageType.Error);
            
            if (GUILayout.Button("➕ Создать GameManager", GUILayout.Height(30)))
            {
                CreateGameManager();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("✅ GameManager найден в сцене", MessageType.Info);
            
            if (GUILayout.Button("🔍 Выделить GameManager", GUILayout.Height(25)))
            {
                Selection.activeGameObject = gameManager.gameObject;
                EditorGUIUtility.PingObject(gameManager.gameObject);
            }
        }
        
        GUILayout.Space(10);
        
        // Шаг 2: Создание слоев
        DrawSectionHeader("Шаг 2: Создание слоев (опционально)");
        
        EditorGUILayout.HelpBox("Рекомендуется создать слои:\n" +
                                "• Collectible (Layer 9) - для золота и ключей\n" +
                                "• Exit (Layer 10) - для выхода из уровня", MessageType.None);
        
        GUILayout.Label("Создайте вручную: Edit → Project Settings → Tags and Layers", EditorStyles.wordWrappedMiniLabel);
        
        GUILayout.Space(10);
        
        // Шаг 3: Создание префабов
        DrawSectionHeader("Шаг 3: Создание префабов");
        
        if (GUILayout.Button("🪙 Создать префаб Gold (Золото)", GUILayout.Height(30)))
        {
            CreateGoldPrefab();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔑 Создать префаб Key (Ключ)", GUILayout.Height(30)))
        {
            CreateKeyPrefab();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🚪 Создать префаб Exit (Выход)", GUILayout.Height(30)))
        {
            CreateExitPrefab();
        }
        
        GUILayout.Space(10);
        
        // Шаг 4: Инструкции
        DrawSectionHeader("📖 Инструкция по использованию");
        
        GUILayout.Label("1. Создайте GameManager (если нет)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. Перетащите Gold префабы в уровень", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3. Добавьте Key (опционально, 1 на уровень)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4. Разместите Exit там, где должен быть выход", EditorStyles.wordWrappedLabel);
        GUILayout.Label("5. Запустите игру и соберите все предметы!", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        DrawSectionHeader("⚙️ Настройки GameManager");
        
        GUILayout.Label("• Require All Gold - нужно ли собрать ВСЁ золото", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Require Key - нужен ли ключ для выхода", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        DrawSectionHeader("🎨 Визуализация");
        
        GUILayout.Label("Gizmos в Scene View:", EditorStyles.boldLabel);
        GUILayout.Label("🟡 Желтый круг - Золото", EditorStyles.wordWrappedLabel);
        GUILayout.Label("🔵 Голубой круг - Ключ", EditorStyles.wordWrappedLabel);
        GUILayout.Label("🟢 Зеленый куб - Выход", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("💡 Совет: После создания префабов настройте их спрайты в Inspector", MessageType.Info);
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }

    private void CreateGameManager()
    {
        // Создаем GameObject
        GameObject gmObject = new GameObject("GameManager");
        
        // Добавляем компонент
        GameManager gm = gmObject.AddComponent<GameManager>();
        
        // Выделяем в иерархии
        Selection.activeGameObject = gmObject;
        EditorGUIUtility.PingObject(gmObject);
        
        EditorUtility.DisplayDialog("Готово!", "GameManager создан и добавлен в сцену!", "OK");
    }

    private void CreateGoldPrefab()
    {
        CreateCollectiblePrefab("Gold", Collectible.CollectibleType.Gold, 100);
    }

    private void CreateKeyPrefab()
    {
        CreateCollectiblePrefab("Key", Collectible.CollectibleType.Key, 0);
    }

    private void CreateCollectiblePrefab(string name, Collectible.CollectibleType type, int scoreValue)
    {
        // Создаем GameObject
        GameObject prefabObject = new GameObject(name);
        
        // Добавляем SpriteRenderer
        SpriteRenderer sr = prefabObject.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = type == Collectible.CollectibleType.Gold ? Color.yellow : Color.cyan;
        
        // Добавляем коллайдер
        CircleCollider2D collider = prefabObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;
        
        // Добавляем компонент Collectible
        Collectible collectible = prefabObject.AddComponent<Collectible>();
        
        // Устанавливаем параметры через SerializedObject
        SerializedObject so = new SerializedObject(collectible);
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("scoreValue").intValue = scoreValue;
        so.FindProperty("enablePulseAnimation").boolValue = true;
        so.FindProperty("pulseSpeed").floatValue = 2f;
        so.FindProperty("pulseScale").floatValue = 1.2f;
        so.ApplyModifiedProperties();
        
        // Создаем папку для префабов если нет
        string folderPath = "Assets/GamePrefabs/Collectibles";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/GamePrefabs", "Collectibles");
        }
        
        // Сохраняем как префаб
        string prefabPath = $"{folderPath}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
        
        // Удаляем из сцены
        DestroyImmediate(prefabObject);
        
        // Выделяем префаб в Project
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        EditorUtility.DisplayDialog("Готово!", 
            $"Префаб {name} создан!\n" +
            $"Путь: {prefabPath}\n\n" +
            $"Замените спрайт на свой в Inspector.", "OK");
    }

    private void CreateExitPrefab()
    {
        // Создаем GameObject
        GameObject exitObject = new GameObject("LevelExit");
        
        // Добавляем SpriteRenderer
        SpriteRenderer sr = exitObject.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        sr.color = Color.green;
        
        // Добавляем коллайдер
        BoxCollider2D collider = exitObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);
        
        // Добавляем компонент LevelExit
        LevelExit exit = exitObject.AddComponent<LevelExit>();
        
        // Создаем папку для префабов если нет
        string folderPath = "Assets/GamePrefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "GamePrefabs");
        }
        
        // Сохраняем как префаб
        string prefabPath = $"{folderPath}/LevelExit.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(exitObject, prefabPath);
        
        // Удаляем из сцены
        DestroyImmediate(exitObject);
        
        // Выделяем префаб в Project
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        EditorUtility.DisplayDialog("Готово!", 
            $"Префаб LevelExit создан!\n" +
            $"Путь: {prefabPath}\n\n" +
            $"Замените спрайт на свой в Inspector.", "OK");
    }
}

