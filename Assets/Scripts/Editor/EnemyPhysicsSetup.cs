using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматическая настройка физики для врагов
/// Отключает коллизии между врагами (Layer 10 x Layer 10)
/// </summary>
public class EnemyPhysicsSetup : EditorWindow
{
    [MenuItem("Lode Runner/Настройка физики врагов")]
    public static void ShowWindow()
    {
        GetWindow<EnemyPhysicsSetup>("Физика врагов");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Настройка физики врагов", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Этот инструмент отключит физические коллизии между врагами.\n\n" +
            "Враги будут проходить друг через друга, но система разделения " +
            "не даст им визуально слипаться.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Проверяем текущее состояние
        int enemyLayer = 10; // Layer врагов
        bool collisionEnabled = !Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer);

        if (collisionEnabled)
        {
            EditorGUILayout.HelpBox(
                "⚠️ ВНИМАНИЕ: Коллизии между врагами ВКЛЮЧЕНЫ!\n" +
                "Враги будут упираться друг в друга при встрече.",
                MessageType.Warning
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "✅ Коллизии между врагами ОТКЛЮЧЕНЫ.\n" +
                "Враги проходят друг через друга свободно.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space();

        // Кнопка для отключения коллизий
        if (GUILayout.Button("Отключить коллизии между врагами", GUILayout.Height(40)))
        {
            DisableEnemyCollisions();
        }

        EditorGUILayout.Space();

        // Кнопка для включения обратно (на случай если нужно)
        if (GUILayout.Button("Включить коллизии между врагами", GUILayout.Height(30)))
        {
            EnableEnemyCollisions();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Информация:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Layer врагов: {enemyLayer} ({LayerMask.LayerToName(enemyLayer)})");
        EditorGUILayout.LabelField($"Коллизии: {(collisionEnabled ? "ВКЛЮЧЕНЫ ⚠️" : "ОТКЛЮЧЕНЫ ✅")}");
    }

    private void DisableEnemyCollisions()
    {
        int enemyLayer = 10;
        
        // Отключаем коллизии Layer 10 x Layer 10
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        
        Debug.Log("✅ Коллизии между врагами (Layer 10 x Layer 10) ОТКЛЮЧЕНЫ!");
        Debug.Log("Враги теперь будут проходить друг через друга.");
        
        // Перерисовываем окно
        Repaint();
    }

    private void EnableEnemyCollisions()
    {
        int enemyLayer = 10;
        
        // Включаем коллизии Layer 10 x Layer 10
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, false);
        
        Debug.Log("⚠️ Коллизии между врагами (Layer 10 x Layer 10) ВКЛЮЧЕНЫ!");
        Debug.Log("Враги будут упираться друг в друга.");
        
        // Перерисовываем окно
        Repaint();
    }
}

