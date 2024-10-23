using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
    [HideInInspector]
    public int currentScene = 1;

    public void LoadScene()
    {
        StartCoroutine(LoadYourAsyncScene());
    }

   IEnumerator LoadYourAsyncScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentScene);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    //Sets the current scene to Map Select
   public void GoToMapSelectScene()
   {
        currentScene = 0;
   }
}
