using UnityEngine;
using UnityEngine.InputSystem;

// Routes tracked XR controller input to the existing spatial controls.
public class Chapter6XRRay : MonoBehaviour
{
    public bool leftHand;
    public QuietCornerController experience;
    public LineRenderer line;
    public QuietCornerTarget hovered;
    public int interactionCount;
    InputAction trigger;
    Color idle;

    void OnEnable()
    {
        trigger = new InputAction("Trigger", InputActionType.Button,
            "<XRController>{" + (leftHand ? "LeftHand" : "RightHand") + "}/trigger");
        trigger.Enable();
        idle = leftHand ? new Color(.2f,.85f,.9f) : new Color(1f,.65f,.2f);
    }

    void Update()
    {
        hovered = null;
        Vector3 end = transform.position + transform.forward * 12;
        if (Physics.Raycast(transform.position, transform.forward, out var hit, 25, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            hovered = hit.collider.GetComponentInParent<QuietCornerTarget>();
        }
        line.SetPosition(0,transform.position);
        line.SetPosition(1,end);
        line.startColor = line.endColor = hovered != null ? Color.green : idle;
        if (hovered != null && trigger.WasPressedThisFrame())
        {
            experience.Activate(hovered.action,hovered.destination);
            interactionCount++;
        }
    }

    void OnDisable() { trigger?.Dispose(); }
}
