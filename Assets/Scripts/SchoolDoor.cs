using UnityEngine;

/// <summary>A solid hinged leaf. Sweeps its opening arc and pauses against people or furniture.</summary>
public class SchoolDoor : MonoBehaviour
{
    public Transform leaf;
    public float openAngle = 90f;
    public float rotationSpeed = 135f;
    public float smoothTime = .28f;
    public bool startOpen;
    public bool IsOpen { get { return isOpen; } }
    public bool IsBlocked { get; private set; }
    public float CurrentAngle { get { return angle; } }
    bool isOpen;
    float angle, angleVelocity;
    Quaternion closedRotation, leafRotation;
    Vector3 leafOffset;
    BoxCollider leafCollider;
    Rigidbody body;
    readonly Collider[] hits = new Collider[96];

    void Awake()
    {
        if (leaf == null)
        {
            foreach (var candidate in GetComponentsInChildren<BoxCollider>())
                if (!candidate.isTrigger) { leaf = candidate.transform; break; }
        }
        if (leaf == null) { enabled = false; return; }
        leafCollider = leaf.GetComponent<BoxCollider>();
        if (leafCollider == null) { enabled = false; return; }
        leafCollider.enabled = true; leafCollider.isTrigger = false;
        closedRotation = transform.localRotation;
        leafOffset = transform.InverseTransformPoint(leaf.position);
        leafRotation = Quaternion.Inverse(transform.rotation) * leaf.rotation;
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true; body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        isOpen = startOpen; angle = startOpen ? openAngle : 0;
        transform.localRotation = closedRotation * Quaternion.Euler(0, angle, 0);
    }
    public void Toggle() { if (leafCollider != null) isOpen = !isOpen; }
    void FixedUpdate()
    {
        if (leafCollider == null) return;
        float target = isOpen ? openAngle : 0;
        float proposed = Mathf.SmoothDamp(angle, target, ref angleVelocity, smoothTime, rotationSpeed, Time.fixedDeltaTime);
        if (Mathf.Abs(proposed - target) < .05f && Mathf.Abs(angleVelocity) < 1) { proposed = target; angleVelocity = 0; }
        IsBlocked = false;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(proposed - angle) / 1f));
        float from = angle, accepted = angle;
        for (int i = 1; i <= steps; i++)
        {
            float sample = Mathf.Lerp(from, proposed, i / (float)steps);
            if (!CanOccupy(sample)) { IsBlocked = true; angleVelocity = 0; break; }
            accepted = sample;
        }
        angle = accepted;
        Quaternion rotation = WorldRotation(angle);
        if (body != null) body.MoveRotation(rotation); else transform.rotation = rotation;
    }
    Quaternion WorldRotation(float value)
    {
        return (transform.parent != null ? transform.parent.rotation : Quaternion.identity) * closedRotation * Quaternion.Euler(0, value, 0);
    }
    bool CanOccupy(float value)
    {
        Quaternion rootRotation = WorldRotation(value);
        Vector3 position = transform.position + rootRotation * Vector3.Scale(leafOffset, transform.lossyScale);
        Quaternion rotation = rootRotation * leafRotation;
        Vector3 scale = leafCollider.transform.lossyScale;
        Vector3 center = position + rotation * Vector3.Scale(leafCollider.center, scale);
        Vector3 half = Vector3.Scale(leafCollider.size, scale) * .5f;
        half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
        int count = Physics.OverlapBoxNonAlloc(center, half + Vector3.one * .002f, hits, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider[] candidates = count == hits.Length ? Physics.OverlapBox(center, half + Vector3.one * .002f, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) : hits;
        if (candidates != hits) count = candidates.Length;
        for (int i = 0; i < count; i++)
        {
            var other = candidates[i];
            if (other == null || other.transform.IsChildOf(transform)) continue;
            Vector3 direction; float depth;
            if (Physics.ComputePenetration(leafCollider, position, rotation, other, other.transform.position, other.transform.rotation, out direction, out depth) && depth > .004f)
                return false;
        }
        return true;
    }
}
