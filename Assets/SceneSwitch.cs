using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
 [HideInInspector]
 public int currentScene = 1;

   //Sets the current scene to the selected Map
   public void GoToScene()
   {
        SceneManager.LoadScene(currentScene);
   }

    //Sets the current scene to Map Select
   public void GoToMapSelectScene()
   {
        currentScene = 0;
        SceneManager.LoadScene(currentScene);
   }
}
