using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DeadMemoryPuzzleBuilder
{
    const string RootName = "DeadMemory_Case1_Puzzle";
    static Transform root;
    static Material steel, dark, paper, brass, blue, green, red, glass;
    static SchoolPuzzleDirector director;

    [MenuItem("DEAD MEMORY/Build Case 1 UV Puzzle")]
    public static void BuildMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if (Application.isPlaying) return "Stop Play Mode before building.";
        var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        Directory.CreateDirectory("Assets/DeadMemory/Materials");
        steel = Mat("OldSteel", new Color(.13f,.2f,.19f), .4f);
        dark = Mat("Charcoal", new Color(.025f,.035f,.038f), .12f);
        paper = Mat("AgedPaper", new Color(.68f,.64f,.49f), .05f);
        brass = Mat("OldBrass", new Color(.5f,.34f,.13f), .55f);
        blue = Mat("FuseBlue", new Color(.12f,.35f,.6f), .2f);
        green = Mat("FuseGreen", new Color(.15f,.45f,.24f), .2f);
        red = Mat("FuseRed", new Color(.55f,.15f,.12f), .2f);
        glass = Mat("PhotoInk", new Color(.13f,.14f,.11f), .1f);
        root = new GameObject(RootName).transform;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.16f,.17f,.16f);
        RenderSettings.ambientIntensity = 1;
        director = root.gameObject.AddComponent<SchoolPuzzleDirector>();
        director.player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        director.spawnPosition = new Vector3(-3.2f,.04f,5f); director.spawnYaw = 152;

        // 101: the opening classroom establishes the UV language and the final destination.
        Note("intro", "บันทึกในห้องเรียน", "ฉันกลับมาอยู่ในห้องเดิมได้อย่างไร…\nตู้เหล็กหลังห้องยังอยู่ แต่ต้องใช้ทั้งกุญแจและรหัส\nเพื่อนเคยฝากข้อความด้วยหมึกที่มองไม่เห็น\nกด Q สองครั้งเพื่อใช้แสงสีม่วง แล้วลองส่องกระดาน\nตารางเรียนบอกเวลา ส่วนกระดานบอกวิธีอ่านรหัสกล่องห้อง 102", new Vector3(-2.13f,1.6f,3f), Vector3.left, false);
        Note("timetable", "ตารางเรียนเก่า / ห้อง 101", "เข้าเรียน       08:00\nพักกลางวัน   12:00\nเลิกเรียน      15:00\n\nกล่องเครื่องมือเก็บอยู่ในห้อง 102", new Vector3(-9.82f,1.65f,4f), Vector3.right, false);
        Note("uv_schedule", "รอย UV บนกระดาน / ห้อง 101", "รหัสกล่องเครื่องมือ:\nเอาเลขหลักหน่วยของชั่วโมง\nเข้าเรียน → พักกลางวัน → เลิกเรียน", new Vector3(-6,1.75f,.155f), Vector3.forward, true);
        var final = Cabinet("photo_locker", SchoolPuzzleKind.PhotoLocker, "ตู้รูปถ่าย / ห้อง 101", new Vector3(-8.7f,0,8.8f), 180);
        Text(final.transform, "ตู้ความทรงจำ\nกุญแจ + รหัส 4 หลัก", new Vector3(0,1.5f,.278f), Vector3.forward, .026f, new Color(.86f,.8f,.64f));
        var photoRoot = new GameObject("Photograph"); photoRoot.transform.SetParent(final.transform, false); photoRoot.transform.localPosition = new Vector3(0,1.05f,.12f);
        var photo = photoRoot.AddComponent<SchoolPuzzleObject>(); photo.id = "photograph"; photo.kind = SchoolPuzzleKind.Photograph; photo.title = "ภาพถ่ายที่ถูกลืม";
        Cube(photoRoot.transform,"Polaroid",Vector3.zero,new Vector3(.36f,.43f,.025f),paper,false);
        Cube(photoRoot.transform,"Image",new Vector3(0,.025f,.017f),new Vector3(.3f,.29f,.008f),glass,false);
        // Two silhouettes printed inside the physical photograph.
        foreach(float x in new[]{-.07f,.07f}) { Cube(photoRoot.transform,"SchoolUniform",new Vector3(x,-.005f,.023f),new Vector3(.08f,.15f,.004f),steel,false); Sphere(photoRoot.transform,"Portrait",new Vector3(x,.095f,.026f),new Vector3(.066f,.066f,.006f),paper); }
        Text(photoRoot.transform,"03  /  15",new Vector3(0,-.16f,.026f),Vector3.forward,.008f,Color.black);
        var photoCollider = photoRoot.AddComponent<BoxCollider>(); photoCollider.size = new Vector3(.4f,.46f,.06f);
        photoRoot.SetActive(false);

        // 102: a numerical interpretation rather than an arbitrary collectible hunt.
        var toolbox = Cabinet("toolbox",SchoolPuzzleKind.Toolbox,"กล่องเครื่องมือ / ห้อง 102",new Vector3(8.8f,0,8.7f),180);
        Text(toolbox.transform,"กล่องเครื่องมือ\nรหัสจากตารางเรียน",new Vector3(0,1.4f,.278f),Vector3.forward,.024f,new Color(.9f,.8f,.58f));
        Note("maintenance", "ใบแจ้งซ่อม / ห้อง 102", "ฟิวส์สำรองหายไปสามตัว:\nสีแดง — หลังตะแกรงห้อง 103 ต้องใช้ไขควง\nสีน้ำเงิน — ชั้นอุปกรณ์ห้อง 104\nสีเขียว — ห้องเก็บของ 106\n\nตู้ไฟอยู่ห้อง 107 ผังวงจรอยู่ห้อง 105\nชะแลงถูกเก็บในช่องแม่เหล็ก จะเปิดได้เมื่อคืนไฟ",new Vector3(9.8f,1.6f,5.8f),Vector3.left,false);

        // 103: a tool gate and a key that can be found before its lock.
        var vent = Cabinet("vent",SchoolPuzzleKind.Vent,"ตะแกรงช่องซ่อม / ห้อง 103",new Vector3(-9.25f,0,18.6f),180);
        Text(vent.transform,"ฟิวส์สำรอง\nสกรูปากแบน",new Vector3(0,1.4f,.278f),Vector3.forward,.025f,new Color(.85f,.82f,.7f));
        for(int i=0;i<6;i++) Cube(vent.doorLeaf,"Grille",new Vector3(.45f,.35f+i*.22f,.034f),new Vector3(.7f,.035f,.035f),brass,false);
        Pickup("staff_key","dm_staff_key","กุญแจลิ้นชักครู",new Vector3(-9.7f,1.1f,12.5f),Vector3.right,brass);
        Note("uv_bag","รอยบนกระเป๋า / ห้อง 103","ฝากกุญแจลิ้นชักครูไว้ที่นี่\nแฟ้มของพวกเราอยู่ห้อง 105",new Vector3(-9.8f,1.6f,12.5f),Vector3.right,true);

        // 104: the first half of the personal identity riddle.
        Pickup("blue_fuse","dm_fuse_blue","ฟิวส์สีน้ำเงิน",new Vector3(9.7f,1.1f,13.5f),Vector3.left,blue);
        Note("uv_my_number","เลขที่ของฉัน / ห้อง 104","ฉันคือเลขที่ 03\n\nนาฬิกาของเรา:\nเลขของฉันเป็นชั่วโมง\nเลขของเพื่อนเป็นนาที",new Vector3(6,1.7f,10.155f),Vector3.forward,true);

        // 105: reusable evidence and a separate order puzzle.
        var drawer = Cabinet("records",SchoolPuzzleKind.RecordsDrawer,"ลิ้นชักแฟ้มครู / ห้อง 105",new Vector3(-9.25f,0,28.6f),180);
        drawer.text = "สมุดเช็กชื่อเก่า\nเลขที่ 03 — ชื่อของคุณ\nเลขที่ 15 — ชื่อของเพื่อนสนิท\n\nข้างชื่อเพื่อนเขียนว่า: ‘อย่าลืมเวลาของเรา’\nหลังแฟ้มมีเข็มนาทีซ่อนอยู่\nรหัสตู้รูปถ่าย: เลขของฉัน แล้วตามด้วยเลขของเพื่อน ใช้เลขสองหลักทั้งคู่";
        Text(drawer.transform,"แฟ้มนักเรียน\nกุญแจอยู่ห้อง 103",new Vector3(0,1.4f,.278f),Vector3.forward,.024f,new Color(.85f,.82f,.7f));
        Note("uv_breakers","ผังวงจร UV / ห้อง 105","หลังใส่ฟิวส์ครบ:\nสวิตช์ซ้าย → สวิตช์ขวา → สวิตช์กลาง\n\nหากผิด วงจรจะตัดและเริ่มลำดับใหม่\nฟิวส์จะไม่เสีย",new Vector3(-6,1.75f,20.155f),Vector3.forward,true);

        // 106: a physical latch as a second, different lock.
        Pickup("green_fuse","dm_fuse_green","ฟิวส์สีเขียว",new Vector3(9.7f,1.1f,22.5f),Vector3.left,green);
        var supply = Cabinet("supply",SchoolPuzzleKind.SupplyLocker,"ตู้เก็บของติดแน่น / ห้อง 106",new Vector3(9.25f,0,28.7f),180);
        Text(supply.transform,"บานตู้บิดติด\nต้องใช้ชะแลง",new Vector3(0,1.4f,.278f),Vector3.forward,.025f,new Color(.85f,.82f,.7f));

        // 107: fuse insertion and an order-sensitive sequence; wrong guesses are recoverable.
        var panel = Cabinet("fuse_panel",SchoolPuzzleKind.FusePanel,"ตู้ไฟ / ห้อง 107",new Vector3(-8.6f,0,38.5f),180);
        Text(panel.transform,"ตู้ไฟสำรอง\nช่องฟิวส์ 3 ช่อง\nผังวงจร: ห้อง 105",new Vector3(0,1.4f,.278f),Vector3.forward,.024f,new Color(.85f,.82f,.7f));
        for(int i=0;i<3;i++)
        {
            Cube(panel.transform,"FuseSocket_"+i,new Vector3(-.24f+i*.24f,.8f,.29f),new Vector3(.18f,.32f,.04f),dark,false);
            var cartridge=Cube(panel.transform,"FuseCartridge_"+new[]{"dm_fuse_blue","dm_fuse_green","dm_fuse_red"}[i],new Vector3(-.24f+i*.24f,.8f,.325f),new Vector3(.12f,.24f,.055f),i==0?blue:i==1?green:red,false);
            cartridge.SetActive(false);
        }
        Note("power_note","คำเตือนข้างตู้ไฟ / ห้อง 107","ใส่ฟิวส์ทั้งสามสีให้ครบก่อนเปิดสวิตช์\nอย่าเดาลำดับ ให้ดูผัง UV ห้อง 105\nเมื่อไฟกลับ ช่องแม่เหล็กจะปล่อยชะแลงออกมา",new Vector3(-9.8f,1.7f,35.5f),Vector3.right,false);

        // 108: reconstruct the clock, then interpret both personal numbers.
        var clock = new GameObject("MemoryClock"); clock.transform.SetParent(root); clock.transform.position = new Vector3(9.72f,1.8f,34.5f); clock.transform.rotation = Quaternion.LookRotation(Vector3.left,Vector3.up);
        var clockNode = clock.AddComponent<SchoolPuzzleObject>(); clockNode.id="clock"; clockNode.kind=SchoolPuzzleKind.Clock; clockNode.title="นาฬิกาที่หยุดเดิน / ห้อง 108";
        var clockCollider=clock.AddComponent<BoxCollider>(); clockCollider.size = new Vector3(1.05f,1.05f,.13f);
        var face=GameObject.CreatePrimitive(PrimitiveType.Cylinder); face.name="ClockFace"; face.transform.SetParent(clock.transform,false); face.transform.localRotation=Quaternion.Euler(90,0,0);face.transform.localScale=new Vector3(1,.045f,1);face.GetComponent<Renderer>().sharedMaterial=paper;UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
        for(int i=1;i<=12;i++) { float a=i*Mathf.PI/6;Text(clock.transform,i.ToString(),new Vector3(Mathf.Sin(a)*.4f,Mathf.Cos(a)*.4f,.06f),Vector3.forward,.013f,Color.black); }
        director.clockHour=Hand(clock.transform,"HourHand",.22f,.024f);
        director.clockMinute=Hand(clock.transform,"MinuteHand",.33f,.015f);
        Text(clock.transform,"ช่องลับใต้หน้าปัด",new Vector3(0,-.7f,.07f),Vector3.forward,.02f,new Color(.85f,.82f,.7f));
        Note("clock_note","ข้อความหลังนาฬิกา / ห้อง 108","เข็มทั้งสองหายไป\nเข็มชั่วโมงอยู่ในตู้ห้อง 106\nเข็มนาทีอยู่ในลิ้นชักครูห้อง 105\n\nเมื่อใส่เข็มครบ ให้ตั้งเวลาจากเลขที่ของเราสองคน\nกุญแจที่ซ่อนไว้ข้างในจะเปิดช่องของมัน",new Vector3(9.8f,1.65f,36.5f),Vector3.left,false);

        // Legible room names form a navigation aid that belongs to the school.
        for(int i=0;i<4;i++)
        {
            string left=new[]{"101  ห้องเรียนเก่า","103  ห้องซ่อมบำรุง","105  ห้องแฟ้มครู","107  ห้องควบคุมไฟ"}[i];
            string right=new[]{"102  ห้องเครื่องมือ","104  ห้องเรียน","106  ห้องเก็บของ","108  ห้องนาฬิกา"}[i];
            Sign(left,new Vector3(-1.88f,2.55f,5+i*10),Vector3.right);
            Sign(right,new Vector3(1.88f,2.55f,5+i*10),Vector3.left);
        }
        Note("floor_plan","แผนผังอาคารเรียน","มองจากทางเข้าแล้วเดินตรงไปตามโถง\nซ้าย: 101 → 103 → 105 → 107\nขวา: 102 → 104 → 106 → 108\n\nแสงขาวใช้เดินสำรวจ แสง UV ใช้อ่านร่องรอยที่ถูกลบ\nคำใบ้ที่พบจะเก็บไว้ในสมุดบันทึก [Tab]",new Vector3(1.87f,1.65f,1.8f),Vector3.left,false);
        for(int i=0;i<4;i++) Practical("PuzzleLamp_"+i,new Vector3(0,2.85f,5+i*10),.9f,false);
        for(int i=0;i<4;i++)foreach(float x in new[]{-6f,6f})Practical("RoomFill_"+x+"_"+i,new Vector3(x,2.8f,5+i*10),2.8f,false);
        Practical("PowerRestored_Lamp",new Vector3(-6,2.8f,35),2.8f,true);
        // Correct missing authored leaf references only where the puzzle route needs the door.
        foreach(var door in UnityEngine.Object.FindObjectsByType<SchoolDoor>(FindObjectsSortMode.None))
            if(door.transform.position.y<1 && door.leaf==null)
            {
                var leaf=Cube(door.transform,"PuzzleRepairedLeaf",new Vector3(0,1.05f,.6f),new Vector3(.06f,2.1f,1.18f),steel,true);
                door.leaf=leaf.transform;
            }
        var cc=director.player.GetComponent<CharacterController>(); cc.enabled=false; director.player.transform.position=director.spawnPosition; director.player.transform.rotation=Quaternion.Euler(0,director.spawnYaw,0); cc.enabled=true;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Built "+root.GetComponentsInChildren<SchoolPuzzleObject>(true).Length+" puzzle objects; Case 1 saved.";
    }
    static Material Mat(string name,Color color,float metal)
    {
        string path="Assets/DeadMemory/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Cube(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Sphere(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    static GameObject Text(Transform parent,string text,Vector3 p,Vector3 normal,float size,Color color)
    {
        var g=new GameObject("Label");g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.LookRotation(-normal,Vector3.up);
        var tm=g.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=color;g.AddComponent<MemoryWorldLabel>().Apply();
        var cabinet=parent.GetComponent<SchoolPuzzleObject>();
        if(cabinet!=null&&cabinet.doorLeaf!=null)g.transform.SetParent(cabinet.doorLeaf,true);
        return g;
    }
    static SchoolPuzzleObject Cabinet(string id,SchoolPuzzleKind kind,string title,Vector3 position,float yaw)
    {
        var g=new GameObject("Puzzle_"+id);g.transform.SetParent(root);g.transform.position=position;g.transform.rotation=Quaternion.Euler(0,yaw,0);
        var n=g.AddComponent<SchoolPuzzleObject>();n.id=id;n.kind=kind;n.title=title;
        var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,.9f,0);c.size=new Vector3(.9f,1.8f,.5f);
        Cube(g.transform,"Back",new Vector3(0,.9f,-.2f),new Vector3(.9f,1.8f,.06f),steel,true);
        Cube(g.transform,"Shelf",new Vector3(0,.75f,0),new Vector3(.88f,.04f,.46f),steel,false);
        foreach(float x in new[]{-.44f,.44f})Cube(g.transform,"Side",new Vector3(x,.9f,0),new Vector3(.06f,1.8f,.46f),steel,true);
        foreach(float y in new[]{.03f,1.77f})Cube(g.transform,"Cap",new Vector3(0,y,0),new Vector3(.9f,.06f,.46f),steel,true);
        var hinge=new GameObject("Hinge");hinge.transform.SetParent(g.transform,false);hinge.transform.localPosition=new Vector3(-.43f,0,.26f);n.doorLeaf=hinge.transform;
        Cube(hinge.transform,"DoorLeaf",new Vector3(.43f,.9f,0),new Vector3(.86f,1.72f,.045f),steel,false);
        Cube(hinge.transform,"Handle",new Vector3(.76f,1.0f,.05f),new Vector3(.04f,.18f,.08f),brass,false);
        return n;
    }
    static SchoolPuzzleObject Note(string id,string title,string text,Vector3 position,Vector3 normal,bool uv)
    {
        var g=new GameObject("Puzzle_"+id);g.transform.SetParent(root);g.transform.position=position;g.transform.rotation=Quaternion.LookRotation(normal,Vector3.up);
        var n=g.AddComponent<SchoolPuzzleObject>();n.id=id;n.kind=uv?SchoolPuzzleKind.UVClue:SchoolPuzzleKind.Note;n.title=title;n.text=text;
        var c=g.AddComponent<BoxCollider>();c.size=new Vector3(uv?2.8f:1.25f,uv?1f:.9f,.025f);
        if(!uv)Cube(g.transform,"Paper",Vector3.zero,new Vector3(1.25f,.9f,.018f),paper,false);
        var label=Text(g.transform,uv?text:title+"\n[E] อ่าน",new Vector3(0,0,.025f),Vector3.forward,uv?.022f:.025f,uv?new Color(.78f,.4f,1f):new Color(.06f,.07f,.055f));
        if(uv){var u=label.AddComponent<UVRevealable>();u.clueId=id;u.autoMaterial=false;u.maxDistance=5.5f;u.coneUse=.72f;u.fadeInSpeed=2.8f;n.uv=u;}
        return n;
    }
    static void Pickup(string id,string itemId,string title,Vector3 position,Vector3 normal,Material material)
    {
        var g=new GameObject("Puzzle_"+id);g.transform.SetParent(root);g.transform.position=position;g.transform.rotation=Quaternion.LookRotation(normal,Vector3.up);
        var n=g.AddComponent<SchoolPuzzleObject>();n.id=id;n.kind=SchoolPuzzleKind.Pickup;n.title=title;n.itemId=itemId;n.itemName=title;
        var c=g.AddComponent<BoxCollider>();c.size=new Vector3(.48f,.25f,.25f);
        Cube(g.transform,"Item",Vector3.zero,new Vector3(.27f,.11f,.13f),material,false);
        var bracket=Cube(g.transform,"Bracket",new Vector3(0,-.14f,-.06f),new Vector3(.55f,.04f,.35f),steel,true);bracket.transform.SetParent(root,true);
        Text(g.transform,title,new Vector3(0,.22f,.12f),Vector3.forward,.019f,new Color(.85f,.82f,.7f));
    }
    static Transform Hand(Transform parent,string name,float length,float width)
    {
        var p=new GameObject(name).transform;p.SetParent(parent,false);p.localPosition=new Vector3(0,0,.085f);Cube(p,"Hand",new Vector3(0,length/2,0),new Vector3(width,length,.018f),dark,false);return p;
    }
    static void Sign(string text,Vector3 position,Vector3 normal)
    {
        var p=new GameObject("RoomSign").transform;p.SetParent(root);p.position=position;p.rotation=Quaternion.LookRotation(normal,Vector3.up);
        Cube(p,"Plate",Vector3.zero,new Vector3(1.2f,.3f,.02f),dark,false);Text(p,text,new Vector3(0,0,.02f),Vector3.forward,.018f,new Color(.8f,.8f,.65f));
    }
    static void Practical(string name,Vector3 position,float intensity,bool power)
    {
        var g=new GameObject(name);g.transform.SetParent(root);g.transform.position=position;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.74f,.46f);l.intensity=intensity;l.range=7;l.shadows=LightShadows.None;l.enabled=!power;
    }
}
