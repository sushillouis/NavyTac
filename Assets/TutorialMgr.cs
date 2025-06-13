using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class TutorialMgr : MonoBehaviour
{
    [SerializeField] public TMP_Text TutorialHeaderText;
    [SerializeField] public TMP_Text TutorialText;

    private enum TutorialStep { None, SelectAll, MoveCamera, UnitCommands, Complete }
    private TutorialStep currentStep = TutorialStep.None;
    private Coroutine activeTutorial;

    private GameInputs inputs;
    Vector3 worldCenter = Vector3.zero;
    Vector3 screenCenter ;

    public GameObject capsulePrefab; // Prefab for the capsule used in the tutorial




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
    // Disable all inputs initially
    DisableAllInputMaps();
    
    // === CAMERA CONTROLS SECTION ===
    currentStep = TutorialStep.MoveCamera;
    DisableAllInputMaps(); // Ensure all inputs are disabled at the start
    // Camera Movement (Arrow Keys)
    // === CAMERA MOVEMENT ===
    TutorialHeaderText.text = "Tutorial: Camera Movement";
    TutorialText.text = 
        "Use <sprite name=ku>, <sprite name=kd>, <sprite name=kl>, <sprite name=kr> to move the camera.\n" +
        "You can also move the mouse to the top/bottom/left/right edges of the screen to move the camera in that direction.";
    EnableOnly(inputs.Camera.XZMove);

    // Wait for movement in all directions
    bool movedUp = false, movedDown = false, movedLeft = false, movedRight = false;
    while (!(movedUp && movedDown && movedLeft && movedRight))
    {
        Vector2 moveInput = inputs.Camera.XZMove.ReadValue<Vector2>();
        if (moveInput.y > 0.5f) movedUp = true;
        if (moveInput.y < -0.5f) movedDown = true;
        if (moveInput.x < -0.5f) movedLeft = true;
        if (moveInput.x > 0.5f) movedRight = true;

        // Mouse at screen edge (simulate edge pan)
        Vector2 mousePos = inputs.Selection.CursorPosition.ReadValue<Vector2>();
        if (mousePos.x <= 5) movedLeft = true;
        if (mousePos.x >= Screen.width - 5) movedRight = true;
        if (mousePos.y <= 5) movedDown = true;
        if (mousePos.y >= Screen.height - 5) movedUp = true;

        yield return null;
    }
    yield return new WaitForSeconds(2f);

    // === CAMERA ZOOM ===
    TutorialHeaderText.text = "Tutorial: Camera Zoom";
    TutorialText.text = 
        "Press <sprite name=r> to zoom in, <sprite name=f> to zoom out.\n" +
        "You can also scroll the mouse wheel up/down to zoom in and out.";
    EnableOnly(inputs.Camera.YMove);

    bool zoomedIn = false, zoomedOut = false;
    while (!(zoomedIn && zoomedOut))
    {
        Vector2 zoomInput = inputs.Camera.YMove.ReadValue<Vector2>();
        if (zoomInput.y < -0.5f) zoomedIn = true;  // R key
        if (zoomInput.y > 0.5f) zoomedOut = true;  // F key

        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        if (scroll > 0.1f) zoomedIn = true;
        if (scroll < -0.1f) zoomedOut = true;

        yield return null;
    }
    yield return new WaitForSeconds(2f);

    // === CAMERA ROTATION ===
    TutorialHeaderText.text = "Tutorial: Camera Rotation";
    TutorialText.text = 
        "Press <sprite name=q> to rotate left, <sprite name=e> to rotate right.\n" +
        "Or, hold the <sprite name=mm> middle mouse button and move the mouse left/right to rotate the camera.";
    EnableOnly(inputs.Camera.Yaw);

    bool rotatedLeft = false, rotatedRight = false;
    while (!(rotatedLeft && rotatedRight))
    {
        float yawInput = inputs.Camera.Yaw.ReadValue<float>();
        if (yawInput < -0.25f) rotatedLeft = true;
        if (yawInput > 0.25f) rotatedRight = true;

        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.x < -2f) rotatedLeft = true;
            if (mouseDelta.x > 2f) rotatedRight = true;
        }

        yield return null;
    }
    yield return new WaitForSeconds(2f);

    // === CAMERA TILT ===
    TutorialHeaderText.text = "Tutorial: Camera Tilt";
    TutorialText.text = 
        "Press <sprite name=z> to tilt up, <sprite name=x> to tilt down.\n" +
        "Or, hold the <sprite name=mm> middle mouse button and move the mouse up/down to tilt the camera.";
    EnableOnly(inputs.Camera.Pitch);

    bool tiltedUp = false, tiltedDown = false;
    while (!(tiltedUp && tiltedDown))
    {
        float pitchInput = inputs.Camera.Pitch.ReadValue<float>();
        if (pitchInput < -0.25f) tiltedUp = true;
        if (pitchInput > 0.25f) tiltedDown = true;

        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.y < -2f) tiltedUp = true;
            if (mouseDelta.y > 2f) tiltedDown = true;
        }

        yield return null;
    }
    yield return new WaitForSeconds(2f);

    // === CAMERA RESET ===
    TutorialHeaderText.text = "Tutorial: Reset Camera";
    TutorialText.text = "Press <sprite name=c> to reset the camera position.";
    EnableOnly(inputs.Camera.RTSView);
    yield return new WaitUntil(() => inputs.Camera.RTSView.WasPressedThisFrame());
    yield return new WaitForSeconds(2f); // Allow reset to complete

    
    // === SELECTION CONTROLS SECTION ===
    currentStep = TutorialStep.SelectAll;
    
    // Clear any previous selections
    SelectionMgr.inst.ClearSelection();
    
    // Single Selection
    TutorialHeaderText.text = "Tutorial: Single Selection";
    TutorialText.text = "<sprite name=ml> on a unit to select it.";
    EnableOnly(inputs.Selection.SingleSelect, inputs.Selection.CursorPosition);
    
    yield return new WaitUntil(() =>  SelectionMgr.inst.selectedEntities.Count == 1);
    yield return new WaitForSeconds(2f); // Allow selection to complete
                                           // Get the center of the world for simulation
    SelectionMgr.inst.ClearSelection();
    // Box Selection
        TutorialHeaderText.text = "Tutorial: Box Selection";
TutorialText.text = "Click and drag with your mouse to draw a selection box.";


// Clear previous selection and prepare for simulation
SelectionMgr.inst.ClearSelection();
screenCenter = Camera.main.WorldToScreenPoint(worldCenter);

// Create simulation box coordinates
Vector3 simBoxStart = screenCenter + new Vector3(-300, -300, 0);
Vector3 simBoxEnd = screenCenter + new Vector3(300, 300, 0);

// Show simulation
SelectionMgr.inst.SimulateBoxSelection(simBoxStart, simBoxEnd);
yield return new WaitForSeconds(2f); // Let player see the selection
EnableOnly(inputs.Selection.BoxSelect);
// Clear simulated selection and prepare for player
SelectionMgr.inst.ClearSelection();
TutorialText.text = "Now you try! Drag to select multiple units.";

// Wait for player to perform box selection
    yield return new WaitUntil(() => SelectionMgr.inst.selectedEntities.Count > 1);
    yield return new WaitForSeconds(2f);
    // Select All (F1)
    TutorialHeaderText.text = "Tutorial: Select All Units";
    TutorialText.text = "Press <sprite name=f1> to select all units.";
    EnableOnly(inputs.Selection.SelectAll);
    
    SelectionMgr.inst.ClearSelection();
    yield return new WaitUntil(() => inputs.Selection.SelectAll.WasPressedThisFrame());
    yield return new WaitForSeconds(2f); // Allow selection to complete
    
    // Unit Type Selection (F2-F4)
    TutorialHeaderText.text = "Tutorial: Select by Type";
    TutorialText.text = "Press <sprite name=f2> for DDG51, <sprite name=f3> for SeaHunter, <sprite name=f4> for JARIUSV.";
    
    // DDG51 Selection (F2)
    SelectionMgr.inst.ClearSelection();
    EnableOnly(inputs.Selection.SelectAllDDG51);
    yield return new WaitUntil(() => inputs.Selection.SelectAllDDG51.WasPressedThisFrame());
    yield return new WaitForSeconds(2f);
    
    // SeaHunter Selection (F3)
    SelectionMgr.inst.ClearSelection();
    EnableOnly(inputs.Selection.SelectAllSEAHUNTER);
    yield return new WaitUntil(() => inputs.Selection.SelectAllSEAHUNTER.WasPressedThisFrame());
    yield return new WaitForSeconds(2f);
    
    // JARIUSV Selection (F4)
    SelectionMgr.inst.ClearSelection();
    EnableOnly(inputs.Selection.SelectAllJARIUSV);
    yield return new WaitUntil(() => inputs.Selection.SelectAllJARIUSV.WasPressedThisFrame());
    yield return new WaitForSeconds(2f);
    
    // === UNIT COMMANDS SECTION ===
    currentStep = TutorialStep.UnitCommands;
    
    // Move Command (Right Click)
    SelectionMgr.inst.ClearSelection();
    TutorialHeaderText.text = "Tutorial: Move Command";
    TutorialText.text = "Select a unit or multiple units.";
    // Wait for unit selection
    EnableOnly(
        inputs.Selection.SingleSelect, 
        inputs.Selection.CursorPosition, 
        inputs.Selection.BoxSelect,
        inputs.Selection.SelectAll, 
        inputs.Selection.SelectAllDDG51, 
        inputs.Selection.SelectAllSEAHUNTER, 
        inputs.Selection.SelectAllJARIUSV
    );
    yield return new WaitUntil(() => SelectionMgr.inst.selectedEntities.Count >= 1);

        // Use a prefab capsule from the inspector
        Vector3 targetWorldPos = new Vector3(500, 30, 3000); // Example offset
        GameObject capsule = null;
        if (capsulePrefab != null)
        {
            capsule = Instantiate(capsulePrefab, targetWorldPos, Quaternion.identity);
            capsule.name = "TutorialTargetCapsule";
            var collider = capsule.GetComponent<Collider>();
            if (collider != null) collider.enabled = false; // Prevent interaction
        }

        // Move command instruction
        TutorialText.text = $"Now <sprite name=mr> on the red capsule to move your units there.\n\nWhen you use move, your units will not stop for any opponent entity on the way, but will attack only if you stop near an enemy.";

        EnableOnly(inputs.Entities.Command, inputs.Selection.CursorPosition);

        // Wait for player to issue a move command within a 500x500 box centered on the capsule
        bool commandIssued = false;
        while (!commandIssued)
        {
            if (inputs.Entities.Command.WasPressedThisFrame())
            {
                // Raycast from mouse position to world
                Vector3 mousePos = inputs.Selection.CursorPosition.ReadValue<Vector2>();
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 hitPoint = hit.point;
                    // Check if hitPoint is within 250 units in X and Z from targetWorldPos (500x500 box)
                    if (Mathf.Abs(hitPoint.x - targetWorldPos.x) <= 250f &&
                        Mathf.Abs(hitPoint.z - targetWorldPos.z) <= 250f)
                    {
                        commandIssued = true;
                        if (capsule != null)
                            GameObject.Destroy(capsule);
                            DisableAllInputMaps(); // Disable all inputs after command is issued
                    }
                }
            }
            yield return null;
        }

    // Store selected entities at the time of command
    List<Entity> selectedEntities = new List<Entity>(SelectionMgr.inst.selectedEntities);

    // Wait until all selected entities have at least one command in their queue
    yield return new WaitUntil(() =>
        selectedEntities.TrueForAll(entity => 
            entity.GetComponentInChildren<UnitAI>() != null &&
            entity.GetComponentInChildren<UnitAI>().commands.Count > 0
        )
    );

    // Wait until all selected entities have finished their commands (queue is empty)
    yield return new WaitUntil(() =>
        selectedEntities.TrueForAll(entity => 
            entity.GetComponentInChildren<UnitAI>() != null &&
            entity.GetComponentInChildren<UnitAI>().commands.Count == 0
        )
    );

    // Remove the capsule after move is complete
    if (capsule != null)
        GameObject.Destroy(capsule);

    yield return new WaitForSeconds(2f); // Allow command to complete
    
    // Attack Move (A + Right Click)
    SelectionMgr.inst.ClearSelection();
    TutorialHeaderText.text = "Tutorial: Attack Move";
    TutorialText.text = "Select a unit or multiple units.";

    // Wait for unit selection
    EnableOnly(
        inputs.Selection.SingleSelect, 
        inputs.Selection.CursorPosition, 
        inputs.Selection.BoxSelect,
        inputs.Selection.SelectAll, 
        inputs.Selection.SelectAllDDG51, 
        inputs.Selection.SelectAllSEAHUNTER, 
        inputs.Selection.SelectAllJARIUSV
    );
    yield return new WaitUntil(() => 
        SelectionMgr.inst.selectedEntities.Count >= 1);

    // Create a capsule for attack move target
    Vector3 attackMoveTargetPos = new Vector3(0, 30, 7000); // Example offset, different from move command
    GameObject attackCapsule = null;
    if (capsulePrefab != null)
    {
        attackCapsule = Instantiate(capsulePrefab, attackMoveTargetPos, Quaternion.identity);
        attackCapsule.name = "TutorialAttackMoveCapsule";
        var collider = attackCapsule.GetComponent<Collider>();
        if (collider != null) collider.enabled = false; // Prevent interaction
    }

    // Attack move command
    TutorialText.text = "Now hold <sprite name=a> and <sprite name=mr> on the red capsule to attack move.\n\nWhen you use attack move, your units will stop and destroy any enemy they encounter, then continue moving forward.\n\nTip: If you attack move and select an opponent entity, all your selected units will attack that target entity.";
    EnableOnly(inputs.Attacks.Attack1, inputs.Entities.Command, inputs.Selection.CursorPosition);

    // Wait for player to issue an attack move command within a 500x500 box centered on the capsule
    bool attackMoveIssued = false;
    while (!attackMoveIssued)
    {
        if (inputs.Attacks.Attack1.IsPressed() && inputs.Entities.Command.WasPressedThisFrame())
        {
            Vector3 mousePos = inputs.Selection.CursorPosition.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 hitPoint = hit.point;
                if (Mathf.Abs(hitPoint.x - attackMoveTargetPos.x) <= 250f &&
                    Mathf.Abs(hitPoint.z - attackMoveTargetPos.z) <= 250f)
                {
                    attackMoveIssued = true;
                    if (attackCapsule != null)
                        GameObject.Destroy(attackCapsule);
                    DisableAllInputMaps(); // Disable all inputs after command is issued
                }
            }
        }
        yield return null;
    }

    // Store selected entities at the time of command
    List<Entity> attackMoveEntities = new List<Entity>(SelectionMgr.inst.selectedEntities);



    // Wait until all selected entities have finished their commands (queue is empty)
    yield return new WaitUntil(() =>
        attackMoveEntities.TrueForAll(entity => 
            entity.GetComponentInChildren<UnitAI>() != null &&
            entity.GetComponentInChildren<UnitAI>().commands.Count == 0
        )
    );

    yield return new WaitForSeconds(2f); // Allow command to complete
    currentStep = TutorialStep.Complete;
    inputs.Enable();  // Restore all inputs
    
    TutorialHeaderText.text = "Tutorial Complete";
    TutorialText.text = "Great job! You've learned all basic controls.";
    
    yield return new WaitForSeconds(3f);
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
