using UnityEngine;
using UnityEditor;

/// <summary>
/// Добавляет пункты меню для быстрого выравнивания выбранных объектов
/// </summary>
public class LevelPropSnapMenuItem
{
    [MenuItem("Tools/Level Props/Выровнять выбранные объекты %#S")] // Ctrl+Shift+S
    private static void SnapSelectedObjects()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Не выбрано ни одного объекта!");
            return;
        }

        int snappedCount = 0;
        
        foreach (GameObject obj in Selection.gameObjects)
        {
            LevelPropSnap snapComponent = obj.GetComponent<LevelPropSnap>();
            
            if (snapComponent != null)
            {
                Undo.RecordObject(obj.transform, "Snap Multiple Objects To Grid");
                snapComponent.SnapToGrid();
                EditorUtility.SetDirty(obj.transform);
                snappedCount++;
            }
        }

        if (snappedCount > 0)
        {
            Debug.Log($"✓ Выровнено объектов: {snappedCount} из {Selection.gameObjects.Length}");
        }
        else
        {
            Debug.LogWarning("На выбранных объектах нет компонента LevelPropSnap!");
        }
    }

    [MenuItem("Tools/Level Props/Добавить LevelPropSnap к выбранным")]
    private static void AddSnapComponentToSelected()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Не выбрано ни одного объекта!");
            return;
        }

        int addedCount = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.GetComponent<LevelPropSnap>() == null)
            {
                Undo.AddComponent<LevelPropSnap>(obj);
                addedCount++;
            }
        }

        Debug.Log($"✓ Добавлен компонент LevelPropSnap к {addedCount} объектам");
    }

    [MenuItem("Tools/Level Props/Выровнять все пропсы на сцене")]
    private static void SnapAllPropsInScene()
    {
        LevelPropSnap[] allSnaps = Object.FindObjectsOfType<LevelPropSnap>();
        
        if (allSnaps.Length == 0)
        {
            Debug.LogWarning("На сцене нет объектов с компонентом LevelPropSnap!");
            return;
        }

        foreach (LevelPropSnap snap in allSnaps)
        {
            Undo.RecordObject(snap.transform, "Snap All Props To Grid");
            snap.SnapToGrid();
            EditorUtility.SetDirty(snap.transform);
        }

        Debug.Log($"✓ Выровнено всех пропсов на сцене: {allSnaps.Length}");
    }

    [MenuItem("GameObject/Level Props/Выровнять по сетке", false, 0)]
    private static void SnapFromGameObjectMenu()
    {
        SnapSelectedObjects();
    }
}

