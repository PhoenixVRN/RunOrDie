using UnityEngine;
using UnityEditor;

/// <summary>
/// Редактор для компонента LevelPropSnap с кнопкой выравнивания
/// </summary>
[CustomEditor(typeof(LevelPropSnap))]
public class LevelPropSnapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Отрисовываем стандартный инспектор
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        
        LevelPropSnap snapComponent = (LevelPropSnap)target;

        // Большая кнопка для выравнивания
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("⚡ ВЫРОВНЯТЬ ПО СЕТКЕ ⚡", GUILayout.Height(40)))
        {
            Undo.RecordObject(snapComponent.transform, "Snap To Grid");
            snapComponent.SnapToGrid();
            EditorUtility.SetDirty(snapComponent.transform);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Дополнительные кнопки для быстрого доступа
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("X: ...0.5", GUILayout.Height(25)))
        {
            Undo.RecordObject(snapComponent.transform, "Snap X");
            Vector3 pos = snapComponent.transform.position;
            pos.x = Mathf.Round(pos.x - 0.5f) + 0.5f;
            snapComponent.transform.position = pos;
            EditorUtility.SetDirty(snapComponent.transform);
        }
        
        if (GUILayout.Button("Y: ...0.5", GUILayout.Height(25)))
        {
            Undo.RecordObject(snapComponent.transform, "Snap Y");
            Vector3 pos = snapComponent.transform.position;
            pos.y = Mathf.Round(pos.y - 0.5f) + 0.5f;
            snapComponent.transform.position = pos;
            EditorUtility.SetDirty(snapComponent.transform);
        }
        
        if (GUILayout.Button("Z: 0.5", GUILayout.Height(25)))
        {
            Undo.RecordObject(snapComponent.transform, "Snap Z");
            Vector3 pos = snapComponent.transform.position;
            pos.z = Mathf.Round(pos.z / snapComponent.gridStepZ) * snapComponent.gridStepZ;
            snapComponent.transform.position = pos;
            EditorUtility.SetDirty(snapComponent.transform);
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        
        // Информация о текущей позиции
        EditorGUILayout.HelpBox(
            $"Текущая позиция:\n" +
            $"X: {snapComponent.transform.position.x:F3}\n" +
            $"Y: {snapComponent.transform.position.y:F3}\n" +
            $"Z: {snapComponent.transform.position.z:F3}",
            MessageType.Info
        );
    }
}

