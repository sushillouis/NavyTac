#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainBoundaryCreator))]
public class TerrainBoundaryCreatorEditor : Editor
{
    private SerializedProperty targetTerrainProp;
    private SerializedProperty spacingProp;
    private SerializedProperty heightToleranceProp;
    private SerializedProperty clearExistingChildrenProp;

    private bool showPreview;
    private Vector2 previewScroll;

    private void OnEnable()
    {
        targetTerrainProp = serializedObject.FindProperty("targetTerrain");
        spacingProp = serializedObject.FindProperty("spacing");
        heightToleranceProp = serializedObject.FindProperty("heightTolerance");
        clearExistingChildrenProp = serializedObject.FindProperty("clearExistingChildren");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(targetTerrainProp);
        EditorGUILayout.PropertyField(spacingProp);
        EditorGUILayout.PropertyField(heightToleranceProp);
        EditorGUILayout.PropertyField(clearExistingChildrenProp);

        var comp = (TerrainBoundaryCreator)target;
        int count = comp.BoundaryPositions != null ? comp.BoundaryPositions.Count : 0;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Boundary positions", count.ToString());

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create (Selected Terrain)"))
            {
                comp.CreateBoundary();
            }
            if (GUILayout.Button("Create (All Terrains)"))
            {
                comp.CreateBoundaryForAllTerrains();
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Data"))
            {
                comp.ClearBoundaryData();
            }
            if (GUILayout.Button("Make Y Zero"))
            {
                var so = new SerializedObject(comp);
                so.Update();
                comp.SendMessage("MakeYZero", SendMessageOptions.DontRequireReceiver);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sort by Nearby"))
            {
                comp.SendMessage("SortByNearbyPoint", SendMessageOptions.DontRequireReceiver);
                EditorUtility.SetDirty(comp);
            }
            if (GUILayout.Button("Insert 5 Between Pairs"))
            {
                comp.SendMessage("CreateFivePointsBetweenEachPair", SendMessageOptions.DontRequireReceiver);
                EditorUtility.SetDirty(comp);
            }
        }

        EditorGUILayout.Space();
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview first 50 points (read-only)");
        if (showPreview)
        {
            const int maxPreview = 50;
            int previewCount = Mathf.Min(count, maxPreview);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(200));
            for (int i = 0; i < previewCount; i++)
            {
                var p = comp.BoundaryPositions[i];
                EditorGUILayout.LabelField(i.ToString(), p.ToString("F3"));
            }
            if (count > maxPreview)
            {
                EditorGUILayout.LabelField($"… and {count - maxPreview} more");
            }
            EditorGUILayout.EndScrollView();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
