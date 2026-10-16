using UnityEngine;
using UnityEngine.InputSystem;

public enum HidingPhase { Outside, OpeningToEnter, Entering, ClosingToHide, Hidden, OpeningToExit, Exiting, Returning }

/// <summary>Moves the real capsule through an open hinged door. Never disables collision to enter or exit.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(FirstPersonController), typeof(CharacterController))]
public class PlayerHiding : MonoBehaviour
{
    public static PlayerHiding Active { get; private set; }
    public HidingPhase Phase { get; private set; }
    public HidingLocker CurrentLocker { get; private set; }
    public bool BlocksInput { get { return Phase != HidingPhase.Outside; } }
    public bool IsConcealed { get { return Phase == HidingPhase.Hidden && CurrentLocker != null && CurrentLocker.DoorClosed; } }
    public Vector3 SafeSavePosition { get { return BlocksInput ? entryPosition : transform.position; } }
    public float SafeSaveYaw { get { return BlocksInput ? entryYaw : transform.eulerAngles.y; } }
    FirstPersonController player;
    CharacterController capsule;
    Vector3 entryPosition, exitPosition;
    float entryYaw, phaseAge, stuckAge, lastDistance, lookYaw, lookPitch;
    void Awake() { player = GetComponent<FirstPersonController>(); capsule = GetComponent<CharacterController>(); }
    void OnEnable() { Active = this; }
    void OnDisable() { Release(true); if (Active == this) Active = null; }
    bool CanAct { get { return (SchoolPuzzleDirector.Instance == null || SchoolPuzzleDirector.Instance.CanInteract) && (SanitySystem.Instance == null || !SanitySystem.Instance.IsDead); } }
    public bool Enter(HidingLocker locker)
    {
        if (!CanAct || !locker.CanEnter(this) || !ClearAt(locker.InsidePosition, locker)) return false;
        CurrentLocker = locker; locker.Claim(this); entryPosition = transform.position; entryYaw = transform.eulerAngles.y;
        player.ResetMotion(); if (FlashlightController.Active != null) FlashlightController.Active.LowerForHiding();
        locker.SetDoor(true); Change(HidingPhase.OpeningToEnter); return true;
    }
    public bool TryExit()
    {
        if (!CanAct || Phase != HidingPhase.Hidden || CurrentLocker == null) return false;
        if (!FindExit(out exitPosition))
        { if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.ShowMessage("ทางออกถูกขวาง — ยังออกจากตู้ไม่ได้"); return false; }
        CurrentLocker.SetDoor(true); Change(HidingPhase.OpeningToExit); return true;
    }
    void Change(HidingPhase next) { Phase = next; phaseAge = stuckAge = 0; lastDistance = float.PositiveInfinity; }
    void Update()
    {
        if (!BlocksInput || CurrentLocker == null || SchoolPuzzleDirector.ModalOpen) return;
        phaseAge += Time.deltaTime;
        switch (Phase)
        {
            case HidingPhase.OpeningToEnter:
                if (CurrentLocker.DoorOpen) Change(HidingPhase.Entering); break;
            case HidingPhase.Entering:
                TurnToDoor();
                if (MoveTo(CurrentLocker.InsidePosition)) { CurrentLocker.SetDoor(false); Change(HidingPhase.ClosingToHide); }
                else if (stuckAge > 1.5f) { exitPosition = entryPosition; Change(HidingPhase.Exiting); }
                break;
            case HidingPhase.ClosingToHide:
                TurnToDoor();
                if (CurrentLocker.DoorClosed) { lookYaw = lookPitch = 0; player.SetViewDirection(CurrentLocker.transform.eulerAngles.y, 0); Change(HidingPhase.Hidden); }
                break;
            case HidingPhase.Hidden:
                var mouse = Mouse.current;
                Vector2 delta = mouse == null ? Vector2.zero : mouse.delta.ReadValue();
#if UNITY_EDITOR
                if (player.EditorTestInput) delta = Vector2.zero;
#endif
                lookYaw = Mathf.Clamp(lookYaw + delta.x * player.lookSensitivity, -32, 32);
                lookPitch = Mathf.Clamp(lookPitch - delta.y * player.lookSensitivity, -25, 25);
                player.SetViewDirection(CurrentLocker.transform.eulerAngles.y + lookYaw, lookPitch); break;
            case HidingPhase.OpeningToExit:
                if (CurrentLocker.DoorOpen) Change(HidingPhase.Exiting); break;
            case HidingPhase.Exiting:
                if (MoveTo(exitPosition)) { CurrentLocker.SetDoor(false); Release(false, true); }
                else if (stuckAge > 1.5f) Change(HidingPhase.Returning);
                break;
            case HidingPhase.Returning:
                if (MoveTo(CurrentLocker.InsidePosition)) { CurrentLocker.SetDoor(false); Change(HidingPhase.ClosingToHide); }
                else if (stuckAge > 1.5f) Release(true);
                break;
        }
    }
    void TurnToDoor() { player.SetViewDirection(Mathf.LerpAngle(transform.eulerAngles.y, CurrentLocker.transform.eulerAngles.y, 1 - Mathf.Exp(-5 * Time.deltaTime)), 0); }
    bool MoveTo(Vector3 point)
    {
        float distance = Vector3.Distance(transform.position, point);
        if (distance < .035f) return true;
        capsule.Move(Vector3.ClampMagnitude(point - transform.position, 2.25f * Time.deltaTime));
        float now = Vector3.Distance(transform.position, point);
        if (lastDistance - now < .001f) stuckAge += Time.deltaTime; else stuckAge = 0;
        lastDistance = now; return now < .035f;
    }
    void Ends(Vector3 pos, out Vector3 low, out Vector3 high, out float radius)
    {
        radius = capsule.radius + .015f;
        Vector3 centre = pos + capsule.center;
        low = centre + Vector3.down * (capsule.height * .5f - capsule.radius - .035f);
        high = centre + Vector3.up * (capsule.height * .5f - capsule.radius - .035f);
    }
    bool Ignore(Collider collider, HidingLocker locker)
    { return collider.transform.IsChildOf(transform) || collider.GetComponentInParent<HidingLocker>() == locker; }
    bool ClearAt(Vector3 pos, HidingLocker locker)
    {
        Vector3 low, high; float radius; Ends(pos, out low, out high, out radius);
        foreach (var c in Physics.OverlapCapsule(low, high, radius, ~0, QueryTriggerInteraction.Ignore)) if (!Ignore(c, locker)) return false;
        return Physics.Raycast(pos + Vector3.up * .20f, Vector3.down, .5f, ~0, QueryTriggerInteraction.Ignore);
    }
    bool FindExit(out Vector3 chosen)
    {
        var locker = CurrentLocker;
        var options = new[] { entryPosition, locker.ApproachPosition, locker.transform.TransformPoint(new Vector3(.42f, .06f, 1.9f)), locker.transform.TransformPoint(new Vector3(-.42f, .06f, 1.9f)) };
        foreach (var pos in options)
        {
            if (!ClearAt(pos, locker)) continue;
            Vector3 low, high; float radius; Ends(transform.position, out low, out high, out radius);
            Vector3 delta = pos - transform.position; bool clear = true;
            foreach (var h in Physics.CapsuleCastAll(low, high, radius, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignore(h.collider, locker)) { clear = false; break; }
            if (clear) { chosen = pos; return true; }
        }
        chosen = default(Vector3); return false;
    }
    void Release(bool open, bool animate = false)
    {
        if (CurrentLocker != null)
        {
            // Let the door close normally after the player is outside.
            if (animate) CurrentLocker.Claim(null); else CurrentLocker.Release(open);
        }
        CurrentLocker = null; Phase = HidingPhase.Outside;
        if (player != null) player.ResetMotion();
    }
    public void AbortForReset() { Release(false); }
}
