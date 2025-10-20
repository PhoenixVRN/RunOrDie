using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматическое исправление проблем с PlayerController
/// </summary>
public class PlayerAutoFix : EditorWindow
{
    [MenuItem("Tools/Lode Runner/🔧 Исправить игрока")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerAutoFix>("Исправление игрока");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    [MenuItem("Tools/Lode Runner/🔧 Быстрое исправление Player", false, 1)]
    public static void QuickFix()
    {
        // Найти всех игроков на сцене
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        
        if (players.Length == 0)
        {
            Debug.LogWarning("На сцене нет объектов с PlayerController!");
            EditorUtility.DisplayDialog("Игрок не найден", 
                "На сцене нет объектов с компонентом PlayerController.\n\n" +
                "Создайте игрока через:\n" +
                "GameObject → Lode Runner → Создать игрока", "OK");
            return;
        }

        int fixedCount = 0;
        
        foreach (var player in players)
        {
            if (FixPlayer(player))
            {
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"✓ Исправлено игроков: {fixedCount}");
            EditorUtility.DisplayDialog("Исправление завершено", 
                $"✓ Исправлено игроков: {fixedCount}\n\n" +
                $"GroundCheck создан и назначен\n" +
                $"InputActions назначен (если найден)", "OK");
        }
        else
        {
            Debug.Log("Все игроки уже настроены правильно!");
            EditorUtility.DisplayDialog("Всё в порядке", 
                "Все игроки уже настроены правильно!\n\n" +
                "GroundCheck и InputActions назначены.", "OK");
        }
    }

    private static bool FixPlayer(PlayerController player)
    {
        bool wasFixed = false;
        GameObject playerObj = player.gameObject;
        
        SerializedObject serializedPlayer = new SerializedObject(player);
        
        // 1. Проверка и создание GroundCheck
        SerializedProperty groundCheckProp = serializedPlayer.FindProperty("groundCheck");
        if (groundCheckProp != null && groundCheckProp.objectReferenceValue == null)
        {
            // Попытка найти существующий GroundCheck
            Transform existingGroundCheck = playerObj.transform.Find("GroundCheck");
            
            if (existingGroundCheck == null)
            {
                // Создаем новый GroundCheck
                GameObject groundCheck = new GameObject("GroundCheck");
                groundCheck.transform.SetParent(playerObj.transform);
                groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
                existingGroundCheck = groundCheck.transform;
                
                Undo.RegisterCreatedObjectUndo(groundCheck, "Create GroundCheck");
                Debug.Log($"✓ Создан GroundCheck для '{playerObj.name}'");
            }
            
            // Назначаем GroundCheck
            groundCheckProp.objectReferenceValue = existingGroundCheck;
            wasFixed = true;
            Debug.Log($"✓ GroundCheck назначен для '{playerObj.name}'");
        }
        
        // 2. Проверка и назначение InputActions
        SerializedProperty inputActionsProp = serializedPlayer.FindProperty("inputActions");
        if (inputActionsProp != null && inputActionsProp.objectReferenceValue == null)
        {
            // Ищем InputActionAsset в проекте
            string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                
                if (inputAsset != null)
                {
                    inputActionsProp.objectReferenceValue = inputAsset;
                    wasFixed = true;
                    Debug.Log($"✓ InputActions назначен для '{playerObj.name}'");
                }
            }
        }
        
        if (wasFixed)
        {
            serializedPlayer.ApplyModifiedProperties();
            EditorUtility.SetDirty(player);
        }
        
        return wasFixed;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Автоматическое исправление игрока", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически исправит:\n\n" +
            "• Создаст GroundCheck объект\n" +
            "• Назначит GroundCheck в PlayerController\n" +
            "• Назначит InputActions (если найден)\n\n" +
            "Работает со всеми игроками на сцене.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🔧 ИСПРАВИТЬ ВСЕ ПРОБЛЕМЫ", GUILayout.Height(40)))
        {
            QuickFix();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(20);
        
        EditorGUILayout.LabelField("Альтернативные действия:", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Создать нового игрока", GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("GameObject/Lode Runner/Создать игрока");
            Close();
        }

        if (GUILayout.Button("Настроить выбранный объект как игрока", GUILayout.Height(30)))
        {
            if (Selection.activeGameObject != null)
            {
                EditorApplication.ExecuteMenuItem("GameObject/Lode Runner/Настроить как игрока");
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Объект не выбран", 
                    "Выберите объект в Hierarchy перед использованием этой функции.", "OK");
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Закрыть"))
        {
            Close();
        }
    }
}

