#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 618

[CustomEditor(typeof(IsoSpriteSorting))]
public class IsoSpriteSortingEditor : Editor {
    const float HANDLE_SIZE_FACTOR = 0.06f;

    public void OnSceneGUI() {
        var sorter = (IsoSpriteSorting)target;
        if (sorter.sorterPositionOffsets.Count == 0) return;

        switch (sorter.sortType) {
            case IsoSortType.Point:
                sorter.sorterPositionOffsets[0] = MoveHandleForIndex(sorter, 0);
                break;
            case IsoSortType.Line:
                DoLineHandles(sorter);
                break;
        }

        if (GUI.changed) {
            Undo.RecordObject(target, "Updated Sorting Offset");
            EditorUtility.SetDirty(target);
        }
    }

    protected void DoLineHandles(IsoSpriteSorting sorter) {
        for (var idx = 0; idx < sorter.sorterPositionOffsets.Count; idx++) {
            sorter.sorterPositionOffsets[idx] = MoveHandleForIndex(sorter, idx);
            if (idx > 0) {
                Handles.DrawLine(
                    sorter.transform.position + sorter.sorterPositionOffsets[idx - 1],
                    sorter.transform.position + sorter.sorterPositionOffsets[idx]
                );
            }
        }
    }

    protected Vector3 MoveHandleForIndex(IsoSpriteSorting sorter, int index) {
        return Handles.FreeMoveHandle(
            sorter.transform.position + sorter.sorterPositionOffsets[index],
            Quaternion.identity,
            HANDLE_SIZE_FACTOR * HandleUtility.GetHandleSize(sorter.transform.position),
            Vector3.zero,
            Handles.DotHandleCap
        ) - sorter.transform.position;
    }

    /// <summary>
    /// The sort manager is typically a singleton managed by Zenject,
    /// but in the editor we don't have that. Instead, we'll just make
    /// a temporary GameObject to hold the manager, perform the sorting,
    /// then delete it immediately after.
    /// </summary>
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var self = (IsoSpriteSorting)target;

        if (GUILayout.Button("Sort Visible Scene")) {
            if (Application.isPlaying) {
                Debug.LogWarning("Cannot sort while in play mode");
                return;
            }

            var temporaryContainer = new GameObject();
            var manager = temporaryContainer.AddComponent<IsoSpriteSortingManager>();
            SortScene(manager);
            DestroyImmediate(temporaryContainer);
        }
    }

    /// <summary>
    /// While in the editor, we can't see changes to sprite sort order
    /// automatically, but we can use this GUI helper function to update it.
    /// </summary>
    private void SortScene(IsoSpriteSortingManager manager) {
        var isoSorters = FindObjectsOfType<IsoSpriteSorting>();
        foreach (var sorter in isoSorters) {
            sorter.InjectManager(manager);
            sorter.Setup();
        }

        manager.UpdateSorting();

        foreach (var sorter in isoSorters) {
            sorter.Unregister();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif