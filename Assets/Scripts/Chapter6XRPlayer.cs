using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;

// Scene-level XR session and locomotion. Head/controller poses come from
// TrackedPoseDriver components using the same bindings in both scenes.
public class Chapter6XRPlayer : MonoBehaviour
{
    public Camera head;
    public CharacterController body;
    public QuietCornerController experience;
    public Chapter6XRSimulator simulator;
    public string sessionStatus = "Starting";
    InputAction move, turn;
    bool turnHeld;
    public bool IsSimulation => simulator != null && simulator.enabled;

    void OnEnable()
    {
        move = new InputAction("Move", InputActionType.Value, "<XRController>{LeftHand}/primary2DAxis");
        turn = new InputAction("Snap turn", InputActionType.Value, "<XRController>{RightHand}/primary2DAxis");
        move.Enable(); turn.Enable();
    }

    IEnumerator Start()
    {
        if (IsSimulation)
        {
            sessionStatus = "XR SIMULATION | No headset connected";
            yield break;
        }
        sessionStatus = "Connecting to OpenXR runtime...";
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        if (manager == null) { sessionStatus = "OpenXR settings are missing"; yield break; }
        if (!manager.isInitializationComplete) yield return manager.InitializeLoader();
        if (manager.activeLoader == null)
        {
            sessionStatus = "No OpenXR headset. Open QuietCorner_Chapter6_Simulation to preview.";
            Debug.LogWarning(sessionStatus);
            yield break;
        }
        manager.StartSubsystems();
        sessionStatus = "OpenXR session active";
    }

    void Update()
    {
        var direction = move.ReadValue<Vector2>();
        var keyboard = Keyboard.current;
        if (IsSimulation && keyboard != null && !simulator.automated)
        {
            direction += new Vector2(
                ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                    - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0),
                ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0)
                    - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0));
            if (keyboard.qKey.wasPressedThisFrame) SnapTurn(-30);
            if (keyboard.eKey.wasPressedThisFrame && (experience == null || !experience.TryToggleChairSeat())) SnapTurn(30);
            if (keyboard.fKey.wasPressedThisFrame && experience != null) experience.ToggleNearbySeat();
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                experience?.StopExperience();
                Cursor.lockState = CursorLockMode.None;
            }
        }
        if (experience != null && experience.seated) direction = Vector2.zero;
        direction = Vector2.ClampMagnitude(direction, 1);
        Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        bool sprinting = IsSimulation && keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        float speed = experience != null ? experience.MovementSpeed(sprinting) : 1.6f;
        if (experience == null || !experience.seated)
            body.Move(((forward * direction.y + right * direction.x) * speed + Vector3.down * 2) * Time.deltaTime);
        var axis = turn.ReadValue<Vector2>().x;
        if (Mathf.Abs(axis) > .7f && !turnHeld) { SnapTurn(Mathf.Sign(axis) * 30); turnHeld = true; }
        if (Mathf.Abs(axis) < .3f) turnHeld = false;
    }

    void SnapTurn(float degrees) { transform.RotateAround(head.transform.position, Vector3.up, degrees); }

    void OnDisable()
    {
        move?.Dispose(); turn?.Dispose();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (!IsSimulation && XRGeneralSettings.Instance != null)
        {
            var manager = XRGeneralSettings.Instance.Manager;
            if (manager != null && manager.isInitializationComplete) { manager.StopSubsystems(); manager.DeinitializeLoader(); }
        }
    }

}
