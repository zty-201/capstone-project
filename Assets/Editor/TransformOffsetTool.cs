using UnityEditor;
using UnityEngine;

// Applies an exact, repeatable position offset to every currently-selected Transform —
// multi-object Inspector editing can't do this (typing into the X field sets every selected
// object to that same literal value instead of adding to each one's own position).
public class TransformOffsetTool : EditorWindow
{
    private Vector3 offset;

    [MenuItem("Tools/Offset Selected Transforms")]
    private static void Open() => GetWindow<TransformOffsetTool>("Offset Selected");

    private void OnGUI()
    {
        offset = EditorGUILayout.Vector3Field("Offset", offset);

        if (GUILayout.Button("Apply to Selected") && Selection.transforms.Length > 0)
        {
            foreach (Transform t in Selection.transforms)
            {
                Undo.RecordObject(t, "Offset Transform");
                t.position += offset;
            }
        }
    }
}
