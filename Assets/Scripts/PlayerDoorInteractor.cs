using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>One aimed interaction command shared by the E key and editor playtesting.</summary>
public class PlayerDoorInteractor : MonoBehaviour
{
    public Camera viewCamera;
    public float interactionDistance = 3f;
    PlayerInventory inventory;
    InteractionPrompt prompt;
    readonly RaycastHit[] interactionHits = new RaycastHit[64];

    void Awake()
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null) inventory = gameObject.AddComponent<PlayerInventory>();
        prompt = GetComponent<InteractionPrompt>();
        if (prompt == null) prompt = gameObject.AddComponent<InteractionPrompt>();
    }
    bool CanUse()
    {
        return viewCamera != null && (SchoolPuzzleDirector.Instance == null || SchoolPuzzleDirector.Instance.CanInteract);
    }
    bool Aim(out RaycastHit hit)
    {
        // Looking steeply down at a nearby handle can intersect the top of our own capsule.
        // Keep the first actual world obstruction, while excluding the player's own body/rig.
        hit = default(RaycastHit);
        int count = Physics.RaycastNonAlloc(viewCamera.transform.position, viewCamera.transform.forward,
            interactionHits, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        RaycastHit[] candidates = interactionHits;
        if (count == interactionHits.Length)
        {
            candidates = Physics.RaycastAll(viewCamera.transform.position, viewCamera.transform.forward,
                interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            count = candidates.Length;
        }
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (candidates[i].transform.IsChildOf(transform) || candidates[i].distance >= nearest) continue;
            nearest = candidates[i].distance; hit = candidates[i];
        }
        return hit.collider != null;
    }
    void Update()
    {
        if (!CanUse()) { if (prompt != null) prompt.ClearPrompt(); return; }
        var hidingPlayer = GetComponent<PlayerHiding>();
        if (hidingPlayer != null && hidingPlayer.BlocksInput)
        {
            bool ready = hidingPlayer.Phase == HidingPhase.Hidden;
            prompt.SetPrompt(ready ? "ออกจากล็อกเกอร์" : "กำลังเข้าหรือออกจากตู้…", ready);
            if (ready && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteractFromView();
            return;
        }
        string text = null; bool available = true;
        if (Aim(out RaycastHit hit))
        {
            SchoolPuzzleObject puzzle = hit.collider.GetComponentInParent<SchoolPuzzleObject>();
            PickupItem pickup = hit.collider.GetComponentInParent<PickupItem>();
            LockedContainer container = hit.collider.GetComponentInParent<LockedContainer>();
            SchoolDoor door = hit.collider.GetComponentInParent<SchoolDoor>();
            var battery = hit.collider.GetComponentInParent<BatteryPickup>();
            var hiding = hit.collider.GetComponentInParent<HidingLocker>();
            if (battery != null) text = battery.Prompt(out available);
            else if (hiding != null) text = hiding.Prompt(hidingPlayer, out available);
            else if (puzzle != null) text = puzzle.Prompt;
            else if (pickup != null) text = pickup.PromptText;
            else if (container != null) text = container.GetPrompt(inventory, out available);
            else if (door != null)
            {
                DoorLock doorLock = door.GetComponent<DoorLock>();
                if (doorLock != null && doorLock.locked) { available = inventory.Has(doorLock.requiredItemId); text = available ? "ไขประตู" : "ประตูล็อกอยู่"; }
                else text = door.IsOpen ? "ปิดประตู" : "เปิดประตู";
            }
        }
        if (text == null) prompt.ClearPrompt(); else prompt.SetPrompt(text, available);
        if (text != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteractFromView();
    }
    public bool TryInteractFromView()
    {
        if (!CanUse()) return false;
        var hiding = GetComponent<PlayerHiding>();
        if (hiding != null && hiding.BlocksInput) return hiding.TryExit();
        if (!Aim(out RaycastHit hit)) return false;
        var battery = hit.collider.GetComponentInParent<BatteryPickup>();
        if (battery != null) return battery.TryPickup(GetComponent<FlashlightBattery>());
        var locker = hit.collider.GetComponentInParent<HidingLocker>();
        if (locker != null) return locker.TryEnter(hiding);
        var puzzle = hit.collider.GetComponentInParent<SchoolPuzzleObject>();
        if (puzzle != null) { if (!puzzle.CanInspect) return false; puzzle.Interact(); return true; }
        var pickup = hit.collider.GetComponentInParent<PickupItem>();
        if (pickup != null) { pickup.Pickup(inventory); return true; }
        var container = hit.collider.GetComponentInParent<LockedContainer>();
        if (container != null) return container.TryOpen(inventory);
        var door = hit.collider.GetComponentInParent<SchoolDoor>();
        if (door == null) return false;
        var doorLock = door.GetComponent<DoorLock>();
        if (doorLock != null && doorLock.locked) return doorLock.TryUnlock(inventory);
        door.Toggle(); return true;
    }
}
