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
public class DeadMemorySurvivalPlaytest : MonoBehaviour
{
    static bool booting, bootBackground;
    public static DeadMemorySurvivalPlaytest Launch()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        bootBackground = Application.runInBackground; booting = true;
        Application.runInBackground = true;
        UnityEditor.EditorApplication.isPaused = false;
        UnityEditor.EditorApplication.update -= UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.update += UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        return new GameObject("DeadMemory_Survival_TestOnly").AddComponent<DeadMemorySurvivalPlaytest>();
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
    public string ReportPath = "";
    HidingLocker[] lockers;
    public bool Finished { get { return Status == "PASSED" || Status == "FAILED"; } }

    IEnumerator Start()
    {
        yield return null; // Allow the scene's normal Start methods to initialize inventory and UI.
        d = SchoolPuzzleDirector.Instance; p = d.player;
        lockers = FindObjectsByType<HidingLocker>(FindObjectsSortMode.None).OrderBy(x=>x.lockerId).ToArray();
        UnityEditor.SessionState.SetBool("DMSurvival.HadSave",PlayerPrefs.HasKey(d.SaveKey));
        UnityEditor.SessionState.SetString("DMSurvival.OldSave",PlayerPrefs.GetString(d.SaveKey,""));
        previousKeyboard = Keyboard.current;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousRunInBackground = booting ? bootBackground : Application.runInBackground; booting = false;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Application.runInBackground = true;
        previousMaxDelta = Time.maximumDeltaTime; Time.maximumDeltaTime = .1f;
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
        string report = "DEAD MEMORY — Hiding and batteries Play Mode test\n" + status + "\n" + failure + "\nReal seconds: " + RealSeconds.ToString("F1") + "\nWalked metres: " + WalkedMetres.ToString("F1") + "\nAssertions: " + Assertions + "\n" + string.Join("\n", Steps) + "\n\nGAME EVENT LOG\n" + string.Join("\n", d.actionLog);
        report += "\nPosition: " + p.transform.position + "\nWaypoint: " + CurrentWaypoint;
        ReportPath="Assets/DeadMemory/QA/Survival_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        File.WriteAllText(ReportPath,report);
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
    class SurvivalNavTarget
    {
        public string id; public Transform transform; public Collider collider; public bool front, cabinet;
        public Vector3 Point { get { return cabinet ? transform.TransformPoint(new Vector3(.35f,1.15f,.59f)) : collider.bounds.center; } }
    }
    SurvivalNavTarget Node(string id)
    {
        var locker=lockers.FirstOrDefault(x=>x.lockerId==id);
        if(locker!=null) return new SurvivalNavTarget {id=id,transform=locker.transform,collider=locker.doorColliders[0],front=true,cabinet=true};
        if(id.StartsWith("battery:"))
        {int slot=int.Parse(id.Substring(8));var cell=BatteryDropManager.Instance.Pickups.First(x=>x.slot==slot);return new SurvivalNavTarget {id=id,transform=cell.transform,collider=cell.GetComponent<Collider>()};}
        var puzzle=FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.id==id);
        return new SurvivalNavTarget {id=id,transform=puzzle.transform,collider=puzzle.GetComponent<Collider>(),front=true};
    }
    Vector3 Point(SurvivalNavTarget n) { return n.Point; }
    void Aim(Vector3 point)
    {
        var v=point-p.playerCamera.transform.position;
        p.SetViewDirection(Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg,-Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg);
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
            for (int j = 0; j < count; j++) if (!buffer[j].transform.IsChildOf(p.transform) && buffer[j].GetComponentInParent<SchoolDoor>()==null) { clear = false; break; }
            free[i] = clear && Physics.Raycast(pos + Vector3.up * .2f, Vector3.down, .4f, ~0, QueryTriggerInteraction.Ignore);
        }
    }
    bool VisibleFrom(Vector3 eye, SurvivalNavTarget n, bool requireFront = false)
    {
        if(n.cabinet)
        {var local=n.transform.InverseTransformPoint(eye-Vector3.up*1.65f);if(Mathf.Abs(local.x)>.70f||local.z<1.10f)return false;}
        Vector3 point = Point(n), delta = point - eye;
        if (delta.magnitude > (requireFront ? 2.3f : 2.95f) || (requireFront && Vector3.Dot((eye - point).normalized, n.transform.forward) < .65f)) return false;
        var hits = Physics.RaycastAll(eye, delta.normalized, delta.magnitude + .1f, ~0, QueryTriggerInteraction.Collide).Where(x => !x.transform.IsChildOf(p.transform)).OrderBy(x => x.distance).ToArray();
        return hits.Length > 0 && hits[0].transform.IsChildOf(n.transform);
    }
    List<Vector3> FindPath(SurvivalNavTarget n)
    {
        BuildGrid(); int start = Index(p.transform.position);
        if (!free[start]) start = Enumerable.Range(0,free.Length).Where(i=>free[i]).OrderBy(i=>(Cell(i)-p.transform.position).sqrMagnitude).First();
        var prev = Enumerable.Repeat(-2,free.Length).ToArray(); var q = new Queue<int>(); prev[start]=-1;q.Enqueue(start);
        int goal=-1;
        while(q.Count>0)
        {
            int i=q.Dequeue();var pos=Cell(i);
            if ((pos-Point(n)).sqrMagnitude<10 && VisibleFrom(pos+Vector3.up*1.65f,n,n.front)){goal=i;break;}
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
    IEnumerator Go(string id)
    {
        CurrentGoal=id;Status="Walking to "+id;
        var n=Node(id);var path=FindPath(n);
        foreach(var target in path)
        {
            CurrentWaypoint=target.ToString();
            float began=Time.realtimeSinceStartup;
            while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(target.x,target.z))>.22f)
            {
                if(Time.realtimeSinceStartup-began>6)throw new Exception("Movement blocked at "+p.transform.position+" toward "+target);
                yield return OpenDoorAhead(target);
                Aim(new Vector3(target.x,p.playerCamera.transform.position.y,target.z));
                p.EditorMove=Vector2.up;yield return null;
            }
        }
        yield return Rest(.5f); Aim(Point(n));yield return Rest(.25f);Aim(Point(n));
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
        {Steps.Add("OPEN DOOR via aimed E | "+door.name);yield return Rest(.65f);}
    }

    IEnumerator Until(Func<bool> condition,float seconds,string label)
    {
        float began=Time.realtimeSinceStartup;
        while(!condition()) { if(Time.realtimeSinceStartup-began>seconds)throw new Exception("Timeout: "+label+" phase="+PlayerHiding.Active.Phase); yield return null; }
    }
    IEnumerator Hide(HidingLocker locker)
    {
        yield return Go(locker.lockerId); Aim(Point(Node(locker.lockerId))); yield return Pulse(Key.E);
        yield return Until(()=>PlayerHiding.Active.IsConcealed,8,"enter "+locker.lockerId);
        Check(PlayerHiding.Active.CurrentLocker==locker && locker.DoorClosed,"Real E enters and closes locker: "+locker.lockerId);
        Check(p.GetComponent<CharacterController>().enabled,"Capsule stays enabled while entering: "+locker.lockerId);
    }
    IEnumerator Leave(HidingLocker locker)
    {
        yield return Pulse(Key.E);yield return Until(()=>!PlayerHiding.Active.BlocksInput,8,"exit "+locker.lockerId);
        Check(!PlayerHiding.Active.IsConcealed && locker.Occupant==null,"Real E exits and restores control: "+locker.lockerId);
        Check(locker.transform.InverseTransformPoint(p.transform.position).z>1,"Exit remains outside cabinet: "+locker.lockerId);
        yield return Rest(.8f);
    }
    void ComponentChecks()
    {
        var b=FlashlightBattery.Active;
        Steps.Add("COMPONENT SIMULATION: controlled charge/time values; fresh case resets them before real traversal.");
        b.ResetPower(4);Check(!b.TryAddCell()&&b.Cells==4,"Full four-cell pack rejects extra battery");
        b.TickPower(90,true,false);Check(Mathf.Abs(b.Cells-3)<.001f,"90 white-light seconds consume one cell");
        b.TickPower(75,true,true);Check(Mathf.Abs(b.Cells-2)<.001f,"75 UV seconds consume one cell");
        b.TickPower(300,false,false);Check(Mathf.Abs(b.Cells-2)<.001f,"Lowered flashlight consumes no battery");
        b.ResetPower(.01f);b.TickPower(2,true,false);Check(!b.HasPower && b.Cells==0,"Charge depletion clamps exactly to zero");
        Check(b.TryAddCell()&&Mathf.Abs(b.Cells-1)<.001f,"One world battery restores one of four cells");
        b.ResetPower(4);float min=1;for(int i=0;i<12100;i++){b.TickPower(.01f,true,false);min=Mathf.Min(min,b.LightMultiplier);}
        Check(b.FlickerEvents==1&&min<.2f,"Rare flicker occurs once within first 121 lit seconds");
        b.TickPower(2,false,false);Check(!b.IsFlickering&&b.LightMultiplier==1,"Lowering flashlight clears temporary flicker");
        b.ResetPower(4);
    }
    IEnumerator Sequence()
    {
        ComponentChecks();d.BeginNewCase();yield return Rest(.6f);
        // Controlled depletion fixture, followed by another genuinely fresh case for the traversal below.
        FlashlightBattery.Active.ResetPower(.01f);yield return Pulse(Key.Q);yield return Rest(1.7f);
        Check(!FlashlightBattery.Active.HasPower&&!FlashlightController.Active.Beam.enabled,"Depleted white flashlight actually switches its beam off");
        yield return Pulse(Key.Q);yield return Rest(.4f);
        Check(!FlashlightController.Active.IsUVOn&&!FlashlightController.Active.Beam.enabled,"Empty charge cannot reveal UV evidence");
        ScreenCapture.CaptureScreenshot("Assets/Screenshots/Survival_EmptyBattery.png");yield return Rest(.2f);
        FlashlightBattery.Active.TryAddCell();yield return Rest(.4f);
        Check(FlashlightController.Active.Beam.enabled&&FlashlightController.Active.IsUVOn,"Restoring a cell powers the selected mode again");
        d.BeginNewCase();yield return Rest(.6f);lastPosition=p.transform.position;
        var manager=BatteryDropManager.Instance;var battery=FlashlightBattery.Active;var flash=FlashlightController.Active;
        var hiding=PlayerHiding.Active;
        Check(lockers.Length==6&&lockers.Count(x=>x.transform.position.y>3)==3,"Six hiding cabinets split evenly across both floors");
        Check(manager.Pickups.Count()==7&&manager.TotalSpawned==7,"New case drops exactly seven single batteries");
        Check(manager.Pickups.Any(x=>x.transform.position.y>3)&&manager.Pickups.Any(x=>x.transform.position.y<3),"Initial wave is distributed over both floors");
        Check(manager.spawnPoints.Length>=28,"Enough distinct supported spawn locations for repeated waves");
        Check(FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None).Count(x=>x.name.StartsWith("Charge cell "))==4,"Battery HUD has four charge segments");
        foreach(var cell in manager.Pickups)
            Check(Physics.Raycast(cell.transform.position+Vector3.up*.02f,Vector3.down,.1f,~0,QueryTriggerInteraction.Ignore),"Spawned battery has physical support: "+cell.slot);
        var target=manager.Pickups.Where(x=>x.transform.position.y>3).OrderBy(x=>(x.transform.position-p.transform.position).sqrMagnitude).First();
        int slot=target.slot;yield return Go("battery:"+slot);Aim(target.GetComponent<Collider>().bounds.center);
        yield return Pulse(Key.E);Check(target!=null&&target.gameObject.activeInHierarchy&&battery.IsFull&&manager.Pickups.Count()==7,"Aimed E cannot consume a pickup while full");
        yield return Pulse(Key.Q);yield return Rest(1);float before=battery.Cells;yield return Rest(1);
        float whiteUsed=before-battery.Cells;Check(flash.CurrentMode==1&&flash.Beam.enabled&&whiteUsed>.005f,"Real Q raises white beam and drains battery");
        Aim(target.GetComponent<Collider>().bounds.center);yield return Pulse(Key.E);
        Check(manager.Pickups.Count()==6&&battery.Cells>3.98f,"Real E collects one cell and caps charge at four");
        yield return Pulse(Key.E);Check(manager.Pickups.Count()==6,"Repeated E cannot collect the same battery twice");
        yield return Pulse(Key.Q);yield return Rest(.6f);before=battery.Cells;yield return Rest(1);
        Check(flash.CurrentMode==2&&flash.IsUVOn&&before-battery.Cells>whiteUsed*1.05f,"Real Q UV mode drains faster than white mode");
        yield return Pulse(Key.Q);yield return Rest(.7f);before=battery.Cells;yield return Rest(.7f);
        Check(flash.CurrentMode==0&&!flash.Beam.enabled&&Mathf.Abs(before-battery.Cells)<.0001f,"Real Q lowers beam and stops drain");
        // Actual read UI blocks flashlight input and pauses the five-minute gameplay timer.
        yield return Go("intro");yield return Pulse(Key.E);Check(d.Modal=="note","Existing E inspection still opens a readable note");
        float remaining=manager.Remaining;before=battery.Cells;yield return Pulse(Key.Q);yield return Rest(.7f);
        Check(Mathf.Abs(remaining-manager.Remaining)<.001f&&Mathf.Abs(before-battery.Cells)<.001f&&flash.CurrentMode==0,"Read UI pauses resource clocks and blocks Q");
        d.CloseModal();yield return Rest(.35f);
        foreach(var locker in lockers.OrderBy(x=>x.transform.position.y<3?1:0).ThenBy(x=>x.transform.position.y<3?-x.transform.position.z:x.transform.position.z))
        {
            yield return Hide(locker);
            Vector3 inside=p.transform.position;before=battery.Cells;p.EditorMove=Vector2.up;
            float moveUntil=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<moveUntil)yield return null;Neutral();
            Check(Vector3.Distance(inside,p.transform.position)<.04f,"Movement is locked inside: "+locker.lockerId);
            yield return Pulse(Key.Q);Check(flash.CurrentMode==0&&!flash.Beam.enabled,"Hidden player cannot reveal flashlight: "+locker.lockerId);
            Check(Mathf.Abs(before-battery.Cells)<.001f,"Hidden flashlight consumes no charge: "+locker.lockerId);
            if(locker.lockerId=="2F_StaffHall")
            {
                Directory.CreateDirectory("Assets/Screenshots");ScreenCapture.CaptureScreenshot("Assets/Screenshots/Survival_Hiding.png");yield return Rest(.4f);
                var eye=new GameObject("QA_Observer_NoEnemyAI");eye.transform.position=locker.ApproachPosition+Vector3.up*1.6f;eye.transform.LookAt(p.playerCamera.transform);
                Check(!GhostPerception.CanSeePlayer(eye.transform,p),"Shared ghost sight query rejects concealed player");
                var threat=eye.AddComponent<SanityThreat>();threat.enabled=false;
                Check(threat.GetFearAt(p.transform.position)==0,"Concealment suppresses existing proximity fear source");
                Destroy(threat);
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="QA_ExitBlocker";
                block.transform.SetPositionAndRotation(locker.transform.TransformPoint(new Vector3(0,1.15f,1.75f)),locker.transform.rotation);
                block.transform.localScale=new Vector3(2.2f,2.3f,1.5f);Physics.SyncTransforms();
                yield return Pulse(Key.E);Check(hiding.IsConcealed&&locker.DoorClosed,"Blocked exit rejects E without trapping player in an animation");
                Destroy(block);yield return Rest(.2f);
                yield return Leave(locker);
                eye.transform.position=p.transform.position+locker.transform.forward*1.8f+Vector3.up*1.6f;eye.transform.LookAt(p.playerCamera.transform);
                Check(GhostPerception.CanSeePlayer(eye.transform,p),"Shared sight query detects exposed player after exit");Destroy(eye);
                ScreenCapture.CaptureScreenshot("Assets/Screenshots/Survival_LockerHUD.png");yield return Rest(.3f);
            }
            else yield return Leave(locker);
        }
        // Wait for the real 300-second active-gameplay timer; do not accelerate the drop clock.
        Status="Waiting for actual five-minute wave";CurrentGoal="real 300 active seconds";
        if(flash.CurrentMode==0)yield return Pulse(Key.Q);
        yield return Until(()=>manager.Waves>=2,330,"real timed battery drop");yield return Rest(.3f);
        Check(manager.TotalSpawned==14&&manager.Pickups.Count()==13,"Second real timed wave drops seven more batteries; existing six remain");
        Check(manager.Pickups.Select(x=>x.slot).Distinct().Count()==13,"Timed wave never stacks batteries at occupied points");
        foreach(var cell in manager.Pickups)Check((cell.transform.position-manager.spawnPoints[cell.slot].position).sqrMagnitude<.0001f,"Battery remains on its assigned support: "+cell.slot);
        var last=lockers.First(x=>x.lockerId=="1F_FrontHall");yield return Hide(last);
        d.SaveCheckpoint();string checkpoint=PlayerPrefs.GetString(d.SaveKey);var saved=JsonUtility.FromJson<SchoolPuzzleProgress>(checkpoint);
        Check(Vector3.Distance(saved.position,hiding.SafeSavePosition)<.01f&&Vector3.Distance(saved.position,p.transform.position)>1,"Saving while hidden records a safe outside position");
        float savedCharge=battery.Cells;int savedCount=manager.Pickups.Count();float savedTimer=manager.Remaining;
        d.ContinueCase();yield return Rest(.4f);lastPosition=p.transform.position;
        Check(!hiding.BlocksInput&&last.Occupant==null&&p.GetComponent<CharacterController>().enabled,"Continue releases hiding occupancy and restores capsule control");
        Check(Mathf.Abs(battery.Cells-savedCharge)<.005f&&manager.Pickups.Count()==savedCount&&Mathf.Abs(manager.Remaining-savedTimer)<1,"Continue preserves charge, active pickups and drop countdown");
        d.BeginNewCase();yield return Rest(.3f);lastPosition=p.transform.position;
        Check(battery.IsFull&&manager.Pickups.Count()==7&&manager.Waves==1&&!hiding.BlocksInput,"New case resets charge, seven initial drops and hiding");
        PlayerPrefs.SetString(d.SaveKey,checkpoint);d.ContinueCase();yield return Rest(.35f);lastPosition=p.transform.position;
        Check(manager.Pickups.Count()==savedCount&&battery.Cells<=savedCharge+.005f,"Reloading a checkpoint does not farm extra batteries");
        Steps.Add("No chase AI is attached in this scene. Sight/concealment integration was checked with an observer fixture, not a live ghost chase.");
    }
}
#endif
