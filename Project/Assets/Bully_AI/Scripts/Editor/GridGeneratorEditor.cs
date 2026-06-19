// Assets/Editor/GridGeneratorEditor.cs
using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds a "Generate Grid" button to the inspector of GridGenerator.
/// The button works while you’re still in Edit mode (no Play required).
/// </summary>
[CustomEditor(typeof(GridGenerator))]
public sealed class GridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw all normal fields first.
        DrawDefaultInspector();

        // Retrieve the component instance.
        GridGenerator generator = target as GridGenerator;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Grid"))
        {
            // Record undo for all created objects.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            generator.GenerateGrid();

            Undo.CollapseUndoOperations(group);
            //Debug.Log("Grid Generated!");
        }
        if (GUILayout.Button("Clean Grid"))
        {
            // Record undo for all created objects.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            generator.CleanGrid();

            Undo.CollapseUndoOperations(group);
        }
    }
}
