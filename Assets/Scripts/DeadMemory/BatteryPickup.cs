using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [HideInInspector] public BatteryDropManager owner;
    [HideInInspector] public int slot;
    public string Prompt(out bool available)
    {
        available = FlashlightBattery.Active != null && !FlashlightBattery.Active.IsFull;
        return available ? "เก็บถ่านไฟฉาย" : "ถ่านไฟฉายเต็มแล้ว";
    }
    public bool TryPickup(FlashlightBattery battery)
    {
        if (!gameObject.activeInHierarchy || owner == null || !owner.Contains(this) || battery == null) return false;
        var camera = battery.GetComponent<FirstPersonController>().playerCamera;
        if (Vector3.Distance(camera.transform.position, transform.position) > 3.15f) return false;
        if (!battery.TryAddCell())
        { if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.ShowMessage("ถ่านเต็มแล้ว เก็บเพิ่มไม่ได้"); return false; }
        gameObject.SetActive(false); owner.Collected(this); return true;
    }
}
