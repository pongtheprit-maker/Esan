using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DeadMemorySurvivalBuilder
{
    const string Folder = "Assets/DeadMemory/Survival/";
    static Material painted, dark, rust, metal, cellBody, cellEnd;
    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool solid = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = pos; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!solid) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
    static Material Mat(string name, Color color, float smoothness, bool textured = false)
    {
        string path = Folder + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color; m.SetFloat("_Smoothness", smoothness);
        if (textured) { m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DeadMemory/HauntedArt/AbandonedPlaster.png"); m.mainTextureScale = new Vector2(.55f, 1); }
        EditorUtility.SetDirty(m); return m;
    }
    static Transform Point(Transform parent, string name, Vector3 local)
    { var p = new GameObject(name).transform; p.SetParent(parent, false); p.localPosition = local; return p; }
    static HidingLocker Locker(Transform root, string id, Vector3 position, float yaw)
    {
        // Replace only overlapping, non-interactive placeholder cabinets from the existing corridor bank.
        // Keep the originals inactive so the edit is reversible and later builder runs can restore them.
        foreach (var c in Physics.OverlapBox(position + Vector3.up * 1.14f, new Vector3(.65f, 1.12f, .60f), Quaternion.Euler(0, yaw, 0), ~0, QueryTriggerInteraction.Ignore))
            if (c.name == "Locker" && c.GetComponentInParent<SchoolPuzzleObject>() == null && c.GetComponentInParent<HidingLocker>() == null)
            { c.gameObject.name = "Locker (replaced by hiding cabinet)"; c.gameObject.SetActive(false); }
        var g = new GameObject("HideLocker_" + id); g.transform.SetParent(root); g.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        var locker = g.AddComponent<HidingLocker>(); locker.lockerId = id;
        locker.inside = Point(g.transform, "Inside standing position", new Vector3(0, .06f, -.025f));
        locker.approach = Point(g.transform, "Clear approach", new Vector3(0, .06f, 1.85f));
        Box(g.transform, "Left steel side", new Vector3(-.61f, 1.14f, 0), new Vector3(.055f, 2.28f, 1.15f), painted);
        Box(g.transform, "Right steel side", new Vector3(.61f, 1.14f, 0), new Vector3(.055f, 2.28f, 1.15f), painted);
        Box(g.transform, "Steel back", new Vector3(0, 1.14f, -.55f), new Vector3(1.22f, 2.28f, .05f), dark);
        Box(g.transform, "Cabinet base on floor", new Vector3(0, .02f, 0), new Vector3(1.22f, .04f, 1.15f), dark);
        Box(g.transform, "Cabinet roof", new Vector3(0, 2.26f, 0), new Vector3(1.22f, .04f, 1.15f), painted);
        locker.hinge = Point(g.transform, "Left hinge pivot", new Vector3(-.585f, 0, .565f));
        Box(locker.hinge, "Door lower steel", new Vector3(.585f, .755f, 0), new Vector3(1.17f, 1.43f, .04f), painted);
        Box(locker.hinge, "Door upper steel", new Vector3(.585f, 2.03f, 0), new Vector3(1.17f, .4f, .04f), painted);
        foreach (float x in new[] { .10f, 1.07f }) Box(locker.hinge, "Vent side frame", new Vector3(x, 1.65f, 0), new Vector3(.20f, .36f, .045f), painted);
        for (int i = 0; i < 5; i++) Box(locker.hinge, "Real ventilation slat", new Vector3(.585f, 1.475f + i * .08f, .008f), new Vector3(.77f, .026f, .055f), painted);
        Box(locker.hinge, "Handle bracket", new Vector3(1.04f, 1.16f, .036f), new Vector3(.07f, .19f, .04f), rust, false);
        Box(locker.hinge, "Handle", new Vector3(1.04f, 1.16f, .072f), new Vector3(.025f, .14f, .028f), metal, false);
        foreach (float y in new[] { .28f, 1.05f, 1.98f }) Box(g.transform, "Visible hinge", new Vector3(-.60f, y, .60f), new Vector3(.045f, .09f, .06f), rust, false);
        locker.doorColliders = locker.hinge.GetComponentsInChildren<Collider>();
        return locker;
    }
    static BatteryPickup Prefab()
    {
        var g = new GameObject("FlashlightBatteryCell"); var pickup = g.AddComponent<BatteryPickup>();
        var collider = g.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .045f, 0); collider.size = new Vector3(.20f, .105f, .15f);
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cylinder.name = "Single industrial cell";
        cylinder.transform.SetParent(g.transform, false); cylinder.transform.localPosition = new Vector3(0, .036f, 0);
        cylinder.transform.localRotation = Quaternion.Euler(0, 0, 90); cylinder.transform.localScale = new Vector3(.07f, .07f, .07f);
        cylinder.GetComponent<Renderer>().sharedMaterial = cellBody; UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        foreach (float x in new[] { -.068f, .068f })
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cap.name = "Metal battery end"; cap.transform.SetParent(g.transform, false);
            cap.transform.localPosition = new Vector3(x, .036f, 0); cap.transform.localRotation = Quaternion.Euler(0, 0, 90);
            cap.transform.localScale = new Vector3(.071f, .004f, .071f); cap.GetComponent<Renderer>().sharedMaterial = cellEnd;
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
        }
        Box(g.transform, "Battery positive mark", new Vector3(.035f, .072f, 0), new Vector3(.024f, .002f, .005f), cellEnd, false);
        Box(g.transform, "Battery positive mark", new Vector3(.035f, .072f, 0), new Vector3(.005f, .002f, .024f), cellEnd, false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(g, Folder + "FlashlightBatteryCell.prefab"); UnityEngine.Object.DestroyImmediate(g);
        return prefab.GetComponent<BatteryPickup>();
    }
    [MenuItem("DEAD MEMORY/Install Hiding and Flashlight Batteries")]
    public static void Menu() { Debug.Log(Apply()); }
    public static string Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        System.IO.Directory.CreateDirectory(Folder);
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t=>t.name=="Locker (replaced by hiding cabinet)"))
        { t.name="Locker";t.gameObject.SetActive(true); }
        var old = GameObject.Find("DeadMemory_Survival"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var root = new GameObject("DeadMemory_Survival").transform;
        painted = Mat("AgedLockerPaint", new Color(.15f, .245f, .225f), .2f, true);
        dark = Mat("LockerInterior", new Color(.07f, .095f, .085f), .1f, true);
        rust = Mat("RustFittings", new Color(.16f, .095f, .055f), .12f);
        metal = Mat("WornSteel", new Color(.29f, .32f, .29f), .5f);
        cellBody = Mat("BatteryCasing", new Color(.33f, .28f, .15f), .32f);
        cellEnd = Mat("BatteryContacts", new Color(.52f, .56f, .49f), .65f);
        Locker(root, "2F_StaffHall", new Vector3(-20.17f, 3.5f, 18.1f), 90);
        Locker(root, "2F_StorageHall", new Vector3(-17.57f, 3.5f, 26f), -90);
        Locker(root, "2F_RestroomHall", new Vector3(-20.17f, 3.5f, 35.7f), 90);
        Locker(root, "1F_FrontHall", new Vector3(-1.38f, 0, 9.1f), 90);
        Locker(root, "1F_MiddleHall", new Vector3(1.38f, 0, 19.1f), -90);
        Locker(root, "1F_RearHall", new Vector3(-1.38f, 0, 29.1f), 90);
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        if (player.GetComponent<PlayerHiding>() == null) player.gameObject.AddComponent<PlayerHiding>();
        if (player.GetComponent<FlashlightBattery>() == null) player.gameObject.AddComponent<FlashlightBattery>();
        if (player.GetComponent<BatteryHUD>() == null) player.gameObject.AddComponent<BatteryHUD>();
        var manager = root.gameObject.AddComponent<BatteryDropManager>(); manager.batteryPrefab = Prefab();
        var holder = Point(root, "Supported battery spawn points", Vector3.zero);
        var anchors = new List<Transform>(); var perRoom = new Dictionary<string, int>();
        var surfaces = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.transform.root != root && r.GetComponentInParent<SchoolPuzzleObject>() == null)
            .OrderBy(r => r.transform.position.x).ThenBy(r => r.transform.position.z).ToArray();
        foreach (var r in surfaces)
        {
            Bounds b = r.bounds; string n = r.name.ToLowerInvariant(); bool upper = b.center.y > 3;
            float floor = upper ? 3.5f : 0;
            bool inMap = upper ? b.center.x > -29 && b.center.x < -9 : b.center.x > -10 && b.center.x < 10;
            bool furnishing = (n.Contains("desk") && !n.Contains("chair") && !n.Contains("curve")) || n.Contains("table") || n.Contains("bench") || n == "storage shelf board" || n == "top";
            if (!inMap || !furnishing || b.size.y > .16f || b.size.x < .32f || b.size.z < .3f || b.max.y < floor + .42f || b.max.y > floor + 1.35f) continue;
            Vector3 pos = new Vector3(b.center.x, b.max.y + .003f, b.center.z);
            if (anchors.Any(a => Vector3.Distance(a.position, pos) < .8f)) continue;
            if (UnityEngine.Object.FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(p => Vector3.Distance(p.transform.position, pos) < .5f)) continue;
            string room = upper ? (b.center.x < -20.8f ? "2W" : b.center.x > -17 ? "2E" : "2H") + Mathf.FloorToInt(b.center.z / 10) : (b.center.x < -2 ? "1W" : b.center.x > 2 ? "1E" : "1H") + Mathf.FloorToInt(b.center.z / 10);
            int count; perRoom.TryGetValue(room, out count); if (count >= 4) continue;
            RaycastHit support;
            if (!Physics.Raycast(pos + Vector3.up * .08f, Vector3.down, out support, .15f, ~0, QueryTriggerInteraction.Ignore)) continue;
            var clutter = Physics.OverlapBox(pos + Vector3.up * .055f, new Vector3(.09f, .045f, .065f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            if (clutter.Any(c => c != support.collider)) continue;
            var anchor = Point(holder, (upper ? "2F_" : "1F_") + r.name + "_" + anchors.Count, Vector3.zero);
            anchor.position = pos; anchors.Add(anchor); perRoom[room] = count + 1;
        }
        manager.spawnPoints = anchors.ToArray();
        if (anchors.Count < 28 || anchors.Count(a => a.position.y > 3) < 7 || anchors.Count(a => a.position.y < 3) < 7)
            throw new InvalidOperationException("Insufficient supported battery anchors: " + anchors.Count);
        EditorUtility.SetDirty(player); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Saved 6 hide lockers (3 per floor), four-cell flashlight HUD and " + anchors.Count + " supported battery points; 7 cells immediately and every 300 active seconds.";
    }
}
