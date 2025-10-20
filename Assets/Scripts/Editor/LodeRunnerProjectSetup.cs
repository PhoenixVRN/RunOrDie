using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Автоматическая настройка проекта для Lode Runner системы
/// Создает необходимые слои, теги и настройки физики
/// </summary>
public class LodeRunnerProjectSetup : EditorWindow
{
    private Vector2 scrollPosition;
    private bool setupComplete = false;
    private List<string> setupLog = new List<string>();

    [MenuItem("Tools/Lode Runner/Настройка проекта")]
    public static void ShowWindow()
    {
        var window = GetWindow<LodeRunnerProjectSetup>("Настройка Lode Runner");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Настройка проекта для Lode Runner", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически создаст:\n" +
            "• Слои (Layers): Ground, Ladder, Rope, Diggable\n" +
            "• Теги (Tags): Player, Ladder, Rope, Diggable\n" +
            "• Настройки коллизий между слоями",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 ВЫПОЛНИТЬ НАСТРОЙКУ", GUILayout.Height(40)))
        {
            SetupProject();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        if (setupComplete)
        {
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Открыть Tags & Layers", GUILayout.Height(30)))
            {
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }

            if (GUILayout.Button("Открыть Physics 2D Settings", GUILayout.Height(30)))
            {
                SettingsService.OpenProjectSettings("Project/Physics 2D");
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Лог настройки:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
        foreach (string log in setupLog)
        {
            if (log.StartsWith("✓"))
            {
                GUI.color = Color.green;
            }
            else if (log.StartsWith("⚠"))
            {
                GUI.color = Color.yellow;
            }
            else if (log.StartsWith("❌"))
            {
                GUI.color = Color.red;
            }
            else
            {
                GUI.color = Color.white;
            }

            EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
        }
        EditorGUILayout.EndScrollView();
    }

    private void SetupProject()
    {
        setupLog.Clear();
        setupLog.Add("=== НАЧАЛО НАСТРОЙКИ ===");

        // Создание слоев
        SetupLayers();

        // Создание тегов
        SetupTags();

        // Настройка коллизий
        SetupCollisionMatrix();

        setupLog.Add("=== НАСТРОЙКА ЗАВЕРШЕНА ===");
        setupComplete = true;

        Debug.Log("✓ Настройка проекта Lode Runner завершена!");
    }

    private void SetupLayers()
    {
        setupLog.Add("\n--- Настройка слоев ---");

        string[] layersToAdd = { "Ground", "Ladder", "Rope", "Diggable" };

        foreach (string layerName in layersToAdd)
        {
            if (CreateLayer(layerName))
            {
                setupLog.Add($"✓ Слой '{layerName}' создан");
            }
            else
            {
                setupLog.Add($"⚠ Слой '{layerName}' уже существует");
            }
        }
    }

    private bool CreateLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Проверяем, существует ли уже слой
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName)
            {
                return false; // Слой уже существует
            }
        }

        // Находим первый пустой слой
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return true;
            }
        }

        return false;
    }

    private void SetupTags()
    {
        setupLog.Add("\n--- Настройка тегов ---");

        string[] tagsToAdd = { "Player", "Ladder", "Rope", "Diggable", "Enemy" };

        foreach (string tagName in tagsToAdd)
        {
            if (CreateTag(tagName))
            {
                setupLog.Add($"✓ Тег '{tagName}' создан");
            }
            else
            {
                setupLog.Add($"⚠ Тег '{tagName}' уже существует");
            }
        }
    }

    private bool CreateTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        // Проверяем, существует ли уже тег
        for (int i = 0; i < tags.arraySize; i++)
        {
            SerializedProperty tag = tags.GetArrayElementAtIndex(i);
            if (tag.stringValue == tagName)
            {
                return false;
            }
        }

        // Добавляем новый тег
        tags.InsertArrayElementAtIndex(tags.arraySize);
        SerializedProperty newTag = tags.GetArrayElementAtIndex(tags.arraySize - 1);
        newTag.stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        return true;
    }

    private void SetupCollisionMatrix()
    {
        setupLog.Add("\n--- Настройка матрицы коллизий ---");

        int groundLayer = LayerMask.NameToLayer("Ground");
        int ladderLayer = LayerMask.NameToLayer("Ladder");
        int ropeLayer = LayerMask.NameToLayer("Rope");
        int diggableLayer = LayerMask.NameToLayer("Diggable");

        if (groundLayer == -1 || ladderLayer == -1 || ropeLayer == -1 || diggableLayer == -1)
        {
            setupLog.Add("❌ Не все слои созданы. Невозможно настроить коллизии.");
            return;
        }

        // Лестницы и веревки не должны сталкиваться друг с другом и с землей
        Physics2D.IgnoreLayerCollision(ladderLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(ropeLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(ladderLayer, ropeLayer, true);
        Physics2D.IgnoreLayerCollision(ladderLayer, diggableLayer, true);
        Physics2D.IgnoreLayerCollision(ropeLayer, diggableLayer, true);

        setupLog.Add("✓ Матрица коллизий настроена:");
        setupLog.Add("  • Ladder не сталкивается с Ground, Rope, Diggable");
        setupLog.Add("  • Rope не сталкивается с Ground, Ladder, Diggable");
    }

    [MenuItem("Tools/Lode Runner/Проверить настройки")]
    public static void CheckSetup()
    {
        bool hasIssues = false;
        string report = "=== ПРОВЕРКА НАСТРОЙКИ ПРОЕКТА ===\n\n";

        // Проверка слоев
        report += "СЛОИ:\n";
        string[] requiredLayers = { "Ground", "Ladder", "Rope", "Diggable" };
        foreach (string layerName in requiredLayers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                report += $"❌ Слой '{layerName}' не найден\n";
                hasIssues = true;
            }
            else
            {
                report += $"✓ Слой '{layerName}' найден (индекс {layer})\n";
            }
        }

        // Проверка тегов
        report += "\nТЕГИ:\n";
        string[] requiredTags = { "Player", "Ladder", "Rope", "Diggable" };
        foreach (string tagName in requiredTags)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tagName);
                report += $"✓ Тег '{tagName}' существует\n";
            }
            catch
            {
                report += $"❌ Тег '{tagName}' не найден\n";
                hasIssues = true;
            }
        }

        if (hasIssues)
        {
            report += "\n⚠ ОБНАРУЖЕНЫ ПРОБЛЕМЫ! Запустите 'Tools → Lode Runner → Настройка проекта'";
            Debug.LogWarning(report);
            EditorUtility.DisplayDialog("Проверка настройки", report, "OK");
        }
        else
        {
            report += "\n✓ ВСЕ НАСТРОЙКИ В ПОРЯДКЕ!";
            Debug.Log(report);
            EditorUtility.DisplayDialog("Проверка настройки", report, "OK");
        }
    }
}

