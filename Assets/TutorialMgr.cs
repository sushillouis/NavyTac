using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TutorialMgr : MonoBehaviour
{
    [SerializeField] public TMP_Text TutorialHeaderText;
    [SerializeField] public TMP_Text TutorialText;

    private enum TutorialStep { None, SelectAll, MoveCamera, Complete }
    private TutorialStep currentStep = TutorialStep.None;
    private Coroutine activeTutorial;

    private GameInputs inputs;

    void Start()
    {
        inputs = UIMgr.inst.inputs;
    }

    void Update()
    {
        if (OpenOceanMain.inst.currentTrainingState == TrainingState.Tutorial)
        {
            TutorialHeaderText.gameObject.SetActive(true);
            TutorialText.gameObject.SetActive(true);

            if (activeTutorial == null)
            {
                RunTutorialSequence();
            }
        }
        else
        {
            TutorialHeaderText.gameObject.SetActive(false);
            TutorialText.gameObject.SetActive(false);

            if (activeTutorial != null)
            {
                StopCoroutine(activeTutorial);
                activeTutorial = null;
                currentStep = TutorialStep.None;
                inputs.Enable();  // Ensure inputs are restored
            }
        }
    }

    private void RunTutorialSequence()
    {
        DisableAllInputMaps(); // Block all other input
        activeTutorial = StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        // STEP 1: F1 (Select All)
        currentStep = TutorialStep.SelectAll;
        TutorialHeaderText.text = "Tutorial: Select All Entities";
        TutorialText.text = "Press 'F1' to select all your units.";

        EnableOnly(inputs.Selection.SelectAll);  // Allow only F1

        yield return new WaitUntil(() => inputs.Selection.SelectAll.WasPressedThisFrame());
        SelectionMgr.inst.SelectAll();

        // STEP 2: F2 (Select All DDG51)
        TutorialHeaderText.text = "Tutorial: Select All DDG51";
        TutorialText.text = "Press 'F2' to select all DDG51 units.";

        EnableOnly(inputs.Selection.SelectAllDDG51); // Allow only F2

        yield return new WaitUntil(() => inputs.Selection.SelectAllDDG51.WasPressedThisFrame());
        SelectionMgr.inst.SelectAllDDG51();

        // STEP 3: F3 (Select SeaHunter)
        TutorialHeaderText.text = "Tutorial: Select SeaHunter";
        TutorialText.text = "Press 'F3' to select all SeaHunter units.";

        EnableOnly(inputs.Selection.SelectAllSEAHUNTER); // Allow only F3

        yield return new WaitUntil(() => inputs.Selection.SelectAllSEAHUNTER.WasPressedThisFrame());
        SelectionMgr.inst.SelectALLSEAHUNTER();

        // STEP 4: F4 (Select All JARIUSV)
        TutorialHeaderText.text = "Tutorial: Select All JARIUSV";
        TutorialText.text = "Press 'F4' to select all JARIUSV units.";

        EnableOnly(inputs.Selection.SelectAllJARIUSV); // Allow only F4

        yield return new WaitUntil(() => inputs.Selection.SelectAllJARIUSV.WasPressedThisFrame());
        SelectionMgr.inst.SelectALLJARIUSV();

        // STEP 5: Camera Controls
        currentStep = TutorialStep.MoveCamera;
        // STEP 5a: Camera Up
        TutorialHeaderText.text = "Tutorial: Camera Controls";
        TutorialText.text = "Press the Up Arrow to move the camera up.";

        EnableOnly(inputs.Camera.XZMove);

        yield return new WaitUntil(() => inputs.Camera.XZMove.ReadValue<Vector2>().y > .5f);

        // STEP 5b: Camera Down
        TutorialHeaderText.text = "Tutorial: Camera Controls";
        TutorialText.text = "Press the Down Arrow to move the camera down.";

        yield return new WaitUntil(() => inputs.Camera.XZMove.ReadValue<Vector2>().y < -.5f);

        // STEP 5c: Camera Left
        TutorialHeaderText.text = "Tutorial: Camera Controls";
        TutorialText.text = "Press the Left Arrow to move the camera left.";

        yield return new WaitUntil(() => inputs.Camera.XZMove.ReadValue<Vector2>().x < -.5f);

        // STEP 5d: Camera Right
        TutorialHeaderText.text = "Tutorial: Camera Controls";
        TutorialText.text = "Press the Right Arrow to move the camera right.";

        yield return new WaitUntil(() => inputs.Camera.XZMove.ReadValue<Vector2>().x > .5f);
        // Complete
        inputs.Enable();  // Restore normal controls



        currentStep = TutorialStep.Complete;
        TutorialHeaderText.text = "Tutorial Complete";
        TutorialText.text = "Great job! You've learned the basic controls.";
        yield return new WaitForSeconds(2f);

        activeTutorial = null;
    }

    private void EnableOnly(params InputAction[] allowedActions)
    {
        DisableAllInputMaps();

        foreach (var action in allowedActions)
        {
            action.Enable();
        }
    }

    private void DisableAllInputMaps()
    {
        inputs.Camera.Disable();
        inputs.Selection.Disable();
        inputs.Entities.Disable();
        inputs.Attacks.Disable();
    }
}
