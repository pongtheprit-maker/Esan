using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DeadMemoryEnvironmentPolish
{
    static Transform root;
    static Material wood,steel,paper,dark;
    static GameObject Box(string name,Vector3 p,Vector3 s,Material m,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;
        if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    public static string Apply()
    {
        if(Application.isPlaying)return "Stop Play Mode first";
        var old=GameObject.Find("DeadMemory_RoomPolish");if(old!=null)Object.DestroyImmediate(old);
        root=new GameObject("DeadMemory_RoomPolish").transform;
        wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/WornWood.mat");
        steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/CorrodedMetal.mat");
        paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/OldPaper.mat");
        dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/DarkRubber.mat");
        // Complete the two classrooms into coherent desk rows facing the existing teaching wall.
        foreach(float centre in new[]{-24.87f,-12.87f})foreach(float x in new[]{centre-1.75f,centre+1.75f})foreach(float z in new[]{2.1f,4.15f})
        {
            Box("Student desk",new Vector3(x,4.26f,z),new Vector3(1,.08f,.65f),wood,true);
            foreach(float dx in new[]{-.44f,.44f})foreach(float dz in new[]{-.27f,.27f})Box("Student desk support",new Vector3(x+dx,3.88f,z+dz),new Vector3(.04f,.76f,.04f),steel,true);
            Box("Student chair seat",new Vector3(x,3.98f,z-.74f),new Vector3(.43f,.07f,.43f),wood,true);
            Box("Student chair back",new Vector3(x,4.3f,z-.95f),new Vector3(.43f,.52f,.045f),wood,true);
            foreach(float dx in new[]{-.17f,.17f})foreach(float dz in new[]{-.17f,.17f})Box("Student chair leg",new Vector3(x+dx,3.72f,z-.74f+dz),new Vector3(.035f,.44f,.035f),steel);
            Box("Exercise book on desk",new Vector3(x+.15f,4.31f,z+.07f),new Vector3(.24f,.02f,.3f),paper);
        }
        // Office chair bases have real floor contact, rather than floating star-shaped spokes.
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="StarLeg"))
        {
            Vector3 pos=t.position+t.forward*.12f;pos.y=3.535f;
            Box("Office chair caster",pos,new Vector3(.065f,.07f,.07f),dark);
        }
        // Small corridor clusters along the wall, away from room doors and the central walking lane.
        var rng=new System.Random(198315);
        foreach(float z in new[]{20.4f,28.8f,37.4f})
        {
            Box("Corridor bench",new Vector3(-20.32f,3.96f,z),new Vector3(.48f,.08f,1.45f),wood,true);
            foreach(float dz in new[]{-.55f,.55f})Box("Bench foot",new Vector3(-20.32f,3.71f,z+dz),new Vector3(.38f,.42f,.06f),steel,true);
            for(int i=0;i<16;i++)
            {
                float x=-20.15f+(float)rng.NextDouble()*.6f,zz=z-1.1f+(float)rng.NextDouble()*2.2f;
                var g=Box("Windblown corridor paper",new Vector3(x,3.504f,zz),new Vector3(.2f,.007f,.28f),paper);g.transform.rotation=Quaternion.Euler(0,rng.Next(360),0);
            }
        }
        // Correct tilted folders so their lowest corner rests on the shelf.
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Folder"))
        {
            var r=t.GetComponent<Renderer>();if(r==null)continue;
            float shelfY=4.68f;float bottom=r.bounds.min.y;
            if(bottom>shelfY+.005f||bottom<shelfY-.01f)t.position+=Vector3.up*(shelfY-bottom);
        }
        // A loose paper/book on a floor is grounded; stacked books are built together by the main builder.
        foreach(var group in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Abandoned exercise books")
            .GroupBy(r=>Mathf.RoundToInt(r.transform.position.x*100)+"/"+Mathf.RoundToInt(r.transform.position.z*100)+"/"+(r.transform.position.y>3)))
        {
            float support=group.First().transform.position.y>3?3.5f:0;
            foreach(var r in group.OrderBy(r=>r.transform.position.y))
            {r.transform.position+=Vector3.up*(support-r.bounds.min.y);support=r.bounds.max.y;}
        }
        // The ground connector opens into a reception foyer, not through a room still dressed as a classroom.
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToArray())
        {
            Vector3 p=r.bounds.center;string n=r.name;
            if(p.y<3&&p.x>-9.8f&&p.x< -2.2f&&p.z>.1f&&p.z<9.8f&&(n.Contains("Desk")||n.Contains("Chair")||n.Contains("Blackboard")))
                Object.DestroyImmediate(r.gameObject);
        }
        foreach(float z in new[]{1.2f,8.7f})
        {
            Box("Foyer waiting bench",new Vector3(-6,.48f,z),new Vector3(2.1f,.08f,.55f),wood,true);
            foreach(float x in new[]{-6.85f,-5.15f})Box("Waiting bench support",new Vector3(x,.22f,z),new Vector3(.07f,.44f,.4f),steel,true);
        }
        Box("Reception counter",new Vector3(-3.5f,.5f,8.8f),new Vector3(1.7f,1,.65f),wood,true);
        Box("Reception ledger",new Vector3(-3.5f,1.025f,8.8f),new Vector3(.3f,.05f,.4f),paper);
        Box("Foyer notice board",new Vector3(-6,1.7f,.16f),new Vector3(2.4f,1.15f,.04f),wood);
        foreach(float x in new[]{-6.8f,-6.2f,-5.6f})Box("Old school notice",new Vector3(x,1.7f,.185f),new Vector3(.4f,.55f,.008f),paper);
        foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            if(t.text=="101"&&t.transform.position.y<3)t.text="โถงต้อนรับ";
        var plan=Object.FindObjectsByType<SchoolPuzzleObject>(FindObjectsSortMode.None).FirstOrDefault(n=>n.id=="floor_plan");
        if(plan!=null)plan.text=plan.text.Replace("ทางเชื่อมเข้าห้อง 101","ทางเชื่อมเข้าโถงต้อนรับ").Replace("ห้องเรียน 101–105","โถงต้อนรับ / ห้องเรียน 102–105");
        foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.GetComponent<MemoryWorldLabel>()==null))
        {tm.characterSize=.016f;tm.gameObject.AddComponent<MemoryWorldLabel>().Apply();}
        var tube=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Select(r=>r.sharedMaterial).FirstOrDefault(m=>m!=null&&m.name=="School_TubeLit");
        if(tube!=null)
        {
            Box("Rear hall light housing",new Vector3(-18.9f,6.72f,30.5f),new Vector3(1.3f,.1f,.2f),steel);
            Box("Rear hall light tube",new Vector3(-18.9f,6.655f,30.5f),new Vector3(1.16f,.035f,.075f),tube);
            Box("Stairwell light housing",new Vector3(-18.3f,6.72f,7.1f),new Vector3(1.3f,.1f,.2f),steel);
            Box("Stairwell light tube",new Vector3(-18.3f,6.655f,7.1f),new Vector3(1.16f,.035f,.075f),tube);
            Box("Connector light housing",new Vector3(-16f,3.28f,3.5f),new Vector3(1.3f,.08f,.2f),steel);
            Box("Connector light tube",new Vector3(-16f,3.225f,3.5f),new Vector3(1.16f,.03f,.075f),tube);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return "Room polish saved: classroom rows, chair casters, supported folders, grounded books, corridor benches and debris.";
    }
}
