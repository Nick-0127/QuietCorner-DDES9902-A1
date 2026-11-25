using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System.Runtime.InteropServices;

// Desktop previews use the same click-once mouse controls as WebGL.
// Physical OpenXR and explicit simulated-device verification keep XR input.
public sealed class QuietCornerBrowserControls : MonoBehaviour
{
    Chapter6XRPlayer rig;
    Camera head;
    QuietCornerController experience;
    float yaw, pitch;
    bool wasLocked;
    bool browserActive;
    int instructionMode = -1;
#if UNITY_WEBGL && !UNITY_EDITOR
    readonly float[] browserFrame = new float[5];
    [DllImport("__Internal")] static extern void QC_MouseInit();
    [DllImport("__Internal")] static extern void QC_MouseRead([Out] float[] output, float dt);
    [DllImport("__Internal")] static extern void QC_MouseRelease();
#endif
    bool Looking
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return browserActive;
#else
            return Cursor.lockState == CursorLockMode.Locked;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartBrowserPreview()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-chapter6-verify") >= 0 ||
            System.Array.IndexOf(args, "-qc-verify") >= 0 ||
            System.Array.IndexOf(args, "-qc-browser-verify") >= 0) return;
        EnablePreview();
    }

    public static QuietCornerBrowserControls EnablePreview()
    {
        var player = FindFirstObjectByType<Chapter6XRPlayer>();
        if (player != null && player.GetComponent<QuietCornerBrowserControls>() != null)
            return player.GetComponent<QuietCornerBrowserControls>();
        if (player == null || !player.IsSimulation) return null;
        return player.gameObject.AddComponent<QuietCornerBrowserControls>();
    }

    void Awake()
    {
        rig = GetComponent<Chapter6XRPlayer>();
        head = rig.head;
        experience = rig.experience;
        // Stop pose drivers before removing their simulated devices, so no XR
        // update can overwrite the browser camera or fire a second selection.
        foreach (var driver in rig.GetComponentsInChildren<TrackedPoseDriver>()) driver.enabled = false;
        foreach (var ray in rig.GetComponentsInChildren<Chapter6XRRay>())
        {
            ray.enabled = false;
            ray.gameObject.SetActive(false);
        }
        rig.enabled = false;
        rig.simulator.enabled = false;
        experience.useDesktopInput = false;
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0, head.transform.localEulerAngles.x);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var instructions = GameObject.Find("Simple movement instructions");
        if (instructions != null)
                instructions.GetComponent<TextMesh>().text = "Click + move mouse - look\nQ - turn left | E - chair / right\nAim dot + click - select\nWASD / arrows - move | Shift fast\nF sofa | Esc - stop";
#if UNITY_WEBGL && !UNITY_EDITOR
        QC_MouseInit();
#endif
        Debug.Log("QUIET_CORNER_MOUSE_V12: Unity Play iframe denies pointer lock; R/RMB recenter, Q/E turn steps; no edge auto-turn");
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ReleaseMouse();
                experience.StopExperience();
                return;
            }
            if (Looking)
            {
                if (keyboard.qKey.wasPressedThisFrame) yaw -= 30;
                if (keyboard.eKey.wasPressedThisFrame && !experience.TryToggleChairSeat()) yaw += 30;
                if (keyboard.fKey.wasPressedThisFrame) experience.ToggleNearbySeat();
            }
        }
        ReadPointer();
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        head.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        Vector2 move = Vector2.zero;
        if (keyboard != null && Looking && !experience.seated)
        {
            move.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
            move.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0);
        }
        move = Vector2.ClampMagnitude(move, 1);
        if (!experience.seated)
            rig.body.Move(((transform.forward * move.y + transform.right * move.x) * experience.MovementSpeed(keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                + Vector3.down * 2) * Time.deltaTime);
    }

    public void SyncView()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0, head.transform.localEulerAngles.x);
    }

    void ReadPointer()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        QC_MouseRead(browserFrame, Time.unscaledDeltaTime);
        browserActive = browserFrame[2] > .5f;
        int mode = !browserActive ? 0 : browserFrame[4] > .5f ? 1 : 2;
        if (instructionMode != mode)
        {
            instructionMode = mode;
            var instructions = GameObject.Find("Simple movement instructions");
            if (instructions != null)
                instructions.GetComponent<TextMesh>().text = mode == 2
                    ? "Mouse look | Q/E turn 30 degrees\nAt edge: press R or hold RMB.\nRecenter, resume, and sweep again.\nAim dot + click = select\nWASD / arrows move | Shift fast\nE chair | F sofa | Esc stop"
                    : "Mouse look | Q - left | E - right\nAim dot + click = select\nWASD / arrows move | Shift fast\nE chair | F sofa | Esc stop";
        }
        if (!browserActive) return;
        yaw += browserFrame[0] * 100;
        pitch = Mathf.Clamp(pitch - browserFrame[1] * 100, -70, 70);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        head.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        if (browserFrame[3] > .5f) ActivateCenteredTarget();
#else
        var mouse = Mouse.current;
        if (mouse == null) return;
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        if (!locked)
        {
            wasLocked = false;
            Cursor.visible = true;
            Vector2 position = mouse.position.ReadValue();
            bool inside = position.x >= 0 && position.y >= 0 && position.x < Screen.width && position.y < Screen.height;
            // The browser needs a click before it can capture the pointer.
            // This first click only enters look mode; it never selects a button.
            if (inside && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }
        Cursor.visible = false;
        if (!wasLocked)
        {
            wasLocked = true;
            return; // Ignore the cursor recentering on entry.
        }
        // Relative movement continues at the screen edge. No mouse button is
        // required for looking; left click is reserved for the centre aim dot.
        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x / Mathf.Max(1, Screen.height) * 100;
        pitch = Mathf.Clamp(pitch - delta.y / Mathf.Max(1, Screen.height) * 100, -70, 70);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        head.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        if (mouse.leftButton.wasPressedThisFrame)
            ActivateCenteredTarget();
#endif
    }

    void ActivateCenteredTarget()
    {
        var target = TargetAt(new Vector2(Screen.width * .5f, Screen.height * .5f));
        if (target == null) return;
        var ezpz = target.GetComponent<InteractableGeneral>();
        if (ezpz != null) ezpz.onPrimaryInteract.Invoke();
        else experience.Activate(target.action, target.destination,target);
        if (target.action == "stop" || target.action == "reset") ReleaseMouse();
    }

    QuietCornerTarget TargetAt(Vector2 position)
    {
        return Physics.Raycast(head.ScreenPointToRay(position), out var hit, 25, ~0, QueryTriggerInteraction.Ignore)
            ? hit.collider.GetComponentInParent<QuietCornerTarget>() : null;
    }

    void OnGUI()
    {
        if (!Looking) return;
        // A small dot replaces the pointer while looking; keep the room clear.
        var center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        GUI.color = new Color(0, 0, 0, .65f);
        GUI.DrawTexture(new Rect(center.x - 3, center.y - 3, 6, 6), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(center.x - 1, center.y - 1, 2, 2), Texture2D.whiteTexture);
    }
    void ReleaseMouse()
    {
        wasLocked = false; browserActive = false;
#if UNITY_WEBGL && !UNITY_EDITOR
        QC_MouseRelease();
#else
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
    }
    void OnApplicationFocus(bool focused) { if (!focused) ReleaseMouse(); }
    void OnDisable() { ReleaseMouse(); }
}
