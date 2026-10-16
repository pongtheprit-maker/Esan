#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Editor-only real-time door collision checks; no puzzle grants or collision disabling.</summary>
public sealed class DeadMemoryDoorRealityQA : MonoBehaviour
{
    public string Status="Preparing", Failure="", Current="";
    public int Assertions;
    public float Seconds;
    public List<string> Steps=new List<string>();
    FirstPersonController player;
    float started;
    bool previousBackground, previousTestInput;
    GameObject blocker;
    public static DeadMemoryDoorRealityQA Launch()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play Mode required");
        return new GameObject("DoorReality_QA_TestOnly").AddComponent<DeadMemoryDoorRealityQA>();
    }
    IEnumerator Start()
    {
        yield return null;
        previousBackground=Application.runInBackground;Application.runInBackground=true;
        UnityEditor.EditorApplication.update-=UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        UnityEditor.EditorApplication.update+=UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        player=FindFirstObjectByType<FirstPersonController>();previousTestInput=player.EditorTestInput;player.EditorTestInput=true;player.EditorMove=Vector2.zero;
        started=Time.realtimeSinceStartup;Status="Running";
        yield return Guard(Sequence());
        if(Status!="FAILED")Finish("PASSED","");
    }
    void Update(){Seconds=Time.realtimeSinceStartup-started;}
    IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0&&Status!="FAILED")
        {
            bool more=false;object next=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
            catch(Exception e){Finish("FAILED",e.Message+" / "+Current);yield break;}
            if(!more){stack.Pop();continue;}var child=next as IEnumerator;if(child!=null)stack.Push(child);else yield return next;
        }
    }
    void Check(bool condition,string text){Assertions++;if(!condition)throw new Exception(text);Steps.Add("PASS | "+text);}
    IEnumerator Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null;}
    IEnumerator Sequence()
    {
        var doors=FindObjectsByType<SchoolDoor>(FindObjectsSortMode.None).Where(d=>d.enabled&&d.leaf!=null).OrderBy(d=>d.name).ToArray();
        Check(doors.Length>=22,"Expected restored usable school and stall door set");
        foreach(var door in doors)
        {
            Current=door.name;var collider=door.leaf.GetComponent<BoxCollider>();
            Check(collider!=null&&collider.enabled&&!collider.isTrigger,"Solid closed leaf: "+door.name);
            if(door.IsOpen){door.Toggle();yield return Wait(2.5f);}
            Check(Mathf.Abs(door.CurrentAngle)<1,"Closed seating angle: "+door.name);
            door.Toggle();yield return Wait(2.5f);
            Check(!collider.isTrigger&&collider.enabled,"Open door retains physical collision: "+door.name);
            Check(Mathf.Abs(door.CurrentAngle-door.openAngle)<1.5f,"Full unobstructed opening arc: "+door.name+" angle="+door.CurrentAngle.ToString("F2"));
            door.Toggle();yield return Wait(2.5f);
            Check(Mathf.Abs(door.CurrentAngle)<1&&!collider.isTrigger,"Returns solid to frame: "+door.name);
        }
        var sample=doors.First(d=>d.name=="SecondFloorDoor_Storage");Current="Capsule blocks Storage door";
        var leaf=sample.leaf;Vector3 offset=sample.transform.InverseTransformPoint(leaf.position);
        Quaternion turn=Quaternion.Euler(0,Mathf.Sign(sample.openAngle)*30,0);
        Vector3 position=sample.transform.position+sample.transform.rotation*(turn*offset);
        blocker=new GameObject("DoorReality_CapsuleBlocker_TestOnly");blocker.transform.position=position;
        var capsule=blocker.AddComponent<CapsuleCollider>();capsule.radius=.25f;capsule.height=1.72f;capsule.center=Vector3.zero;
        Physics.SyncTransforms();sample.Toggle();yield return Wait(2.5f);
        Check(sample.IsBlocked&&Mathf.Abs(sample.CurrentAngle)<30,"Door pauses against a player-sized solid capsule");
        Check(!leaf.GetComponent<BoxCollider>().isTrigger,"Blocked door never turns into a trigger");
        Destroy(blocker);blocker=null;yield return Wait(2.5f);
        Check(Mathf.Abs(sample.CurrentAngle-sample.openAngle)<1.5f,"Door resumes after obstruction clears");
        sample.Toggle();yield return Wait(2.5f);Check(Mathf.Abs(sample.CurrentAngle)<1,"Obstruction test door closes normally");
    }
    void Finish(string status,string failure)
    {
        Status=status;Failure=failure;Seconds=Time.realtimeSinceStartup-started;
        if(blocker!=null)Destroy(blocker);
        if(player!=null){player.EditorMove=Vector2.zero;player.EditorTestInput=previousTestInput;}
        UnityEditor.EditorApplication.update-=UnityEditor.EditorApplication.QueuePlayerLoopUpdate;Application.runInBackground=previousBackground;
        Directory.CreateDirectory("Assets/DeadMemory/RealityPass");
        File.WriteAllText("Assets/DeadMemory/RealityPass/Door_Playtest.txt",status+"\n"+Failure+"\nAssertions: "+Assertions+"\nReal seconds: "+Seconds.ToString("F1")+"\n"+string.Join("\n",Steps));
        Debug.Log("[DM Door Reality QA] "+status+" "+Failure);UnityEditor.EditorApplication.isPaused=true;
    }
    void OnDestroy()
    {
        if(blocker!=null)Destroy(blocker);
        UnityEditor.EditorApplication.update-=UnityEditor.EditorApplication.QueuePlayerLoopUpdate;
        if(player!=null){player.EditorMove=Vector2.zero;player.EditorTestInput=previousTestInput;}
        Application.runInBackground=previousBackground;
    }
}
#endif
