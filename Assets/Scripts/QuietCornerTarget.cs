using UnityEngine;

// A physical, labelled control. The same actions can later be called by an XR ray.
public class QuietCornerTarget : MonoBehaviour
{
    public string action;
    public Vector3 destination;

    void Awake()
    {
        // EZPZ owns the primary interaction event for every spatial control.
        // Adding the bridge here keeps both the authored scene and the XR
        // simulation on the same interaction path.
        if (GetComponent<EZPZQuietCornerBridge>() == null)
            gameObject.AddComponent<EZPZQuietCornerBridge>();
    }
}
