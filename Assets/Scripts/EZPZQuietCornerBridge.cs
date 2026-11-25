using UnityEngine;
using UnityEngine.Events;

// Connects the mandated EZPZ interaction event layer to a Quiet Corner target.
// The bridge is intentionally small so the spatial controls remain readable in
// the scene while EZPZ owns the primary interaction event and hover pulse.
[RequireComponent(typeof(QuietCornerTarget))]
public sealed class EZPZQuietCornerBridge : MonoBehaviour
{
    InteractableGeneral interactable;
    QuietCornerTarget target;
    public int activationCount;

    void Awake()
    {
        target = GetComponent<QuietCornerTarget>();
        interactable = GetComponent<InteractableGeneral>();
        if (interactable == null) interactable = gameObject.AddComponent<InteractableGeneral>();
        interactable.onPrimaryInteract ??= new UnityEvent();
        interactable.onPrimaryInteractLift ??= new UnityEvent();
        interactable.onSecondaryInteract ??= new UnityEvent();
        interactable.onSecondaryInteractLift ??= new UnityEvent();
        interactable.onHoverEnter ??= new UnityEvent();
        interactable.onHoverExit ??= new UnityEvent();
        interactable.onPrimaryInteractBackground ??= new UnityEvent();
        interactable.onPrimaryInteractLiftBackground ??= new UnityEvent();
        interactable.onSecondaryInteractBackground ??= new UnityEvent();
        interactable.onSecondaryInteractLiftBackground ??= new UnityEvent();
        interactable.onHoverEnterBackground ??= new UnityEvent();
        interactable.onHoverExitBackground ??= new UnityEvent();
        interactable.hoverText = "Press trigger to choose";
        var pulse = GetComponent<ScalePulse>();
        if (pulse == null) pulse = gameObject.AddComponent<ScalePulse>();
        pulse.scaleFactor = Vector3.one * .035f;
        interactable.onPrimaryInteract.AddListener(ActivateTarget);
        interactable.onPrimaryInteract.AddListener(pulse.Pulse);
    }

    void ActivateTarget()
    {
        activationCount++;
        var controller = FindFirstObjectByType<QuietCornerController>();
        if (controller != null) controller.Activate(target.action, target.destination, target);
    }
}
