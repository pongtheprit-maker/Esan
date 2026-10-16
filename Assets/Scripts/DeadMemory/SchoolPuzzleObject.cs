using UnityEngine;

public enum SchoolPuzzleKind { Note, UVClue, Pickup, Toolbox, Vent, FusePanel, SupplyLocker, RecordsDrawer, Clock, PhotoLocker, Photograph }

public class SchoolPuzzleObject : MonoBehaviour
{
    public string id;
    public SchoolPuzzleKind kind;
    public string title;
    [TextArea(3, 12)] public string text;
    public string itemId;
    public string itemName;
    public Transform doorLeaf;
    public Vector3 openEuler = new Vector3(0, -105, 0);
    public UVRevealable uv;
    public bool Open { get; private set; }
    Quaternion closed;

    void Awake() { if (doorLeaf != null) closed = doorLeaf.localRotation; }
    void Update()
    {
        if (doorLeaf != null) doorLeaf.localRotation = Quaternion.Slerp(doorLeaf.localRotation, closed * Quaternion.Euler(Open ? openEuler : Vector3.zero), 1f - Mathf.Exp(-5f * Time.deltaTime));
    }
    public void SetOpen(bool value) { Open = value; }
    public bool CanInspect { get { return kind != SchoolPuzzleKind.UVClue || (uv != null && uv.Amount > 0.5f && FlashlightController.Active != null && FlashlightController.Active.IsUVOn); } }
    public string Prompt
    {
        get
        {
            if (!CanInspect) return null;
            if (kind == SchoolPuzzleKind.Pickup) return "หยิบ " + itemName;
            if (kind == SchoolPuzzleKind.Note || kind == SchoolPuzzleKind.UVClue) return "อ่าน " + title;
            if (kind == SchoolPuzzleKind.Photograph) return "ตรวจรูปถ่าย";
            return "ตรวจ " + title;
        }
    }
    public void Interact()
    {
        if (CanInspect && SchoolPuzzleDirector.Instance != null) SchoolPuzzleDirector.Instance.Interact(this);
    }
}
