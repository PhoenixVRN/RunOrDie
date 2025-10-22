using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper для настройки веревок в игре Lode Runner
/// </summary>
public class RopeSetupHelper : EditorWindow
{
    private const string ROPE_LAYER_NAME = "Rope";
    private int ropeLayerIndex = 8; // По умолчанию слой 8
    
    [MenuItem("Lode Runner/Настройка веревок (Rope Setup)")]
    public static void ShowWindow()
    {
        RopeSetupHelper window = GetWindow<RopeSetupHelper>("Настройка веревок");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("🎮 НАСТРОЙКА ВЕРЕВОК (ROPE LAYER)", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Шаг 1: Создание слоя
        DrawSectionHeader("Шаг 1: Создание слоя Rope");
        GUILayout.Label("Текущий статус слоя Rope:", EditorStyles.label);
        
        int currentRopeLayer = LayerMask.NameToLayer(ROPE_LAYER_NAME);
        if (currentRopeLayer == -1)
        {
            EditorGUILayout.HelpBox($"❌ Слой '{ROPE_LAYER_NAME}' НЕ СУЩЕСТВУЕТ!", MessageType.Error);
            GUILayout.Label($"Создайте слой вручную: Edit → Project Settings → Tags and Layers → Layer {ropeLayerIndex}", EditorStyles.wordWrappedMiniLabel);
            ropeLayerIndex = EditorGUILayout.IntSlider("Номер слоя:", ropeLayerIndex, 8, 31);
        }
        else
        {
            EditorGUILayout.HelpBox($"✅ Слой '{ROPE_LAYER_NAME}' существует (Layer {currentRopeLayer})", MessageType.Info);
            ropeLayerIndex = currentRopeLayer;
        }
        
        GUILayout.Space(10);
        
        // Шаг 2: Настройка префабов
        DrawSectionHeader("Шаг 2: Настройка префабов веревок");
        
        if (GUILayout.Button("🔧 Настроить все префабы Rope", GUILayout.Height(30)))
        {
            SetupAllRopePrefabs();
        }
        
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Эта кнопка найдет все префабы в Assets/GamePrefabs/Rope/ и настроит их:\n" +
                                "• Установит слой Rope\n" +
                                "• Добавит BoxCollider2D\n" +
                                "• Установит правильные размеры", MessageType.None);
        
        GUILayout.Space(10);
        
        // Шаг 3: Настройка PlayerController
        DrawSectionHeader("Шаг 3: Настройка PlayerController");
        
        if (GUILayout.Button("🎯 Найти и настроить Player в сцене", GUILayout.Height(30)))
        {
            SetupPlayerRopeLayer();
        }
        
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Эта кнопка найдет PlayerController в сцене и настроит:\n" +
                                "• Rope Layer в инспекторе\n" +
                                "• Параметры движения по веревке", MessageType.None);
        
        GUILayout.Space(10);
        
        // Шаг 4: Инструкция
        DrawSectionHeader("📖 Как работает система веревок");
        
