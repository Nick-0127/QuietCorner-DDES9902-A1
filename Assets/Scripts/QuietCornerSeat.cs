using UnityEngine;

// The chair's forward direction faces its table. The player origin stays at floor level.
public sealed class QuietCornerSeat : MonoBehaviour
{
    public string seatName = "Table seat";
    public Vector3 SittingPosition => transform.position + Vector3.up * .05f;
}
