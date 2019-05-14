#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#pragma warning disable 618

[CustomEditor(typeof(IsoSpriteSorting))]
public class IsoSpriteSortingEditor : Editor
{
    public void OnSceneGUI()
    {
        IsoSpriteSorting myTarget = (IsoSpriteSorting)target;

        myTarget.SorterPositionOffset = Handles.FreeMoveHandle(myTarget.transform.position + myTarget.SorterPositionOffset, Quaternion.identity, 0.08f * HandleUtility.GetHandleSize(myTarget.transform.position), Vector3.zero, Handles.DotCap) - myTarget.transform.position;
        if (myTarget.sortType == IsoSpriteSorting.SortType.Line)
        {
            myTarget.SorterPositionOffset2 = Handles.FreeMoveHandle(myTarget.transform.position + myTarget.SorterPositionOffset2, Quaternion.identity, 0.08f * HandleUtility.GetHandleSize(myTarget.transform.position), Vector3.zero, Handles.DotCap) - myTarget.transform.position;
            Handles.DrawLine(myTarget.transform.position + myTarget.SorterPositionOffset, myTarget.transform.position + myTarget.SorterPositionOffset2);

        }
        if (GUI.changed)
        {
            Undo.RecordObject(target, "Updated Sorting Offset");
            EditorUtility.SetDirty(target);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        IsoSpriteSorting myScript = (IsoSpriteSorting)target;
        if (GUILayout.Button("Sort Visible Scene"))
        {
            myScript.SortScene();
        }
    }
}
#endif