        GUILayout.Label("1️⃣ В воздухе: Игрок автоматически зацепится за веревку", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2️⃣ На земле: Нажмите ↑ под веревкой, чтобы подпрыгнуть и схватиться", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3️⃣ На веревке: ← → для движения по веревке", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4️⃣ Спрыгнуть: Нажмите ↓ чтобы отпустить веревку", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        // Параметры PlayerController
        DrawSectionHeader("⚙️ Параметры в PlayerController");
        GUILayout.Label("• Rope Speed: Скорость движения по веревке (рекомендуется 4)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Center On Rope: Автоцентрирование на высоте веревки (вкл.)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Rope Center Speed: Скорость центрирования (рекомендуется 8)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Can Climb Rope: Можно ли лазать вверх по веревке (опционально)", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("💡 Совет: После настройки проверьте, что в PlayerController → Rope Layer выбран слой 'Rope'", MessageType.Info);
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }

    private void SetupAllRopePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GamePrefabs/Rope" });
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "Не найдено префабов в Assets/GamePrefabs/Rope/", "OK");
            return;
        }

        int ropeLayer = LayerMask.NameToLayer(ROPE_LAYER_NAME);
        if (ropeLayer == -1)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Слой '{ROPE_LAYER_NAME}' не существует!\nСоздайте его в Project Settings → Tags and Layers", "OK");
            return;
        }

        int processedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                // Открываем префаб для редактирования
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                
                // Устанавливаем слой
                SetLayerRecursively(prefabContents, ropeLayer);
                
                // Добавляем/настраиваем BoxCollider2D
                BoxCollider2D collider = prefabContents.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = prefabContents.AddComponent<BoxCollider2D>();
                }
                
                collider.isTrigger = true; // Веревка должна быть триггером
                
                // Настраиваем размер коллайдера (подгоняем под спрайт)
                SpriteRenderer sr = prefabContents.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    // Делаем коллайдер чуть больше спрайта для удобства
                    Bounds spriteBounds = sr.sprite.bounds;
                    collider.size = new Vector2(spriteBounds.size.x * 1.2f, spriteBounds.size.y * 0.5f);
                    collider.offset = new Vector2(0, 0);
                }
                
                // Сохраняем изменения
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                
                processedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Готово!", 
            $"Настроено префабов: {processedCount}\n\n" +
            $"• Установлен слой: {ROPE_LAYER_NAME} (Layer {ropeLayer})\n" +
            $"• Добавлены BoxCollider2D (trigger)\n" +
            $"• Настроены размеры коллайдеров", "OK");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void SetupPlayerRopeLayer()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        
        if (player == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "PlayerController не найден в сцене!", "OK");
            return;
        }

        int ropeLayer = LayerMask.NameToLayer(ROPE_LAYER_NAME);
        if (ropeLayer == -1)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Слой '{ROPE_LAYER_NAME}' не существует!", "OK");
            return;
        }

        // Используем SerializedObject для доступа к приватным полям
        SerializedObject serializedPlayer = new SerializedObject(player);
        
        // Устанавливаем ropeLayer
        SerializedProperty ropeLayerProp = serializedPlayer.FindProperty("ropeLayer");
        if (ropeLayerProp != null)
        {
            ropeLayerProp.intValue = 1 << ropeLayer; // LayerMask = битовая маска
        }
        
        // Устанавливаем рекомендуемые параметры
        SerializedProperty ropeSpeedProp = serializedPlayer.FindProperty("ropeSpeed");
        if (ropeSpeedProp != null && ropeSpeedProp.floatValue == 0f)
        {
            ropeSpeedProp.floatValue = 4f;
        }
        
        SerializedProperty centerOnRopeProp = serializedPlayer.FindProperty("centerOnRope");
        if (centerOnRopeProp != null)
        {
            centerOnRopeProp.boolValue = true;
        }
        
        SerializedProperty ropeCenterSpeedProp = serializedPlayer.FindProperty("ropeCenterSpeed");
        if (ropeCenterSpeedProp != null && ropeCenterSpeedProp.floatValue == 0f)
        {
            ropeCenterSpeedProp.floatValue = 8f;
        }
        
        serializedPlayer.ApplyModifiedProperties();
        
        // Выделяем объект для удобства
        Selection.activeGameObject = player.gameObject;
        EditorGUIUtility.PingObject(player.gameObject);
        
        EditorUtility.DisplayDialog("Готово!", 
            $"PlayerController настроен!\n\n" +
            $"• Rope Layer: {ROPE_LAYER_NAME}\n" +
            $"• Rope Speed: {ropeSpeedProp.floatValue}\n" +
            $"• Center On Rope: {centerOnRopeProp.boolValue}\n" +
            $"• Rope Center Speed: {ropeCenterSpeedProp.floatValue}\n\n" +
            $"Объект выделен в Hierarchy для проверки.", "OK");
    }
}

