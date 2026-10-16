#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Editor-only end-to-end driver. Walks with the real controller, aims and presses E/Q.
/// UI button actions use the exact same public commands as OnGUI. It never grants items or sets puzzle flags.</summary>
[DefaultExecutionOrder(-10000)]
public class DeadMemoryPlaythrough : MonoBehaviour
{
    static bool booting, bootBackground;
    public static DeadMemoryPlaythrough Launch()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        bootBackground = Application.runInBackground; booting = true;
        Application.runInBackground = true;
        UnityEditor.EditorApplication.isPaused = false;
        UnityEditor.EditorApplication.update -= UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.update += UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        return new GameObject("DeadMemory_Playthrough_TestOnly").AddComponent<DeadMemoryPlaythrough>();
    }
    public string Status = "Preparing", Failure = "", CurrentGoal = "";
    public int Assertions;
    public float WalkedMetres, RealSeconds;
    public List<string> Steps = new List<string>();
    SchoolPuzzleDirector d;
    FirstPersonController p;
    Keyboard kb, previousKeyboard;
    InputSettings.BackgroundBehavior previousBackground;
    bool previousRunInBackground;
    float previousMaxDelta;
    public string CurrentWaypoint = "";
    float started;
    Vector3 lastPosition;
    const float Grid = .35f, MinX = -29f, MinZ = .4f;
    const int Width = 112, Height = 113;
    const int LayerSize = Width * Height;
    bool[] free;
    SchoolPuzzleObject[] nodes;
    public bool Finished { get { return Status == "PASSED" || Status == "FAILED"; } }

    IEnumerator Start()
    {
        yield return null; // Allow the scene's normal Start methods to initialize inventory and UI.
        d = SchoolPuzzleDirector.Instance; p = d.player;
        nodes = FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        previousKeyboard = Keyboard.current;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousRunInBackground = booting ? bootBackground : Application.runInBackground; booting = false;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Application.runInBackground = true;
        previousMaxDelta = Time.maximumDeltaTime; Time.maximumDeltaTime = .05f;
        p.EditorTestInput = true;
        kb = InputSystem.AddDevice<Keyboard>("DeadMemoryPlaytest"); kb.MakeCurrent();
        UnityEditor.EditorApplication.update -= UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.update += UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        started = Time.realtimeSinceStartup; lastPosition = p.transform.position;
        StartCoroutine(Guarded(Sequence()));
    }
    void Update()
    {
        if (p == null) return;
        if (kb != null && !Finished) kb.MakeCurrent();
        if (Finished) return;
        WalkedMetres += Vector3.Distance(p.transform.position, lastPosition); lastPosition = p.transform.position;
        RealSeconds = Time.realtimeSinceStartup - started;
        if (RealSeconds > 1200) Finish("FAILED", "Playthrough timed out.");
    }
    void LateUpdate() { if (kb != null && !Finished) kb.MakeCurrent(); }
    IEnumerator Guarded(IEnumerator sequence)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(sequence);
        while (stack.Count > 0 && !Finished)
        {
            object next = null; bool more = false;
            try { more = stack.Peek().MoveNext(); if (more) next = stack.Peek().Current; }
            catch (Exception ex) { Finish("FAILED", ex.Message + " at " + CurrentGoal); yield break; }
            if (!more) { stack.Pop(); continue; }
            var child = next as IEnumerator;
            if (child != null) stack.Push(child); else yield return next;
        }
        if (!Finished) Finish("PASSED", "");
    }
    void Check(bool condition, string label)
    {
        Assertions++;
        if (!condition) throw new Exception(label);
        Steps.Add("PASS | " + label);
    }
    void Finish(string status, string failure)
    {
        Status = status; Failure = failure; Neutral();
        Directory.CreateDirectory("Assets/DeadMemory/QA");
        string report = "DEAD MEMORY — Case 1 Play Mode test\n" + status + "\n" + failure + "\nReal seconds: " + RealSeconds.ToString("F1") + "\nWalked metres: " + WalkedMetres.ToString("F1") + "\nAssertions: " + Assertions + "\n" + string.Join("\n", Steps) + "\n\nGAME EVENT LOG\n" + string.Join("\n", d.actionLog);
        report += "\nPosition: " + p.transform.position + "\nWaypoint: " + CurrentWaypoint;
        File.WriteAllText("Assets/DeadMemory/QA/Playthrough_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt", report);
        Debug.Log("[DMTEST] " + status + " | " + failure + " | " + Assertions + " assertions | " + WalkedMetres.ToString("F0") + "m");
        RestoreInput();
        UnityEditor.EditorApplication.isPaused = true;
    }
    void RestoreInput()
    {
        UnityEditor.EditorApplication.update -= UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        if (kb != null) { InputSystem.RemoveDevice(kb); kb = null; }
        if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
        InputSystem.settings.backgroundBehavior = previousBackground;
        Application.runInBackground = previousRunInBackground;
        Time.maximumDeltaTime = previousMaxDelta;
        if (p != null) { p.EditorMove = Vector2.zero; p.EditorTestInput = false; }
    }
    void OnDestroy() { if (kb != null) { Neutral(); RestoreInput(); } }
    void Neutral() { if (p != null) p.EditorMove = Vector2.zero; if (kb != null) InputSystem.QueueStateEvent(kb, new KeyboardState()); }
    IEnumerator Pulse(Key key)
    {
        InputSystem.QueueStateEvent(kb, new KeyboardState(key));
        yield return null; yield return null;
        Neutral(); yield return null; yield return null;
    }
    IEnumerator Rest(float seconds)
    {
        Neutral(); float until = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < until) yield return null;
    }
    SchoolPuzzleObject Node(string id) { return nodes.First(x => x.id == id); }
    Vector3 Point(SchoolPuzzleObject n) { return n.GetComponent<Collider>().bounds.center; }
    void Aim(Vector3 point)
    {
        var v = point - p.playerCamera.transform.position;
        p.SetViewDirection(Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg, -Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg);
    }
    Vector3 Cell(int i) { int layer=i/LayerSize, local=i%LayerSize; return new Vector3(MinX + local % Width * Grid, .04f+layer*3.5f, MinZ + local / Width * Grid); }
    int Index(Vector3 v)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((v.x - MinX) / Grid), 0, Width - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt((v.z - MinZ) / Grid), 0, Height - 1);
        return z * Width + x + (v.y>2.5f?LayerSize:0);
    }
    void BuildGrid()
    {
        free = new bool[LayerSize * 2]; var buffer = new Collider[96];
        for (int i = 0; i < free.Length; i++)
        {
            var pos = Cell(i); bool clear = true;
            int count = Physics.OverlapCapsuleNonAlloc(pos + Vector3.up * .32f, pos + Vector3.up * 1.48f, .305f, buffer, ~0, QueryTriggerInteraction.Ignore);
            // Planning predicts an ordinary door can be opened. The walk driver still aims and uses E at each actual closed leaf.
            for (int j = 0; j < count; j++)
            {
                var door=buffer[j].GetComponentInParent<SchoolDoor>();
                if (!buffer[j].transform.IsChildOf(p.transform) && (door==null||door.IsOpen)) { clear = false; break; }
            }
            free[i] = clear && Physics.Raycast(pos + Vector3.up * .2f, Vector3.down, .4f, ~0, QueryTriggerInteraction.Ignore);
        }
    }
    bool VisibleFrom(Vector3 eye, SchoolPuzzleObject n, bool requireFront = false)
    {
        Vector3 point = Point(n), delta = point - eye;
        if (delta.magnitude > (requireFront ? 2.3f : 2.95f) || (requireFront && Vector3.Dot((eye - point).normalized, n.transform.forward) < .6f)) return false;
        var hits = Physics.RaycastAll(eye, delta.normalized, delta.magnitude + .1f, ~0, QueryTriggerInteraction.Collide).Where(x => !x.transform.IsChildOf(p.transform)).OrderBy(x => x.distance).ToArray();
        return hits.Length > 0 && hits[0].collider.GetComponentInParent<SchoolPuzzleObject>() == n;
    }
    List<Vector3> FindPath(SchoolPuzzleObject n)
    {
        BuildGrid(); int start = Index(p.transform.position);
        if (!free[start]) start = Enumerable.Range(0,free.Length).Where(i=>free[i]).OrderBy(i=>(Cell(i)-p.transform.position).sqrMagnitude).First();
        var prev = Enumerable.Repeat(-2,free.Length).ToArray(); var q = new Queue<int>(); prev[start]=-1;q.Enqueue(start);
        int goal=-1;
        while(q.Count>0)
        {
            int i=q.Dequeue();var pos=Cell(i);
            if ((pos-Point(n)).sqrMagnitude<10 && VisibleFrom(pos+Vector3.up*1.65f,n,true)){goal=i;break;}
            int local=i%LayerSize,layer=i/LayerSize,x=local%Width,z=local/Width;
            int bottom=Index(new Vector3(-19.4f,.04f,4.25f)),top=Index(new Vector3(-19.4f,3.54f,11.25f));
            int linked=i==bottom?top:i==top?bottom:-1;
            if(linked>=0&&free[linked]&&prev[linked]==-2){prev[linked]=i;q.Enqueue(linked);}
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dz==0)continue;int nx=x+dx,nz=z+dz;
                if(nx<0||nx>=Width||nz<0||nz>=Height)continue;
                int j=layer*LayerSize+nz*Width+nx;if(!free[j]||prev[j]!=-2)continue;
                if(dx!=0&&dz!=0&&(!free[layer*LayerSize+z*Width+nx]||!free[layer*LayerSize+nz*Width+x]))continue;
                prev[j]=i;q.Enqueue(j);
            }
        }
        if(goal<0)throw new Exception("No walkable, visible approach to "+n.id+" from "+p.transform.position);
        var path=new List<Vector3>();for(int i=goal;i>=0;i=prev[i])path.Add(Cell(i));path.Reverse();
        var expanded=new List<Vector3>();
        for(int k=0;k<path.Count;k++)
        {
            if(k>0&&Mathf.Abs(path[k].y-path[k-1].y)>2)
            {
                bool up=path[k].y>path[k-1].y;
                for(int s=0;s<=24;s++)
                {float z=up?4.25f+s*(7f/24f):11.25f-s*(7f/24f);expanded.Add(new Vector3(-19.4f,Mathf.Clamp01((z-4.7f)/5.6f)*3.5f+.04f,z));}
            }
            expanded.Add(path[k]);
        }
        return expanded;
    }
    IEnumerator SetLightMode(int mode)
    {
        // The initial sequence tests real Q key pulses. Use its public gameplay command
        // for repeated route setup to avoid synthetic input edges surviving editor ticks.
        var light=FlashlightController.Active;
        for(int i=0;i<3&&light.CurrentMode!=mode;i++)
        {
            if(!light.CycleLightMode())throw new Exception("Q gameplay command refused during route setup");
            yield return Rest(.15f);
        }
        Check(light.CurrentMode==mode,"Q gameplay mode selected for route: "+mode);
    }
    bool routeChanged;
    IEnumerator Go(string id)
    {
        CurrentGoal=id;Status="Walking to "+id;
        var n=Node(id);
        // Stow light between evidence checks; never grant battery power.
        if(!id.StartsWith("uv_")) yield return SetLightMode(0);
        bool arrived=false;
        for(int route=0;route<24;route++)
        {
            routeChanged=false;var path=FindPath(n);
            foreach(var target in path)
            {
                CurrentWaypoint=target.ToString();
                float began=Time.realtimeSinceStartup;
                while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(target.x,target.z))>.22f)
                {
                    if(Time.realtimeSinceStartup-began>6)throw new Exception("Movement blocked at "+p.transform.position+" toward "+target);
                    yield return OpenDoorAhead(target);
                    if(routeChanged)break;
                    Aim(new Vector3(target.x,p.playerCamera.transform.position.y,target.z));
                    p.EditorMove=Vector2.up;yield return null;
                }
                if(routeChanged)break;
            }
            if(!routeChanged){arrived=true;break;}
        }
        if(!arrived)throw new Exception("Too many door-route revisions for "+id);
        yield return Rest(.5f); Aim(Point(n));yield return Rest(.25f);Aim(Point(n));
        if(id.StartsWith("uv_")&&id!="uv_schedule")
        { yield return SetLightMode(2); yield return Rest(1.2f); }
        Check(VisibleFrom(p.playerCamera.transform.position,n),"Clear line of sight and reach: "+id);
    }
    IEnumerator OpenDoorAhead(Vector3 target)
    {
        Vector3 delta=new Vector3(target.x-p.transform.position.x,0,target.z-p.transform.position.z);
        if(delta.sqrMagnitude<.01f)yield break;
        var hits=Physics.SphereCastAll(p.transform.position+Vector3.up*1.05f,.22f,delta.normalized,Mathf.Min(1.2f,delta.magnitude+.18f),~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance);
        var hit=hits.FirstOrDefault(h=>h.collider.GetComponentInParent<SchoolDoor>()!=null&&!h.collider.GetComponentInParent<SchoolDoor>().IsOpen);
        if(hit.collider==null)yield break;
        var door=hit.collider.GetComponentInParent<SchoolDoor>();Aim(door.leaf.GetComponent<Collider>().bounds.center);
        if(p.GetComponent<PlayerDoorInteractor>().TryInteractFromView())
        {
            Steps.Add("OPEN DOOR via aimed E | "+door.name);yield return Rest(.5f);
            float began=Time.realtimeSinceStartup;
            while(Mathf.Abs(door.CurrentAngle-door.openAngle)>1.5f&&Time.realtimeSinceStartup-began<3)
            {
                if(door.IsBlocked){Aim(door.leaf.GetComponent<Collider>().bounds.center);p.EditorMove=Vector2.down;}
                else Neutral();yield return null;
            }
            Neutral();Physics.SyncTransforms();routeChanged=true;yield return Rest(.15f);
        }
    }
    IEnumerator Interact(string id)
    {
        yield return Go(id); Aim(Point(Node(id)));
        Check(p.GetComponent<PlayerDoorInteractor>().TryInteractFromView(), "Player E command accepted: " + id);
        yield return Rest(.35f);
    }
    IEnumerator Read(string id)
    {
        yield return Interact(id);Check(d.Modal=="note","Opened readable: "+id);d.CloseModal();yield return Rest(.3f);
    }
    IEnumerator Code(string digits)
    {
        Key[] keys={Key.Digit0,Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9};
        foreach(char c in digits) { d.PressDigit(c-'0'); yield return null; } d.SubmitCode();yield return Rest(.3f);
    }
    IEnumerator Sequence()
    {
        d.BeginNewCase(); yield return Rest(.5f); lastPosition=p.transform.position;
        Check(p.transform.position.y>3.4f,"New case spawns on second floor");
        Check(Node("photo_locker").transform.position.y>=3.5f,"Ending locker remains on second floor");
        Check(Node("fuse_panel").transform.position.x<-20.9f&&Node("fuse_panel").transform.position.z>20&&Node("fuse_panel").transform.position.z<28,"Electrical panel is in upstairs Storage");
        Check(nodes.Where(n=>n.kind==SchoolPuzzleKind.Note).All(n=>n.GetComponentsInChildren<TextMesh>(true).Length==0),"Notes have no world-space explanation text");
        var sanity=p.GetComponent<SanitySystem>();var atmosphere=FindFirstObjectByType<MemorySanityAtmosphere>();
        float recovery=sanity.recoveryPerSecond;sanity.recoveryPerSecond=0;int health=sanity.Health;
        sanity.SetSanity(55);yield return Rest(2.5f);float medium=atmosphere.Redness;
        Check(medium>.15f&&medium<.85f,"Medium sanity gradually changes atmosphere");
        sanity.SetSanity(10);yield return Rest(3.5f);
        Check(atmosphere.Redness>medium+.2f&&atmosphere.AmbientAudioLevel>.05f,"Low sanity deepens red world and strange ambience");
        Check(sanity.Health==health,"Atmosphere adds no jumpscare or health damage");
        sanity.SetSanity(100);yield return Rest(4f);sanity.recoveryPerSecond=recovery;
        Check(atmosphere.Redness<.01f&&atmosphere.AmbientAudioLevel<.01f,"Atmosphere restores smoothly when sanity returns");
        yield return Read("intro"); yield return Read("timetable");
        yield return Go("uv_schedule");
        yield return Pulse(Key.Q); yield return Rest(.7f);Aim(Point(Node("uv_schedule")));yield return Rest(.8f);
        Check(!FlashlightController.Active.IsUVOn && Node("uv_schedule").uv.Amount < .05f,"White flashlight does not reveal UV clue");
        yield return Pulse(Key.Q); yield return Rest(.7f);Aim(Point(Node("uv_schedule")));yield return Rest(1.2f);
        Check(FlashlightController.Active.IsUVOn && Node("uv_schedule").uv.HasBeenRevealed&&!d.state.notes.Contains("uv_schedule"),"UV reveals physical trace; explanation waits for E inspection");
        yield return Read("uv_schedule");
        yield return Interact("photo_locker");yield return Code("0315");Check(!d.state.locker,"Correct final code alone cannot bypass missing key");d.CloseModal();yield return Rest(.3f);
        yield return Interact("toolbox");yield return Code("000");Check(!d.state.toolbox,"Wrong toolbox code rejected");yield return Code("825");Check(d.state.toolbox && PlayerInventory.Instance.Has("dm_screwdriver"),"Toolbox opens with 825 and gives screwdriver");
        yield return Read("maintenance");yield return Read("floor_plan");
        yield return Interact("blue_fuse");Check(PlayerInventory.Instance.Has("dm_fuse_blue"),"Blue fuse picked up with real E interaction");
        yield return Go("uv_my_number");Aim(Point(Node("uv_my_number")));yield return Rest(1.2f);yield return Read("uv_my_number");
        yield return Interact("supply");Check(!d.state.supply,"Jammed locker rejects missing crowbar");
        yield return Interact("green_fuse");Check(PlayerInventory.Instance.Has("dm_fuse_green"),"Green fuse collected");
        yield return Interact("records");Check(!d.state.records,"Records drawer rejects missing staff key");
        yield return Go("uv_breakers");Aim(Point(Node("uv_breakers")));yield return Rest(1.2f);yield return Read("uv_breakers");
        yield return Interact("fuse_panel");Check(d.Modal=="breaker","Electrical panel UI opens");d.InstallFuses();
        Check(d.state.fuses.Count==2 && !d.PressBreaker(0) && !d.state.power,"Incomplete fuses cannot power panel");
        d.CloseModal();yield return Rest(.3f);
        yield return Interact("vent");Check(d.state.vent && PlayerInventory.Instance.Has("dm_fuse_red"),"Screwdriver opens vent and retrieves red fuse");
        yield return Interact("staff_key");Check(PlayerInventory.Instance.Has("dm_staff_key"),"Staff key collected");
        yield return Interact("fuse_panel");d.InstallFuses();Check(d.state.fuses.Count==3,"All three fuses installed without duplication");
        d.PressBreaker(0);d.PressBreaker(1);d.PressBreaker(2);Check(!d.state.power && d.state.fuses.Count==3,"Wrong switch order resets safely without consuming fuses");
        d.PressBreaker(0);d.PressBreaker(2);d.PressBreaker(1);yield return Rest(.4f);Check(d.state.power && PlayerInventory.Instance.Has("dm_crowbar"),"Left-right-middle restores power and releases crowbar");
        yield return Interact("supply");Check(d.state.supply && PlayerInventory.Instance.Has("dm_hour"),"Crowbar opens locker and yields hour hand");
        yield return Interact("records");Check(d.state.records && PlayerInventory.Instance.Has("dm_minute"),"Staff key opens drawer and yields minute hand");d.CloseModal();yield return Rest(.3f);
        // Continue is tested against the game's actual serialized checkpoint, without inventing state.
        string checkpoint=PlayerPrefs.GetString(d.SaveKey);int notes=d.state.notes.Count;
        d.BeginNewCase();Check(!d.state.power && !d.state.locker && PlayerInventory.Instance.Count==0,"New case resets progression and items");
        PlayerPrefs.SetString(d.SaveKey,checkpoint);d.ContinueCase();yield return Rest(.4f);lastPosition=p.transform.position;
        Check(d.state.power && d.state.notes.Count==notes && PlayerInventory.Instance.Has("dm_hour") && PlayerInventory.Instance.Has("dm_minute"),"Continue restores real checkpoint, notes, consumed fuses and inventory");
        yield return Read("clock_note");yield return Interact("clock");Check(d.Modal=="clock","Clock inspection opens");
        Check(!d.ConfirmClock() && !d.state.clock,"Clock cannot solve before hands installed");d.InstallClockParts();
        Check(d.state.hourInstalled&&d.state.minuteInstalled&&!PlayerInventory.Instance.Has("dm_hour"),"Both hands installed and consumed exactly once");
        Check(!d.ConfirmClock() && !d.state.clock,"Wrong clock time rejected");
        d.AdjustClock(1,0);d.AdjustClock(1,0);d.AdjustClock(1,0);d.AdjustClock(0,5);d.AdjustClock(0,5);d.AdjustClock(0,5);
        Check(d.ConfirmClock()&&d.state.clock&&PlayerInventory.Instance.Has("dm_photo_key"),"03:15 solves clock and releases photograph key");yield return Rest(.4f);
        yield return Interact("photo_locker");yield return Code("0000");Check(!d.state.locker&&PlayerInventory.Instance.Has("dm_photo_key"),"Wrong final code preserves key and remains recoverable");
        yield return Code("0315");Check(d.state.locker&&!PlayerInventory.Instance.Has("dm_photo_key"),"Final locker uses key and correct code");
        yield return Rest(1.1f);yield return Interact("photograph");Check(d.state.complete&&d.Modal=="ending","Photograph completes Case 1 through real raycast interaction");
        Check(d.state.notes.Contains("uv_my_number")&&d.state.notes.Contains("uv_breakers"),"Key UV evidence preserved through checkpoint");
        Check(p.transform.position.y>3.4f,"Case 1 ends upstairs through normal traversal");
        Status="Running";yield return Rest(2f);
    }
}
#endif
