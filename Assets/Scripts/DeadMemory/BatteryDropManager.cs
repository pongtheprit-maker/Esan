using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BatteryDropManager : MonoBehaviour
{
    public Transform[] spawnPoints;
    public BatteryPickup batteryPrefab;
    public float waveInterval = 300;
    public int waveSize = 7;
    public static BatteryDropManager Instance { get; private set; }
    public float Remaining { get; private set; } = 300;
    public int Waves { get; private set; }
    public int TotalSpawned { get; private set; }
    public int Pending { get; private set; }
    public bool Initialized { get; private set; }
    public IEnumerable<BatteryPickup> Pickups { get { return active.Values; } }
    readonly Dictionary<int, BatteryPickup> active = new Dictionary<int, BatteryPickup>();
    float saveClock;
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public bool Contains(BatteryPickup pickup) { BatteryPickup found; return active.TryGetValue(pickup.slot, out found) && found == pickup; }
    void Update()
    {
        var d = SchoolPuzzleDirector.Instance;
        if (!Initialized || d == null || !d.Started || d.state.complete || SchoolPuzzleDirector.ModalOpen) return;
        Advance(Time.deltaTime);
        saveClock += Time.deltaTime;
        if (saveClock >= 30) { saveClock = 0; d.SaveCheckpoint(); }
    }
    public void Advance(float seconds)
    {
        if (!Initialized || seconds <= 0) return;
        Remaining -= seconds;
        while (Remaining <= 0) { Remaining += Mathf.Max(1, waveInterval); Waves++; Pending += waveSize; }
        int before = TotalSpawned; FillPending();
        if (TotalSpawned != before && SchoolPuzzleDirector.Instance != null) SchoolPuzzleDirector.Instance.SaveCheckpoint();
    }
    void Clear()
    {
        foreach (var p in active.Values) if (p != null) { p.gameObject.SetActive(false); Destroy(p.gameObject); }
        active.Clear(); saveClock = 0;
    }
    public void ResetForNewCase()
    {
        Clear(); Initialized = true; Remaining = waveInterval; Waves = 1; TotalSpawned = 0; Pending = waveSize;
        if (FlashlightBattery.Active != null) FlashlightBattery.Active.ResetPower(4);
        if (FlashlightController.Active != null) FlashlightController.Active.LowerForHiding();
        FillPending();
    }
    void Spawn(int slot)
    {
        var p = Instantiate(batteryPrefab, spawnPoints[slot].position, spawnPoints[slot].rotation, transform);
        p.name = "Flashlight cell — " + spawnPoints[slot].name; p.owner = this; p.slot = slot;
        p.gameObject.SetActive(true); active.Add(slot, p); TotalSpawned++;
    }
    void FillPending()
    {
        if (spawnPoints == null || batteryPrefab == null) return;
        while (Pending > 0 && active.Count < spawnPoints.Length)
        {
            bool upstairs = (TotalSpawned & 1) == 0;
            var free = Enumerable.Range(0, spawnPoints.Length).Where(i => !active.ContainsKey(i)).ToList();
            var preferred = free.Where(i => (spawnPoints[i].position.y > 3) == upstairs).ToList();
            if (preferred.Count > 0) free = preferred;
            var player = FirstPersonControllerObject();
            if (Waves > 1 && player != null)
            {
                var distant = free.Where(i => Vector3.Distance(spawnPoints[i].position, player.position) > 6).ToList();
                if (distant.Count > 0) free = distant;
            }
            Spawn(free[Random.Range(0, free.Count)]); Pending--;
        }
    }
    Transform FirstPersonControllerObject() { var d = SchoolPuzzleDirector.Instance; return d != null && d.player != null ? d.player.transform : null; }
    public void Collected(BatteryPickup p)
    {
        if (!Contains(p)) return;
        active.Remove(p.slot); Destroy(p.gameObject); FillPending();
        if (SchoolPuzzleDirector.Instance != null) SchoolPuzzleDirector.Instance.SaveCheckpoint();
    }
    public void Capture(SchoolPuzzleProgress state)
    {
        if (!Initialized) return;
        state.survivalInitialized = true; state.flashlightCells = FlashlightBattery.Active != null ? FlashlightBattery.Active.Cells : 4;
        state.batterySeconds = Remaining; state.batteryWaves = Waves; state.batteryTotal = TotalSpawned; state.batteryPending = Pending;
        state.batterySlots = active.Keys.OrderBy(i => i).ToList();
    }
    public void Restore(SchoolPuzzleProgress state)
    {
        if (!state.survivalInitialized) { ResetForNewCase(); return; }
        Clear(); Initialized = true; Remaining = Mathf.Clamp(state.batterySeconds, .001f, waveInterval);
        Waves = Mathf.Max(1, state.batteryWaves); Pending = Mathf.Max(0, state.batteryPending);
        if (FlashlightBattery.Active != null) FlashlightBattery.Active.ResetPower(state.flashlightCells);
        if (FlashlightController.Active != null) FlashlightController.Active.LowerForHiding();
        foreach (int i in state.batterySlots ?? new List<int>()) if (i >= 0 && i < spawnPoints.Length && !active.ContainsKey(i)) Spawn(i);
        TotalSpawned = Mathf.Max(active.Count, state.batteryTotal);
    }
}
