using System;
using UnityEngine;

/// <summary>Four rechargeable cells. A world pickup restores one cell, with no reserve inventory.</summary>
[DisallowMultipleComponent]
public class FlashlightBattery : MonoBehaviour
{
    public static FlashlightBattery Active { get; private set; }
    public const float Capacity = 4;
    [Min(1)] public float whiteSecondsPerCell = 90;
    [Min(1)] public float uvSecondsPerCell = 75;
    public Vector2 flickerInterval = new Vector2(65, 115);
    public float Cells { get; private set; } = Capacity;
    public bool HasPower { get { return Cells > .0001f; } }
    public bool IsFull { get { return Cells >= Capacity - .0001f; } }
    public float LightMultiplier { get; private set; } = 1;
    public int FlickerEvents { get; private set; }
    public bool IsFlickering { get { return flickerAge >= 0; } }
    System.Random random;
    float untilFlicker, flickerAge = -1;
    void Awake() { random = new System.Random(unchecked(Environment.TickCount + GetInstanceID())); ResetPower(Capacity); }
    void OnEnable() { Active = this; }
    void OnDisable() { if (Active == this) Active = null; }
    float NextInterval() { return Mathf.Lerp(Mathf.Max(10, flickerInterval.x), Mathf.Max(flickerInterval.x, flickerInterval.y), (float)random.NextDouble()); }
    public void ResetPower(float cells)
    {
        Cells = Mathf.Clamp(cells, 0, Capacity); flickerAge = -1; LightMultiplier = 1;
        if (random == null) random = new System.Random();
        untilFlicker = NextInterval(); FlickerEvents = 0;
    }
    // Called by the flashlight's normal Update, so both white and UV use the same timing path.
    public void TickPower(float seconds, bool lit, bool uv)
    {
        if (seconds <= 0) return;
        if (!lit || !HasPower) { flickerAge = -1; LightMultiplier = 1; return; }
        bool hadPower = HasPower;
        Cells = Mathf.Max(0, Cells - seconds / Mathf.Max(1, uv ? uvSecondsPerCell : whiteSecondsPerCell));
        if (hadPower && !HasPower)
        {
            flickerAge = -1; LightMultiplier = 0;
            if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.ShowMessage("ถ่านไฟฉายหมด — หาถ่านเพิ่ม", 4);
            return;
        }
        untilFlicker -= seconds;
        if (flickerAge < 0 && untilFlicker <= 0) { flickerAge = 0; FlickerEvents++; untilFlicker = NextInterval(); }
        if (flickerAge >= 0)
        {
            flickerAge += seconds;
            // Two short brownouts rather than a rapid strobe. Total event < 0.6 s.
            bool dip = flickerAge < .12f || (flickerAge > .29f && flickerAge < .42f);
            LightMultiplier = dip ? .13f : .86f;
            if (flickerAge >= .58f) { flickerAge = -1; LightMultiplier = 1; }
        }
        else LightMultiplier = 1;
    }
    public bool TryAddCell()
    {
        if (IsFull) return false;
        Cells = Mathf.Min(Capacity, Cells + 1);
        if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.ShowMessage("เติมถ่านไฟฉาย +1 ขีด", 2);
        return true;
    }
}
