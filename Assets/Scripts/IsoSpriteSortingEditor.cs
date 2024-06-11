#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#pragma warning disable 618

[CustomEditor(typeof(IsoSpriteSorting))]
public class IsoSpriteSortingEditor : Editor {
    SerializedProperty sorterPositionOffset;
    SerializedProperty sorterPositionOffset2;

    private void OnEnable() {
        sorterPositionOffset = serializedObject.FindProperty("SorterPositionOffset");
        sorterPositionOffset2 = serializedObject.FindProperty("SorterPositionOffset2");
    }

    public void OnSceneGUI() {
        IsoSpriteSorting sorter = (IsoSpriteSorting)target;

        serializedObject.Update();
        sorterPositionOffset.vector2Value = Handles.FreeMoveHandle(
            sorter.transform.position + (Vector3)sorterPositionOffset.vector2Value,
            Quaternion.identity,
            0.08f * HandleUtility.GetHandleSize(sorter.transform.position),
            Vector3.zero,
            Handles.DotHandleCap
        ) - sorter.transform.position;
        if (sorter.sortType == IsoSpriteSorting.SortType.Line) {
            sorterPositionOffset2.vector2Value = Handles.FreeMoveHandle(
                sorter.transform.position + (Vector3)sorterPositionOffset2.vector2Value,
                Quaternion.identity,
                0.08f * HandleUtility.GetHandleSize(sorter.transform.position),
                Vector3.zero,
                Handles.DotHandleCap
            ) - sorter.transform.position;
            Vector2 pos = sorter.transform.position;
            Handles.DrawLine(pos + sorterPositionOffset.vector2Value, pos + sorterPositionOffset2.vector2Value);
        }
        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        IsoSpriteSorting myScript = (IsoSpriteSorting)target;
        if (GUILayout.Button("Sort Visible Scene")) {
            myScript.SortScene();
        }
    }
}
#endif
