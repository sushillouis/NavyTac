#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ResetScene))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        // // Add a button to trigger some action
        ResetScene resetScene = (ResetScene)target;
        if (GUILayout.Button("Reset Scene"))
        {
            resetScene.ReloadScene();
        }


    }
}

[CustomEditor(typeof(FogWarMgr))]
public class FogWarMgrEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FogWarMgr fogWarMgr = (FogWarMgr)target;

        if (GUILayout.Button("Toggle Fog of War"))
        {
            fogWarMgr.FOW = !fogWarMgr.FOW;
            EditorUtility.SetDirty(fogWarMgr); // Mark the object as dirty to ensure changes are saved
        }
    }
}
#endif