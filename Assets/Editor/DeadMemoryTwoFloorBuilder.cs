using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Repeatable two-floor layout and environment dressing pass, with supported physical props.</summary>
public static class DeadMemoryTwoFloorBuilder
{
    static Transform art;
    static Material plaster, floor, ceiling, wood, rust, paper, grime, concrete, dark;
    static System.Random random;
    static SchoolPuzzleObject[] nodes;
    static float R(float a, float b) { return a + (float)random.NextDouble() * (b - a); }
    static SchoolPuzzleObject N(string id) { return nodes.First(n => n.id == id); }
    [MenuItem("DEAD MEMORY/Build Two Floor Horror Case")]
    public static void BuildMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if (Application.isPlaying) return "Stop Play Mode first.";
        random = new System.Random(315825);
        var old = GameObject.Find("DeadMemory_Environment"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        DeadMemoryPuzzleBuilder.Build();
        SecondFloorFitout.StaffRooms(); SecondFloorFitout.Restrooms(); SecondFloorFitout.Doors();
        art = new GameObject("DeadMemory_Environment").transform;
        Directory.CreateDirectory("Assets/DeadMemory/Environment");
        plaster = Surface("PeelingPlaster", new Color(.48f,.46f,.35f), new Color(.15f,.19f,.13f), 0);
        floor = Surface("WornFloor", new Color(.29f,.28f,.23f), new Color(.11f,.13f,.12f), 1);
        ceiling = Surface("DampCeiling", new Color(.39f,.39f,.32f), new Color(.16f,.18f,.12f), 0);
        wood = Surface("WornWood", new Color(.29f,.20f,.12f), new Color(.1f,.075f,.045f), 2);
        rust = Surface("CorrodedMetal", new Color(.23f,.28f,.26f), new Color(.29f,.13f,.065f), 3);
        paper = Surface("OldPaper", new Color(.56f,.51f,.36f), new Color(.29f,.25f,.15f), 4);
        grime = Simple("DampMould", new Color(.075f,.095f,.066f), .02f);
        concrete = Simple("ExposedAggregate", new Color(.24f,.23f,.19f), .01f);
        grime.SetFloat("_Cull",0); concrete.SetFloat("_Cull",0);
        dark = Simple("DarkRubber", new Color(.028f,.032f,.025f), .12f);
        AssetDatabase.StartAssetEditing();
        try { AgeExistingSurfaces(); } finally { AssetDatabase.StopAssetEditing(); }
        nodes = UnityEngine.Object.FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var d = UnityEngine.Object.FindFirstObjectByType<SchoolPuzzleDirector>();
        d.spawnPosition = new Vector3(-24.7f,3.54f,3.5f); d.spawnYaw = 345;
        Move("intro",new Vector3(-28.73f,5.02f,3.3f),90,"สมุดที่ติดอยู่บนผนัง / ห้อง 2A");
        Move("timetable",new Vector3(-28.73f,5.1f,5f),90,"ตารางเรียนเก่า / ห้อง 2A");
        Move("uv_schedule",new Vector3(-24.87f,5.2f,11.73f),180,"รอยหมึกบนกระดาน / ห้อง 2A");
        Move("photo_locker",new Vector3(-28.25f,3.5f,1.5f),90,"ตู้ของที่ระลึก / ห้อง 2A");
        Move("toolbox",new Vector3(-25.5f,3.5f,27.5f),180,"ตู้เครื่องมือ / Storage ชั้น 2");
        Move("maintenance",new Vector3(-21.02f,5.05f,25.7f),270,"ใบแจ้งซ่อม / Storage ชั้น 2");
        Move("records",new Vector3(-21.42f,3.5f,18.9f),270,"ตู้แฟ้มนักเรียน / ห้องครูฝั่งตะวันตก ชั้น 2");
        Move("uv_breakers",new Vector3(-21.02f,5.15f,13.1f),270,"ผังวงจรที่ถูกลบ / ห้องครูฝั่งตะวันตก ชั้น 2");
        Move("uv_my_number",new Vector3(-12.87f,5.2f,11.73f),180,"รอยหมึกบนกระดาน / ห้อง 2B");
        Move("fuse_panel",new Vector3(-24f,4.05f,20.27f),0,"ตู้ไฟสำรอง / Storage ชั้น 2");
        Move("power_note",new Vector3(-26.3f,5.1f,20.13f),0,"ป้ายซ่อมบำรุง / Storage ชั้น 2");
        Move("clock",new Vector3(-9.02f,5.3f,19f),270,"นาฬิกาประจำห้องครู / ชั้น 2");
        Move("clock_note",new Vector3(-9.01f,5.05f,20.6f),270,"บันทึกเรื่องนาฬิกา / ห้องครู ชั้น 2");
        Move("floor_plan",new Vector3(-20.72f,5.05f,12.9f),90,"ผังอาคารเรียน");
        N("intro").text="ห้อง 2A… ฉันเคยนั่งเรียนที่นี่\nตู้เก็บของหลังห้องยังล็อกอยู่ รูปของเราน่าจะยังอยู่ข้างใน\nเพื่อนชอบเขียนด้วยหมึกที่มองเห็นได้ใต้แสงสีม่วง\nบางทีบนกระดานอาจยังเหลือร่องรอย\n\n[Q] วนไฟขาว / UV / ปิด   [E] อ่าน   [Tab] บันทึก";
        N("timetable").text="ตารางเรียน ห้อง 2A\nเข้าเรียน 08:00\nพักกลางวัน 12:00\nเลิกเรียน 15:00\n\nหมายเหตุแม่บ้าน: ย้ายเครื่องมือขึ้น Storage ชั้น 2";
        N("uv_schedule").text="เขาลืมรหัสตู้เครื่องมือเสมอ\nฉันเลยบอกให้ดูเวลาบนตารางเรียน\nเอาเพียงหลักหน่วยของชั่วโมง\nเข้าเรียน → พักกลางวัน → เลิกเรียน";
        N("maintenance").text="ใบงานซ่อมบำรุง\nย้ายตู้ไฟและเครื่องมือขึ้น Storage ชั้น 2\nฟิวส์แดง: หลังตะแกรงห้อง 103 ชั้น 1 ใช้ไขควง\nฟิวส์น้ำเงิน: ชั้นอุปกรณ์ห้อง 104 ชั้น 1\nฟิวส์เขียว: ห้องเก็บอุปกรณ์ 106 ชั้น 1\n\nผังวงจรฉบับสำรองอยู่ห้องครูฝั่งตะวันตก ชั้น 2\nช่องเก็บชะแลงปลดด้วยไฟจากวงจรนี้";
        N("uv_bag").text="ฝากกุญแจแฟ้มนักเรียนไว้ตรงนี้\nเอากลับให้ครูที่ห้องพักครูฝั่งตะวันตก ชั้น 2 ด้วย";
        N("uv_breakers").text="สายที่ 1 — ซ้าย\nสายที่ 2 — ขวา\nสายที่ 3 — กลาง\n\nติดตั้งฟิวส์ครบก่อนยกสวิตช์\nถ้าผิดให้เริ่มลำดับใหม่ ฟิวส์ยังใช้ต่อได้";
        N("power_note").text="วงจรสำรองอาคารเรียน\nติดตั้งฟิวส์ทั้งสามสีก่อนจ่ายไฟ\nดูผังสายที่ห้องครูฝั่งตะวันตก ชั้น 2\nช่องเก็บชะแลงข้างแผงจะปลดเมื่อวงจรทำงาน\nอย่าวางสิ่งของขวางหน้าตู้ไฟ";
        N("clock_note").text="นาฬิกาของห้องพักครูหยุดเดินอีกแล้ว\nเข็มชั่วโมง: เก็บในตู้ที่ติดแน่น ห้องอุปกรณ์ 106 ชั้น 1\nเข็มนาที: เก็บรวมกับแฟ้มนักเรียน ห้องครูฝั่งตะวันตก ชั้น 2\n\nมีรอยเขียนหลังหน้าปัดว่า ‘เวลาของเราสองคน’\nลองตามหาเลขที่ของเราอีกครั้ง";
        N("floor_plan").text="ชั้น 2\nด้านหน้า: ห้องเรียน 2A / 2B\nตอนกลาง: ห้องครูตะวันตก / ตะวันออก\nต่อไปทางตะวันตก: Storage\nด้านหลัง: ห้องน้ำหญิง / ชาย\n\nบันไดอยู่ด้านหน้าอาคาร\nลงชั้น 1 แล้วไปทางตะวันออก ผ่านทางเชื่อมเข้าห้อง 101\nชั้น 1: ห้องเรียน 101–105, ห้องอุปกรณ์ 106, ห้องเรียน 107–108\n\nเอกสารต้องเล็งแล้วกด E; ร่องรอยหมึกต้องใช้ UV ก่อนตรวจ";
        HideExplanatoryLabels();
        LinkStairAndGround();
        RepairClassroomDoorways();
        FitStorage();
        d.powerRewardModel=art.Find("Crowbar").gameObject;
        DressRooms();
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if(l.GetComponentInParent<FlashlightController>()!=null)continue;
            if(l.name.StartsWith("SecondFloor")) { l.intensity=5; l.range=9; l.color=new Color(.78f,.84f,.65f); }
        }
        Lamp("UpperHall_Back",new Vector3(-18.9f,6.15f,30.5f),3.5f,8,new Color(.68f,.76f,.64f));
        Lamp("Storage_worklight",new Vector3(-25.1f,6.1f,24),4.5f,7,new Color(.9f,.78f,.54f));
        Lamp("StaffEast_worklight",new Vector3(-12.7f,6.1f,18.5f),4,9,new Color(.78f,.84f,.68f));
        Lamp("Restroom_Women",new Vector3(-24.9f,6.1f,34),3.5f,8,new Color(.65f,.77f,.69f));
        Lamp("Classroom_2B",new Vector3(-12.8f,6.1f,6.8f),4.8f,9,new Color(.72f,.81f,.68f));
        Lamp("Stairwell",new Vector3(-18.3f,5.9f,7.1f),3.5f,9,new Color(.87f,.76f,.55f));
        Lamp("Ground_connection",new Vector3(-16f,2.8f,3.5f),3.5f,9,new Color(.84f,.72f,.5f));
        RenderSettings.ambientLight=new Color(.18f,.19f,.155f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Exponential; RenderSettings.fogDensity=.007f; RenderSettings.fogColor=new Color(.12f,.135f,.11f);
        var sanity=d.player.GetComponent<SanitySystem>(); sanity.distortedSaturation=-12; sanity.distortedContrast=12;
        var atmosphere=art.gameObject.AddComponent<MemorySanityAtmosphere>(); atmosphere.sanity=sanity;
        var cc=d.player.GetComponent<CharacterController>(); cc.enabled=false; d.player.transform.SetPositionAndRotation(d.spawnPosition,Quaternion.Euler(0,d.spawnYaw,0));cc.enabled=true;
        DeadMemoryEnvironmentPolish.Apply();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Two-floor case saved. Spawn/end: 2A; panel/tools: Storage; staff records and clock: staff rooms; fuse exploration: floor 1. Environment objects="+art.GetComponentsInChildren<Renderer>().Length;
    }
    static void Move(string id,Vector3 p,float yaw,string title)
    { var n=N(id);n.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));n.title=title; }
    static void HideExplanatoryLabels()
    {
        foreach(var n in nodes)
        {
            foreach(var tm in n.GetComponentsInChildren<TextMesh>(true))
            {
                int clockDigit;
                if(n.kind==SchoolPuzzleKind.Photograph||(n.kind==SchoolPuzzleKind.Clock&&int.TryParse(tm.text,out clockDigit)))continue;
                if(n.kind==SchoolPuzzleKind.UVClue)
                {tm.text=n.id=="uv_my_number"?"03  /  __":n.id=="uv_breakers"?"I   →   III   →   II":n.id=="uv_schedule"?"08   12   15\n_8   _2   _5":"...";tm.characterSize=.018f;}
                else UnityEngine.Object.DestroyImmediate(tm.gameObject);
            }
            if(n.kind==SchoolPuzzleKind.Note)
            {
                var p=n.transform.Find("Paper"); if(p!=null)p.localScale=new Vector3(.52f,.36f,.018f);
                n.GetComponent<BoxCollider>().size=new Vector3(.56f,.4f,.04f);
                // A small pin and print lines communicate a real note without displaying the explanation.
                for(int i=0;i<6;i++)Box(n.transform,"Faded ink",new Vector3(-.025f,.1f-i*.032f,.013f),new Vector3(.32f-i%3*.04f,.004f,.002f),dark,false);
                Box(n.transform,"Drawing pin",new Vector3(0,.16f,.018f),new Vector3(.025f,.025f,.016f),rust,false);
            }
        }
        var puzzle=GameObject.Find("DeadMemory_Case1_Puzzle").transform;
        foreach(Transform t in puzzle.Cast<Transform>().ToArray()) if(t.name=="RoomSign")UnityEngine.Object.DestroyImmediate(t.gameObject);
        for(int i=0;i<4;i++)
        {Sign(new Vector3(-1.88f,2.55f,5+i*10),90,(101+i*2).ToString());Sign(new Vector3(1.88f,2.55f,5+i*10),270,(102+i*2).ToString());}
    }
    static void AgeExistingSurfaces()
    {
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(r.sharedMaterial==null||r.GetComponent<TextMesh>()!=null||r.GetComponentInParent<FlashlightController>()!=null)continue;
            string m=r.sharedMaterial.name,n=r.name;
            Material use=null;
            if(m=="School_Wall")use=plaster;
            else if(m=="School_Floor"||m=="FO_TileFloor")use=floor;
            else if(m=="School_Ceiling")use=ceiling;
            else if(m=="School_Wood"||m=="FO_Panel"||m=="FO_Desk"||m=="FO_Shelf")use=wood;
            else if(m=="School_Metal"||m=="School_LockerA"||m=="School_LockerB"||m=="FO_Gray"||m=="OldSteel")use=rust;
            if(use==null)continue;
            var material=new Material(use); material.name=use.name+"_"+r.GetInstanceID();
            // Per-object tiling follows actual dimensions rather than stretching one stain across a whole room.
            Vector3 s=r.bounds.size; float u=Mathf.Max(s.x,s.z),v=s.y>.35f?s.y:Mathf.Min(s.x,s.z);
            material.mainTextureScale=new Vector2(Mathf.Max(1,u/2.5f),Mathf.Max(1,v/2.5f));
            material.mainTextureOffset=new Vector2(R(0,1),R(0,1));
            string path="Assets/DeadMemory/Environment/Instance_"+r.GetInstanceID()+".mat";
            AssetDatabase.CreateAsset(material,path);r.sharedMaterial=material;
        }
    }
    static void LinkStairAndGround()
    {
        // The existing near landing ended at an open stair void. Add a real side passage and guardrail.
        Box(art,"Upper stair side passage",new Vector3(-17.38f,3.4f,7.4f),new Vector3(1.15f,.2f,6.2f),floor,true);
        Box(art,"Stair void handrail",new Vector3(-17.96f,4.55f,7.4f),new Vector3(.055f,.07f,6.1f),rust,true);
        for(float z=4.5f;z<10.5f;z+=.55f)Box(art,"Stair void baluster",new Vector3(-17.96f,4.02f,z),new Vector3(.035f,1.04f,.035f),rust,true);
        // Keep the landing edge closed against accidental falls; stair entry is at its upper end.
        Box(art,"Near landing guard",new Vector3(-19.15f,4.05f,4.48f),new Vector3(2.45f,1.1f,.07f),rust,true);
        Box(art,"Ground connector threshold",new Vector3(-10,.015f,3.5f),new Vector3(1.8f,.03f,1.4f),floor,true);
        // Test actual boundary geometry. If a wall spans the connector, split it around a proper door opening.
        Physics.SyncTransforms();
        foreach(var c in Physics.OverlapBox(new Vector3(-10,1.1f,3.5f),new Vector3(.16f,1.02f,.62f)))
        {
            if(c.GetComponentInParent<SchoolPuzzleObject>()!=null||!c.name.Contains("Wall"))continue;
            Bounds b=c.bounds; if(b.size.z<1.3f||b.size.x>.5f)continue;
            c.gameObject.SetActive(false);
            float a=2.82f,zz=4.18f;
            if(a>b.min.z)Box(art,"Connector wall south",new Vector3(b.center.x,b.center.y,(b.min.z+a)/2),new Vector3(b.size.x,b.size.y,a-b.min.z),plaster,true);
            if(b.max.z>zz)Box(art,"Connector wall north",new Vector3(b.center.x,b.center.y,(b.max.z+zz)/2),new Vector3(b.size.x,b.size.y,b.max.z-zz),plaster,true);
            Box(art,"Connector lintel",new Vector3(b.center.x,2.67f,3.5f),new Vector3(b.size.x,1.06f,1.36f),plaster,true);
        }
        foreach(float z in new[]{2.78f,4.22f})Box(art,"Connector jamb",new Vector3(-10,1.1f,z),new Vector3(.2f,2.2f,.08f),wood,true);
        Box(art,"Connector header",new Vector3(-10,2.22f,3.5f),new Vector3(.2f,.12f,1.52f),wood,true);
        Sign(new Vector3(-10.16f,2.53f,3.5f),270,"อาคารเรียน • ชั้น 1");
        Sign(new Vector3(-20.71f,5.9f,3.5f),90,"2A");Sign(new Vector3(-17.02f,5.9f,3.5f),270,"2B");
    }
    static void FitStorage()
    {
        // Replace solid placeholder shelf blocks with open shelves and visibly supported supplies.
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("SecondFloor_Storage")).ToArray())
        {if(t.name.Contains("Tube"))continue;UnityEngine.Object.DestroyImmediate(t.gameObject);}
        Shelf(new Vector3(-28.35f,3.5f,22.2f));Shelf(new Vector3(-28.35f,3.5f,25.7f));
        Box(art,"Storage carton floor",new Vector3(-27.25f,3.82f,26.5f),new Vector3(.62f,.64f,.62f),paper,true);
        // The wall mounted panel has a back plate and visible fixings; a dry, clear maintenance bay.
        Box(art,"Panel wall backplate",new Vector3(-24f,4.96f,20.18f),new Vector3(1.05f,1.95f,.08f),rust,false);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)Box(art,"Panel screw",new Vector3(-24+x*.46f,4.96f+y*.85f,20.24f),Vector3.one*.04f,dark,false);
        Box(art,"Electrical conduit",new Vector3(-24f,6.03f,20.15f),new Vector3(.06f,1.2f,.06f),rust,false);
        Box(art,"Locked crowbar channel",new Vector3(-23.25f,4.8f,20.22f),new Vector3(.28f,1.3f,.17f),rust,false);
        Box(art,"Crowbar",new Vector3(-23.25f,4.85f,20.34f),new Vector3(.035f,.95f,.035f),dark,false);
        // Functional surface for maintenance equipment, feet rest on the actual floor.
        Table(new Vector3(-21.65f,3.5f,27f),.95f,.7f);
    }
    static void RepairClassroomDoorways()
    {
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.Contains("Classroom_2")&&r.name.Contains("HallWall")))
        {
            Bounds b=r.bounds;float min=b.min.z,max=b.max.z;
            if(r.name.Contains("Front"))max=2.69f;else min=4.31f;
            r.transform.position=new Vector3(b.center.x,b.center.y,(min+max)/2);
            r.transform.localScale=new Vector3(r.transform.localScale.x,r.transform.localScale.y,max-min);
        }
    }
    static void DressRooms()
    {
        // School furniture placeholders had chair seats and backs without supports.
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.Contains("ChairSeat")).ToArray())
        {
            Bounds b=r.bounds;float fy=b.center.y>3?3.5f:0;float bottom=b.min.y;
            if(bottom-fy<.15f||bottom-fy>1)continue;
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.16f,.16f})
                Box(art,"Chair support",new Vector3(b.center.x+x,fy+(bottom-fy)/2,b.center.z+z),new Vector3(.035f,bottom-fy,.035f),rust,false);
        }
        // Distinct clutter clusters leave central circulation and every door unobstructed.
        foreach(var data in new[]{new Vector3(-27.7f,3.5f,8.1f),new Vector3(-10.1f,3.5f,8.7f),new Vector3(-27.8f,3.5f,16.4f),new Vector3(-9.9f,3.5f,21f),new Vector3(-22.2f,3.5f,36.5f),new Vector3(-15.7f,3.5f,34.8f),new Vector3(-8.8f,0,6.8f),new Vector3(8.5f,0,17.2f),new Vector3(-8.8f,0,26.7f),new Vector3(8.5f,0,35.8f)})
        {
            for(int i=0;i<14;i++)
            {
                Vector3 p=data+new Vector3(R(-.6f,.6f),.004f,R(-.7f,.7f));
                Box(art,"Discarded school paper",p,new Vector3(R(.17f,.26f),.007f,R(.23f,.34f)),paper,false).transform.rotation=Quaternion.Euler(0,R(0,360),0);
            }
            for(int i=0;i<5;i++)Box(art,"Fallen plaster",data+new Vector3(R(-.6f,.6f),.015f,R(-.6f,.6f)),new Vector3(R(.06f,.2f),.03f,R(.05f,.16f)),concrete,false).transform.rotation=Quaternion.Euler(0,R(0,360),0);
            for(int i=0;i<3;i++)Box(art,"Abandoned exercise books",data+new Vector3(.1f,.0175f+i*.035f,.1f),new Vector3(.24f,.035f,.32f),i%2==0?wood:paper,false);
        }
        // Wall scars are irregular polygon patches fitted to real inward wall planes, not hovering cubes.
        foreach(float z in new[]{13.5f,21.7f,28.8f,36.4f})
        {
            Scar(new Vector3(-20.72f,4.3f,z),Vector3.right,1.1f,1.4f,grime);
            Scar(new Vector3(-17.02f,4.5f,z+.3f),Vector3.left,.8f,1.5f,concrete);
        }
        foreach(float z in new[]{4.4f,8.5f,16.7f,24.9f,36.8f})
        {
            Scar(new Vector3(-28.72f,4.5f,z),Vector3.right,R(.5f,1.7f),R(.9f,1.8f),grime);
            Scar(new Vector3(-9.02f,4.3f,z),Vector3.left,R(.5f,1.4f),R(.7f,1.6f),concrete);
        }
        // Damp restroom walls, grounded bins and pipework communicate utility rather than another classroom.
        foreach(float x in new[]{-27.8f,-23.4f,-15.5f,-10f})
        {Scar(new Vector3(x,4.5f,39.76f),Vector3.back,.9f,1.6f,grime);}
        foreach(float x in new[]{-27.9f,-10.1f})
        {Box(art,"Waste bin",new Vector3(x,3.75f,30.5f),new Vector3(.35f,.5f,.35f),rust,true);Box(art,"Bin opening",new Vector3(x,4.005f,30.5f),new Vector3(.29f,.015f,.29f),dark,false);}
        // Skirtings and surface conduits attach to walls and make room edges readable at eye level.
        foreach(float x in new[]{-28.71f,-9.03f})
        {Box(art,"Upper skirting",new Vector3(x,3.59f,20),new Vector3(.045f,.18f,39.4f),wood,false);Box(art,"Aged service pipe",new Vector3(x,6.26f,20),new Vector3(.055f,.055f,39.2f),rust,false);}
        foreach(float z in new[]{12.2f,19.8f,27.8f})
            Box(art,"Chipped room skirting",new Vector3(-24.8f,3.59f,z),new Vector3(7.4f,.18f,.045f),wood,false);
        // Repair old ground wall shelves: two visible brackets under each collectible support.
        var puzzle=GameObject.Find("DeadMemory_Case1_Puzzle").transform;
        foreach(Transform shelf in puzzle.Cast<Transform>().Where(t=>t.name=="Bracket").ToArray())
        {
            for(int i=-1;i<=1;i+=2)
            {
                var b=Box(art,"Shelf wall bracket",Vector3.zero,new Vector3(.035f,.2f,.18f),rust,false);
                b.transform.SetPositionAndRotation(shelf.position+shelf.right*(i*.19f)+Vector3.down*.1f-shelf.forward*.07f,shelf.rotation);
            }
        }
    }
    static void Shelf(Vector3 pos)
    {
        foreach(float z in new[]{-.85f,.85f})foreach(float x in new[]{-.22f,.22f})Box(art,"Storage shelf upright",pos+new Vector3(x,1.05f,z),new Vector3(.04f,2.1f,.04f),rust,true);
        for(int i=0;i<4;i++)
        {
            float y=.12f+i*.58f;Box(art,"Storage shelf board",pos+new Vector3(0,y,0),new Vector3(.53f,.045f,1.8f),wood,true);
            if(i<3)foreach(float z in new[]{-.5f,.4f})Box(art,"Supported supplies carton",pos+new Vector3(0,y+.0225f+.19f,z),new Vector3(.43f,.38f,.55f),paper,false);
        }
    }
    static void Table(Vector3 p,float w,float depth)
    {
        Box(art,"Maintenance worktop",p+Vector3.up*.8f,new Vector3(w,.06f,depth),wood,true);
        foreach(float x in new[]{-w*.42f,w*.42f})foreach(float z in new[]{-depth*.4f,depth*.4f})Box(art,"Worktable leg",p+new Vector3(x,.385f,z),new Vector3(.05f,.77f,.05f),rust,true);
        Box(art,"Old maintenance manual",p+new Vector3(0,.85f,0),new Vector3(.27f,.04f,.33f),paper,false);
    }
    static void Scar(Vector3 pos,Vector3 normal,float w,float h,Material m)
    {
        var g=new GameObject("Peeling plaster scar");g.transform.SetParent(art);g.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(normal));
        int count=16;var verts=new Vector3[count+1];var uv=new Vector2[count+1];var tris=new int[count*3];uv[0]=new Vector2(.5f,.5f);
        for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,k=R(.65f,1);verts[i+1]=new Vector3(Mathf.Cos(a)*w*.5f*k,Mathf.Sin(a)*h*.5f*k,0);uv[i+1]=new Vector2(verts[i+1].x/w+.5f,verts[i+1].y/h+.5f);tris[i*3]=0;tris[i*3+1]=(i+1)%count+1;tris[i*3+2]=i+1;}
        var mesh=new Mesh();mesh.name="Irregular damaged plaster";mesh.vertices=verts;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path="Assets/DeadMemory/Environment/Scar_"+g.GetInstanceID()+".asset";AssetDatabase.CreateAsset(mesh,path);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;
    }
    static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material m,bool collision)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Sign(Vector3 pos,float yaw,string text)
    {
        var g=new GameObject("School room sign");g.transform.SetParent(art);g.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));Box(g.transform,"Sign plate",Vector3.zero,new Vector3(1.2f,.25f,.025f),wood,false);
        var t=new GameObject("Room name");t.transform.SetParent(g.transform,false);t.transform.localPosition=new Vector3(0,0,.018f);t.transform.localRotation=Quaternion.Euler(0,180,0);var tm=t.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.characterSize=.018f;tm.color=new Color(.72f,.69f,.51f);t.AddComponent<MemoryWorldLabel>().Apply();
    }
    static void Lamp(string name,Vector3 pos,float intensity,float range,Color c)
    {var g=new GameObject(name);g.transform.SetParent(art);g.transform.position=pos;var l=g.AddComponent<Light>();l.type=LightType.Point;l.intensity=intensity;l.range=range;l.color=c;l.shadows=LightShadows.None;}
    static Material Simple(string name,Color c,float smooth)
    {string p="Assets/DeadMemory/Environment/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=c;m.SetFloat("_Smoothness",smooth);return m;}
    static Material Surface(string name,Color baseColor,Color decay,int kind)
    {
        const int sz=256;var tex=new Texture2D(sz,sz,TextureFormat.RGB24,true);var pixels=new Color[sz*sz];
        for(int y=0;y<sz;y++)for(int x=0;x<sz;x++)
        {
            float n=Mathf.PerlinNoise(x/38f+kind*13,y/41f+9),fine=Mathf.PerlinNoise(x/4f,y/4f),cloud=Mathf.PerlinNoise(x/85f+2,y/76f+7);
            float stain=Mathf.SmoothStep(.42f,.7f,n)*Mathf.SmoothStep(.37f,.65f,cloud);
            if(kind==1 && (x%64<2||y%64<2))stain=.85f;
            if(kind==2)stain=Mathf.Clamp01(stain+Mathf.Sin(y*.4f+Mathf.PerlinNoise(x/45f,y/80f)*4)*.1f);
            if(kind==0)
            {
                float coarse=Mathf.PerlinNoise(x/64f+3,y/73f+8),medium=Mathf.PerlinNoise(x/18f+15,y/22f+4),grain=Mathf.PerlinNoise(x/5f,y/6f);
                float mottled=coarse*.62f+medium*.27f+grain*.11f;
                pixels[y*sz+x]=Color.Lerp(baseColor,decay,Mathf.SmoothStep(.48f,.74f,mottled)*.75f)*(.94f+grain*.13f);
                continue;
            }
            pixels[y*sz+x]=Color.Lerp(baseColor,decay,stain)*(.82f+fine*.24f);
        }
        tex.SetPixels(pixels);tex.Apply();tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Bilinear;
        string tp="Assets/DeadMemory/Environment/"+name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        if(prior==null)AssetDatabase.CreateAsset(tex,tp);else{EditorUtility.CopySerialized(tex,prior);UnityEngine.Object.DestroyImmediate(tex);tex=prior;}
        var m=Simple(name,Color.white,.1f);m.mainTexture=tex;m.SetTexture("_BaseMap",tex);return m;
    }
}
