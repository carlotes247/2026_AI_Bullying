// Assets/Scripts/GridGenerator.cs
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Attach this to an empty GameObject and press "Generate Grid" in its inspector.
/// </summary>
[DisallowMultipleComponent]
public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Number of rows (along the X axis).")]
    public int rows = 5;

    [Tooltip("Number of columns (along the Z axis).")]
    public int columns = 5;

    [Tooltip("Distance between adjacent grid cells. Default is 100 units.")]
    public float spacing = 100f;

    [Header("Prefab to Instantiate")]
    [Tooltip("The prefab that will be placed in each cell.")]
    public GameObject prefab;

    /// <summary>
    /// Called from the custom inspector button.
    /// </summary>
    public void GenerateGrid()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[GridGenerator] Prefab is not assigned.");
            return;
        }

        // Optional: clear any old children first
        CleanGrid();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Vector3 position = new Vector3(
                    i * spacing,
                    0f,                           // keep all at the same Y
                    j * spacing);

#if UNITY_EDITOR
                // Instantiates a prefab in edit mode.
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance != null)
                {
                    // Parent to this generator so the hierarchy stays tidy
                    instance.transform.SetParent(transform);
                    // Convert local position into world space relative to this object
                    instance.transform.position = transform.TransformPoint(position);
                }
#endif
            }
        }
    }

    public void CleanGrid()
    {
        // Optional: clear any old children first
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }
}
