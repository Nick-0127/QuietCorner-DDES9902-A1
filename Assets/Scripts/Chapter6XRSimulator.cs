using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Keyboard/mouse drive Unity's simulated XR devices, not the camera directly.
// TrackedPoseDriver and XR bindings therefore follow the real input pathway.
public class Chapter6XRSimulator : MonoBehaviour
{
    public Camera headCamera;
    public Transform trackingSpace;
    public bool automated;
    public Vector3 headPosition = new Vector3(0,1.65f,0);
    public Quaternion headRotation = Quaternion.identity;
    public Vector3 leftPosition = new Vector3(-.25f,1.25f,.35f);
    public Vector3 rightPosition = new Vector3(.25f,1.25f,.35f);
    public Quaternion leftRotation = Quaternion.identity, rightRotation = Quaternion.identity;
    public bool leftTrigger, rightTrigger;
    XRSimulatedHMD hmd;
    XRSimulatedController left, right;
    float yaw, pitch;

    void OnEnable()
    {
        InputSystem.RegisterLayout<XRSimulatedHMD>();
        InputSystem.RegisterLayout<XRSimulatedController>();
        hmd = InputSystem.AddDevice<XRSimulatedHMD>("Chapter6 simulated headset");
        left = InputSystem.AddDevice<XRSimulatedController>("Chapter6 simulated left hand");
        right = InputSystem.AddDevice<XRSimulatedController>("Chapter6 simulated right hand");
        InputSystem.SetDeviceUsage(left, "LeftHand");
        InputSystem.SetDeviceUsage(right, "RightHand");
        InputSystem.onBeforeUpdate += BeforeInput;
    }

    void BeforeInput()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        if (!automated && Mouse.current != null)
        {
            var mouse = Mouse.current;
            if (mouse.rightButton.isPressed)
            {
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * .12f; pitch = Mathf.Clamp(pitch - delta.y * .12f,-75,75);
            }
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            headRotation = Quaternion.Euler(pitch,yaw,0);
            leftPosition = headPosition + headRotation * new Vector3(-.25f,-.4f,.35f);
            rightPosition = headPosition + headRotation * new Vector3(.25f,-.4f,.35f);
            leftRotation = headRotation;
            Vector2 pointer = mouse.rightButton.isPressed ? new Vector2(Screen.width*.5f,Screen.height*.5f) : mouse.position.ReadValue();
            Ray ray = headCamera.ScreenPointToRay(pointer);
            Vector3 point = Physics.Raycast(ray,out var hit,30,~0,QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(12);
            rightRotation = Quaternion.LookRotation(trackingSpace.InverseTransformPoint(point) - rightPosition,Vector3.up);
            rightTrigger = mouse.leftButton.isPressed;
        }
        var state = new XRSimulatedHMDState
        {
            isTracked = true, trackingState = 3, devicePosition = headPosition, deviceRotation = headRotation,
            centerEyePosition = headPosition, centerEyeRotation = headRotation,
            leftEyePosition = headPosition + headRotation * Vector3.left * .032f, leftEyeRotation = headRotation,
            rightEyePosition = headPosition + headRotation * Vector3.right * .032f, rightEyeRotation = headRotation
        };
        InputSystem.QueueStateEvent(hmd,state);
        InputSystem.QueueStateEvent(left,ControllerState(leftPosition,leftRotation,leftTrigger));
        InputSystem.QueueStateEvent(right,ControllerState(rightPosition,rightRotation,rightTrigger));
    }

    XRSimulatedControllerState ControllerState(Vector3 position, Quaternion rotation, bool pressed)
    {
        return new XRSimulatedControllerState { isTracked=true, trackingState=3,
            devicePosition=position, deviceRotation=rotation, trigger=pressed ? 1 : 0 }.WithButton(ControllerButton.TriggerButton,pressed);
    }

    public void AimAt(Vector3 worldPoint, bool useLeft = false)
    {
        var p = trackingSpace.InverseTransformPoint(worldPoint);
        if (useLeft) leftRotation=Quaternion.LookRotation(p-leftPosition,Vector3.up);
        else rightRotation=Quaternion.LookRotation(p-rightPosition,Vector3.up);
    }

    void OnDisable()
    {
        InputSystem.onBeforeUpdate -= BeforeInput;
        if (hmd != null && hmd.added) InputSystem.RemoveDevice(hmd);
        if (left != null && left.added) InputSystem.RemoveDevice(left);
        if (right != null && right.added) InputSystem.RemoveDevice(right);
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
    }
}
