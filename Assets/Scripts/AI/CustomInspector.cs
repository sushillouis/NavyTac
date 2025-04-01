
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameMgr))]
public class CustomInspector : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();
        // // Add a button to trigger some action
         GameMgr gameMgr = (GameMgr)target;
        if (GUILayout.Button("Do Something"))
        {
           gameMgr.ReloadScene();
        }

        
    }
}
