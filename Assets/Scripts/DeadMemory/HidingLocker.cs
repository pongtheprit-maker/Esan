using UnityEngine;

/// <summary>A separate physical hide cabinet; puzzle lockers retain their own interaction.</summary>
public class HidingLocker : MonoBehaviour
{
    public string lockerId;
    public Transform hinge, inside, approach;
    public Collider[] doorColliders;
    public PlayerHiding Occupant { get; private set; }
    public bool DoorClosed { get { return Mathf.Abs(angle) < .6f && target == 0; } }
    public bool DoorOpen { get { return Mathf.Abs(angle + 105) < 1; } }
    float angle, target, velocity;
    public Vector3 InsidePosition { get { return inside.position; } }
    public Vector3 ApproachPosition { get { return approach.position; } }
    void Update()
    {
        angle = Mathf.SmoothDamp(angle, target, ref velocity, .16f, 230, Time.deltaTime);
        if (Mathf.Abs(angle - target) < .05f) { angle = target; velocity = 0; }
        ApplyDoor();
    }
    void ApplyDoor()
    {
        if (hinge != null) hinge.localRotation = Quaternion.Euler(0, angle, 0);
        foreach (var c in doorColliders) if (c != null) c.isTrigger = !DoorClosed;
    }
    public void SetDoor(bool open) { target = open ? -105 : 0; ApplyDoor(); }
    public bool CanEnter(PlayerHiding player)
    {
        if (player == null || Occupant != null || player.BlocksInput) return false;
        Vector3 local = transform.InverseTransformPoint(player.transform.position);
        return local.z >= 1.02f && local.z < 3.1f && Mathf.Abs(local.x) < 1.2f && Mathf.Abs(local.y) < .4f;
    }
    public string Prompt(PlayerHiding player, out bool available)
    { available = CanEnter(player); return Occupant != null ? "มีคนอยู่ในตู้" : available ? "เข้าไปซ่อนในล็อกเกอร์" : "เข้าจากด้านหน้าล็อกเกอร์"; }
    public bool TryEnter(PlayerHiding player) { return CanEnter(player) && player.Enter(this); }
    public void Claim(PlayerHiding player) { Occupant = player; }
    public void Release(bool leaveOpen)
    { Occupant = null; angle = target = leaveOpen ? -105 : 0; velocity = 0; ApplyDoor(); }
}
