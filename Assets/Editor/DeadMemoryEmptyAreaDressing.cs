using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Contextual dressing for vacant school halls; preserves the existing scene and gameplay.</summary>
public static class DeadMemoryEmptyAreaDressing
{
    const string Folder="Assets/DeadMemory/EmptyAreaDressing/";
    static Transform root;
    static Material wood, steel, paper, dark, green, carton, plastic, brass, glass;
    static Material[] covers;
    static readonly List<string> placed=new List<string>(), skipped=new List<string>();
    static SchoolPuzzleObject[] clues;
    static SchoolDoor[] doors;
    static HidingLocker[] hides;
    static Transform[] batteryPoints;
    static Material Mat(string name,Color color,float smooth=.1f,float metallic=0)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(Transform parent,string name,Vector3 position,Quaternion rotation)
    {var g=new GameObject(name);g.isStatic=true;g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(position,rotation);return g.transform;}
    static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.isStatic=true;g.transform.SetParent(p,false);g.transform.localPosition=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static GameObject Cylinder(Transform p,string name,Vector3 at,float diameter,float height,Material mat,Vector3 euler=default(Vector3))
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.isStatic=true;g.transform.SetParent(p,false);g.transform.localPosition=at;g.transform.localEulerAngles=euler;g.transform.localScale=new Vector3(diameter,height/2,diameter);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static bool Protected(Vector3 centre,Vector3 half,Quaternion rotation,bool floorObject)
    {
        var b=new Bounds(centre,Vector3.zero);
        foreach(float xx in new[]{-1f,1f})foreach(float yy in new[]{-1f,1f})foreach(float zz in new[]{-1f,1f})b.Encapsulate(centre+rotation*new Vector3(half.x*xx,half.y*yy,half.z*zz));
        foreach(var o in clues)
        {
            Vector3 q=o.transform.position;
            if(Mathf.Abs(q.y-centre.y)>half.y+1.15f)continue;
            if(b.SqrDistance(q)<.85f*.85f||b.SqrDistance(q+o.transform.forward*1.1f)<.6f*.6f)return true;
        }
        foreach(var p in batteryPoints)if(b.SqrDistance(p.position)<.4f*.4f)return true;
        foreach(var d in doors)
            if(Mathf.Abs(d.transform.position.y-(centre.y-half.y))<.5f&&Vector2.Distance(new Vector2(d.transform.position.x,d.transform.position.z),new Vector2(centre.x,centre.z))<1.65f+Mathf.Max(half.x,half.z))return true;
        foreach(var l in hides)
            if(Mathf.Abs(l.transform.position.y-(centre.y-half.y))<.5f&&Vector2.Distance(new Vector2(l.transform.position.x,l.transform.position.z),new Vector2(centre.x,centre.z))<2.4f+Mathf.Max(half.x,half.z))return true;
        if(!floorObject)return false;
        float y=centre.y-half.y;
        if(y<3&&centre.x>-21.1f&&centre.x< -16.7f&&centre.z<31)return true; // stair and lower hall spine
        if(y<3&&centre.x>-38.1f&&centre.x< -34.5f&&centre.z>30)return true; // west end door approach
        if(y<3&&centre.x< -9&&centre.z>33.9f&&centre.z<36.7f)return true; // cross-building rear route
        if(y>3&&centre.x>-20.0f&&centre.x< -17.7f)return true;
        return false;
    }
    static bool Floor(string name,Vector3 position,float yaw,Vector3 size,Action<Transform> build)
    {
        Quaternion rotation=Quaternion.Euler(0,yaw,0);Vector3 centre=position+Vector3.up*(size.y/2+.045f);
        if(Protected(centre,size/2,rotation,true)){skipped.Add(name+": reserved access");return false;}
        Physics.SyncTransforms();
        if(Physics.OverlapBox(centre,size/2-new Vector3(.01f,.025f,.01f),rotation,~0,QueryTriggerInteraction.Ignore).Length>0)
        {skipped.Add(name+": occupied");return false;}
        foreach(float x in new[]{-.42f,.42f})foreach(float z in new[]{-.42f,.42f})
            if(!Physics.Raycast(position+rotation*new Vector3(x*size.x,.22f,z*size.z),Vector3.down,.35f,~0,QueryTriggerInteraction.Ignore))
            {skipped.Add(name+": no floor support");return false;}
        var g=Group(root,name,position,rotation);build(g);placed.Add(name);Physics.SyncTransforms();return true;
    }
    static void WallBoard(string name,Vector3 surface,Vector3 normal,float width=2.05f)
    {
        Quaternion rotation=Quaternion.LookRotation(normal);var half=new Vector3(width/2,.54f,.10f);
        if(Protected(surface,half,rotation,false)){skipped.Add(name+": reserved wall");return;}
        RaycastHit hit;
        if(!Physics.Raycast(surface+normal*.25f,-normal,out hit,.5f,~0,QueryTriggerInteraction.Ignore)||!(hit.collider.name.Contains("Wall")||hit.collider.name.Contains("Partition")))
        {skipped.Add(name+": no wall backing");return;}
        var g=Group(root,name,hit.point+normal*.014f,rotation);
        Box(g,"Cork backing fixed to wall",new Vector3(0,0,.025f),new Vector3(width,1.04f,.05f),carton);
        foreach(float y in new[]{-.54f,.54f})Box(g,"Weathered wood frame",new Vector3(0,y,.06f),new Vector3(width+.1f,.06f,.07f),wood);
        foreach(float x in new[]{-width/2,width/2})Box(g,"Weathered wood frame",new Vector3(x,0,.06f),new Vector3(.06f,1.08f,.07f),wood);
        for(int i=0;i<5;i++)
        {
            float x=-width*.39f+i*width*.195f,y=(i%2==0?.10f:-.12f);
            var sheet=Box(g,"Faded school notice",new Vector3(x,y,.058f),new Vector3(.29f,.46f,.006f),paper);sheet.transform.localEulerAngles=new Vector3(0,0,(i-2)*2);
            for(int k=0;k<4;k++)Box(g,"Faded printed rules",new Vector3(x,y+.12f-k*.062f,.062f),new Vector3(k==0?.18f:.21f,.008f,.001f),covers[(i+k)%covers.Length]);
            Cylinder(g,"Notice pin",new Vector3(x,y+.20f,.066f),.018f,.012f,steel,new Vector3(90,0,0));
        }
        placed.Add(name);
    }
    static void Bench(Transform g)
    {
        Box(g,"Bench seat",new Vector3(0,.465f,0),new Vector3(2.15f,.07f,.48f),wood,true);
        Box(g,"Bench backrest",new Vector3(0,.76f,-.205f),new Vector3(2.15f,.40f,.045f),wood,true);
        foreach(float x in new[]{-.84f,.84f})
        {
            foreach(float z in new[]{-.17f,.17f})Box(g,"Bench leg touches floor",new Vector3(x,.215f,z),new Vector3(.045f,.43f,.045f),steel,true);
            Box(g,"Bench back support",new Vector3(x,.65f,-.205f),new Vector3(.035f,.60f,.035f),steel);
        }
        Book(g,new Vector3(.72f,.505f,.02f),1);
    }
    static void Book(Transform g,Vector3 at,int c)
    {
        Box(g,"Faded exercise book bottom cover",at+Vector3.up*.002f,new Vector3(.20f,.004f,.27f),covers[c%covers.Length]);
        Box(g,"Yellowed page block",at+Vector3.up*.013f,new Vector3(.19f,.018f,.26f),paper);
        Box(g,"Faded exercise book top cover",at+Vector3.up*.024f,new Vector3(.20f,.004f,.27f),covers[c%covers.Length]);
        Box(g,"Book binding",at+new Vector3(-.097f,.013f,0),new Vector3(.006f,.026f,.27f),covers[c%covers.Length]);
    }
    static void Cup(Transform g,Vector3 at)
    {
        Cylinder(g,"Trophy plinth",at+Vector3.up*.025f,.16f,.05f,dark);
        Cylinder(g,"Trophy stem",at+Vector3.up*.13f,.035f,.16f,brass);
        Cylinder(g,"Trophy cup base",at+Vector3.up*.22f,.11f,.05f,brass);
        // A shallow open bowl made from a ring mesh, rather than a solid cylinder cap.
        var mesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();
        const int n=16;
        for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;foreach(var r in new[]{new Vector2(.055f,.225f),new Vector2(.10f,.34f),new Vector2(.087f,.34f),new Vector2(.047f,.238f)})vertices.Add(new Vector3(Mathf.Cos(a)*r.x,r.y,Mathf.Sin(a)*r.x)+at);}
        for(int i=0;i<n;i++)for(int k=0;k<3;k++){int a=i*4+k,b=((i+1)%n)*4+k;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var obj=new GameObject("Tarnished open trophy cup");obj.isStatic=true;obj.transform.SetParent(g,false);
        string path=Folder+"Cup_"+g.name.Replace(" ","_")+"_"+at.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(mesh,path);else {EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=brass;
    }
    static void Showcase(Transform g)
    {
        Box(g,"Display cabinet base",new Vector3(0,.30f,0),new Vector3(2.15f,.60f,.55f),wood,true);
        Box(g,"Display cabinet backing",new Vector3(0,1.22f,-.255f),new Vector3(2.15f,1.25f,.04f),green,true);
        foreach(float y in new[]{.63f,1.22f,1.85f})Box(g,"Supported display shelf",new Vector3(0,y,0),new Vector3(2.15f,.045f,.55f),wood,true);
        foreach(float x in new[]{-1.04f,1.04f})Box(g,"Cabinet upright",new Vector3(x,1.22f,0),new Vector3(.055f,1.25f,.55f),steel,true);
        for(int i=0;i<5;i++)Cup(g,new Vector3(-.80f+i*.4f,.66f,0));
        for(int i=0;i<4;i++)Book(g,new Vector3(-.70f+i*.45f,1.245f,.02f),i);
        var pane=Box(g,"Dusty protective glass",new Vector3(0,1.23f,.272f),new Vector3(2.04f,1.16f,.008f),glass);
        pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        foreach(float x in new[]{-.52f,.52f})Box(g,"Sliding display frame",new Vector3(x,1.23f,.28f),new Vector3(.028f,1.17f,.015f),steel);
    }
    static void Cartons(Transform g)
    {
        var sizes=new[]{new Vector3(.76f,.48f,.66f),new Vector3(.58f,.40f,.54f),new Vector3(.43f,.30f,.41f)};float support=0;
        for(int i=0;i<sizes.Length;i++)
        {
            var s=sizes[i];Vector3 p=new Vector3(i%2*.045f,support+s.y/2,0);
            Box(g,"Closed school archive carton",p,s,carton,true);
            Box(g,"Paper packing tape",new Vector3(p.x,support+s.y+.002f,0),new Vector3(.075f,.004f,s.z+.004f),paper);
            Box(g,"Old carton label",p+new Vector3(.10f,0,s.z/2+.003f),new Vector3(.20f,.11f,.005f),paper);support+=s.y;
        }
        Book(g,new Vector3(.05f,support+.003f,0),2);
    }
    static void Wheels(Transform g,float width,float depth)
    {foreach(float x in new[]{-width/2,width/2})foreach(float z in new[]{-depth/2,depth/2})Cylinder(g,"Caster on floor",new Vector3(x,.065f,z),.13f,.045f,dark,new Vector3(0,0,90));}
    static void ChairRack(Transform g)
    {
        Wheels(g,1.55f,.48f);Box(g,"Chair trolley base",new Vector3(0,.17f,0),new Vector3(1.85f,.08f,.68f),steel,true);
        foreach(float x in new[]{-.86f,.86f})Box(g,"Rack upright",new Vector3(x,.95f,-.23f),new Vector3(.045f,1.55f,.045f),steel,true);
        Box(g,"Rack upper rail",new Vector3(0,1.70f,-.23f),new Vector3(1.76f,.055f,.055f),steel,true);
        for(int i=0;i<5;i++)
        {
            float x=-.67f+i*.33f;
            foreach(float dx in new[]{-.13f,.13f})Box(g,"Folded chair frame rests on rack",new Vector3(x+dx,.83f,0),new Vector3(.025f,1.24f,.045f),steel);
            Box(g,"Folded chair wooden seat",new Vector3(x,.70f,.025f),new Vector3(.27f,.43f,.045f),wood);
            Box(g,"Folded chair back",new Vector3(x,1.31f,.025f),new Vector3(.27f,.26f,.045f),wood);
        }
    }
    static void Janitor(Transform g)
    {
        Wheels(g,.60f,.40f);
        foreach(float y in new[]{.18f,.67f,1.04f})Box(g,"Janitor trolley tray",new Vector3(0,y,0),new Vector3(.78f,.045f,.55f),green,true);
        foreach(float x in new[]{-.34f,.34f})foreach(float z in new[]{-.22f,.22f})Box(g,"Trolley upright",new Vector3(x,.61f,z),new Vector3(.025f,.86f,.025f),steel);
        for(int i=0;i<3;i++)
        {Cylinder(g,"Cleaning bottle",new Vector3(-.23f+i*.23f,1.17f,.06f),.11f,.22f,plastic);Cylinder(g,"Bottle cap",new Vector3(-.23f+i*.23f,1.295f,.06f),.055f,.03f,covers[i]);}
        Box(g,"Folded cleaning cloth",new Vector3(.05f,.72f,0),new Vector3(.35f,.06f,.28f),paper);
        Cylinder(g,"Mop shaft",new Vector3(.52f,.79f,-.10f),.025f,1.48f,wood);
        Box(g,"Mop head on floor",new Vector3(.52f,.025f,-.10f),new Vector3(.30f,.05f,.16f),paper);
    }
    static void WaterCooler(Transform g)
    {
        Box(g,"Water dispenser cabinet",new Vector3(0,.54f,0),new Vector3(.44f,1.08f,.43f),plastic,true);
        Box(g,"Dispenser dark recess",new Vector3(0,.75f,.221f),new Vector3(.30f,.28f,.014f),dark);
        Box(g,"Drip tray",new Vector3(0,.57f,.26f),new Vector3(.34f,.025f,.14f),steel);
        Cylinder(g,"Water bottle neck supported on machine",new Vector3(0,1.13f,0),.14f,.10f,covers[1]);
        Cylinder(g,"Opaque dusty water bottle",new Vector3(0,1.38f,0),.30f,.40f,covers[1]);
        foreach(float x in new[]{-.075f,.075f})Cylinder(g,"Water tap",new Vector3(x,.80f,.25f),.035f,.07f,steel,new Vector3(90,0,0));
    }
    static void Table(Transform g)
    {
        Box(g,"Activity table top",new Vector3(0,.78f,0),new Vector3(2.35f,.075f,.88f),wood,true);
        foreach(float x in new[]{-.98f,.98f})foreach(float z in new[]{-.32f,.32f})Box(g,"Activity table leg",new Vector3(x,.37f,z),new Vector3(.045f,.74f,.045f),steel,true);
        Box(g,"Old student drawing sheet",new Vector3(-.38f,.821f,.08f),new Vector3(.70f,.006f,.46f),paper);
        for(int i=0;i<3;i++)Book(g,new Vector3(.60f,.822f+i*.027f,-.12f),i);
        Box(g,"Covered stationery box",new Vector3(-.86f,.90f,-.13f),new Vector3(.31f,.16f,.24f),green);
        foreach(float x in new[]{-.70f,.70f})foreach(float z in new[]{-.94f,.94f})
        {
            var chair=Group(g,"Activity chair",g.TransformPoint(new Vector3(x,0,z)),g.rotation*Quaternion.Euler(0,z>0?180:0,0));
            Box(chair,"Chair seat",new Vector3(0,.465f,0),new Vector3(.44f,.065f,.44f),wood,true);
            Box(chair,"Chair back",new Vector3(0,.78f,-.205f),new Vector3(.44f,.48f,.04f),wood,true);
            foreach(float dx in new[]{-.17f,.17f})foreach(float dz in new[]{-.17f,.17f})Box(chair,"Chair leg on floor",new Vector3(dx,.215f,dz),new Vector3(.03f,.43f,.03f),steel,true);
            foreach(float dx in new[]{-.17f,.17f})Box(chair,"Chair back support",new Vector3(dx,.73f,-.205f),new Vector3(.025f,.62f,.025f),steel);
        }
    }
    static void DeadPlant(Transform g)
    {
        Cylinder(g,"Old plant pot on floor",new Vector3(0,.17f,0),.38f,.34f,carton);
        Cylinder(g,"Dry soil",new Vector3(0,.344f,0),.32f,.014f,dark);
        Cylinder(g,"Dead shrub stem rooted in soil",new Vector3(0,.73f,0),.023f,.78f,wood);
        var ends=new[]{new Vector3(.20f,.93f,.11f),new Vector3(-.17f,1.04f,.08f),new Vector3(.10f,1.19f,-.13f),new Vector3(-.07f,1.25f,-.02f)};
        for(int i=0;i<ends.Length;i++)
        {
            var begin=new Vector3(0,.55f+i*.12f,0);var delta=ends[i]-begin;
            Cylinder(g,"Attached dry branch",(begin+ends[i])/2,.012f,delta.magnitude,wood,Quaternion.FromToRotation(Vector3.up,delta).eulerAngles);
            if(i%2==0){var leaf=Box(g,"Withered attached leaf",ends[i]+Vector3.down*.035f,new Vector3(.045f,.003f,.10f),covers[2]);leaf.transform.localEulerAngles=new Vector3(48,25+i*50,15);}
        }
        var col=g.gameObject.AddComponent<CapsuleCollider>();col.center=new Vector3(0,.17f,0);col.height=.34f;col.radius=.19f;
    }
    static void WasteBin(Transform g)
    {
        Cylinder(g,"Old school waste bin",new Vector3(0,.25f,0),.34f,.50f,green);
        Cylinder(g,"Dark bin liner",new Vector3(0,.502f,0),.30f,.014f,dark);
        foreach(var p in new[]{new Vector3(-.06f,.53f,.03f),new Vector3(.06f,.545f,-.04f)})Box(g,"Discarded crumpled paper",p,new Vector3(.085f,.055f,.07f),paper);
        var col=g.gameObject.AddComponent<CapsuleCollider>();col.center=new Vector3(0,.25f,0);col.radius=.17f;col.height=.5f;
    }
    static void DeskDetails()
    {
        var tops=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.transform.root!=root && (r.name=="Top"||r.name.StartsWith("Top (")||r.name.EndsWith("_Top")||r.name=="Student desk"))
            .Where(r=>r.bounds.size.y<.16f&&r.bounds.size.x>.65f&&r.bounds.size.x<1.8f&&r.bounds.size.z>.42f&&r.bounds.max.y<5)
            .OrderBy(r=>r.bounds.center.x).ThenBy(r=>r.bounds.center.z).ToArray();
        int count=0;
        foreach(var r in tops)
        {
            var b=r.bounds;Vector3 p=b.center;p.y=b.max.y+.003f;
            if(batteryPoints.Any(a=>(a.position-p).sqrMagnitude<.5f*.5f)||clues.Any(a=>(a.transform.position-p).sqrMagnitude<1))continue;
            if((count++%3)!=0)continue;
            var intended=new Bounds(p+new Vector3(.06f,.02f,.03f),new Vector3(.46f,.065f,.34f));
            if(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(other=>other!=r&&other.bounds.min.y>=p.y-.004f&&other.bounds.Intersects(intended)))continue;
            var g=Group(root,"Exercise book and pencil on existing desk",p,Quaternion.Euler(0,r.transform.eulerAngles.y,0));
            Book(g,new Vector3(.12f,0,.03f),count);
            Cylinder(g,"Pencil resting on desk",new Vector3(-.12f,.0035f,.03f),.007f,.15f,covers[3],new Vector3(0,0,90));
            placed.Add(g.name);
            if(count>84)break;
        }
    }
    static void CombineStaticDecoration()
    {
        int clusterIndex=0;
        foreach(Transform cluster in root)
        {
            var original=cluster.GetComponentsInChildren<MeshRenderer>();
            int materialIndex=0;
            foreach(var set in original.Where(r=>r.GetComponent<MeshFilter>()!=null).GroupBy(r=>r.sharedMaterial).OrderBy(s=>AssetDatabase.GetAssetPath(s.Key)))
            {
                var instances=new List<CombineInstance>();
                foreach(var r in set)
                {
                    var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                    for(int sub=0;sub<mesh.subMeshCount;sub++)instances.Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=cluster.worldToLocalMatrix*r.transform.localToWorldMatrix});
                }
                var combined=new Mesh();combined.name=cluster.name+" grouped decoration";combined.indexFormat=IndexFormat.UInt32;combined.CombineMeshes(instances.ToArray(),true,true);combined.RecalculateBounds();
                string path=Folder+"Combined_C"+clusterIndex.ToString("D2")+"_M"+materialIndex.ToString("D2")+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(combined,path);else{EditorUtility.CopySerialized(combined,saved);UnityEngine.Object.DestroyImmediate(combined);combined=saved;}
                var visual=new GameObject("Grouped "+set.Key.name);visual.isStatic=true;visual.transform.SetParent(cluster,false);visual.AddComponent<MeshFilter>().sharedMesh=combined;
                var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=set.Key;renderer.shadowCastingMode=set.Key==glass?ShadowCastingMode.Off:ShadowCastingMode.On;materialIndex++;
            }
            foreach(var r in original){var filter=r.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(r);if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);}
            clusterIndex++;
        }
    }
    [MenuItem("DEAD MEMORY/Dress Empty School Areas")]
    public static void Menu(){Debug.Log(Apply());}
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before dressing the saved scene.");
        System.IO.Directory.CreateDirectory(Folder);
        var previous=GameObject.Find("DeadMemory_EmptyAreaDressing");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
        root=new GameObject("DeadMemory_EmptyAreaDressing").transform;placed.Clear();skipped.Clear();
        clues=UnityEngine.Object.FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        doors=UnityEngine.Object.FindObjectsByType<SchoolDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        hides=UnityEngine.Object.FindObjectsByType<HidingLocker>(FindObjectsSortMode.None);
        var manager=UnityEngine.Object.FindFirstObjectByType<BatteryDropManager>();batteryPoints=manager==null?new Transform[0]:manager.spawnPoints;
        wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/WornWood.mat");steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/CorrodedMetal.mat");
        paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/OldPaper.mat");dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/DarkRubber.mat");
        green=Mat("OxidizedSchoolGreen",new Color(.13f,.21f,.19f),.12f,.2f);carton=Mat("DustyArchiveCardboard",new Color(.30f,.24f,.16f));
        plastic=Mat("YellowedPlastic",new Color(.46f,.47f,.39f),.25f);brass=Mat("TarnishedTrophyBrass",new Color(.38f,.28f,.11f),.32f,.6f);
        covers=new[]{Mat("FadedMaroonBook",new Color(.22f,.08f,.07f)),Mat("FadedBlueBook",new Color(.07f,.14f,.19f)),Mat("FadedOliveBook",new Color(.18f,.22f,.12f)),Mat("FadedOchreBook",new Color(.31f,.24f,.10f))};
        glass=Mat("DustyDisplayGlass",new Color(.30f,.37f,.34f,.11f),.35f);
        glass.SetFloat("_Surface",1);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
        // West rear hall: institutional display and waiting corners, with the exit approach open.
        Floor("West hall trophy cabinet",new Vector3(-42.0f,0,39.48f),180,new Vector3(2.3f,1.9f,.66f),Showcase);
        Floor("West hall chair trolley",new Vector3(-29.15f,0,38.9f),165,new Vector3(2.0f,1.85f,.85f),ChairRack);
        Floor("West hall waiting bench",new Vector3(-45.65f,0,32.7f),90,new Vector3(2.3f,1.02f,.65f),Bench);
        Floor("West hall archive cartons",new Vector3(-44.6f,0,38.9f),170,new Vector3(.92f,1.3f,.85f),Cartons);
        Floor("West hall cleaning trolley",new Vector3(-27.2f,0,32.7f),-90,new Vector3(1.3f,1.6f,.72f),Janitor);
        Floor("West hall exit waiting bench",new Vector3(-39.8f,0,38.7f),180,new Vector3(2.3f,1.02f,.65f),Bench);
        Floor("West hall drinking water corner",new Vector3(-33.0f,0,39.1f),180,new Vector3(.58f,1.65f,.60f),WaterCooler);
        Floor("West hall waste bin",new Vector3(-32.35f,0,39.1f),0,new Vector3(.45f,.65f,.45f),WasteBin);
        Floor("West hall abandoned potted shrub left",new Vector3(-38.4f,0,39.3f),15,new Vector3(.45f,1.35f,.45f),DeadPlant);
        Floor("West hall abandoned potted shrub right",new Vector3(-34.1f,0,39.3f),-35,new Vector3(.45f,1.35f,.45f),DeadPlant);
        WallBoard("West hall school noticeboard",new Vector3(-33.0f,1.9f,39.88f),Vector3.back);
        WallBoard("West hall student work display",new Vector3(-39.5f,1.9f,39.88f),Vector3.back);
        // The broad under-building space becomes a disused school activity/work area.
        Floor("Lower hall activity table A",new Vector3(-23.4f,0,17.8f),0,new Vector3(2.5f,1.10f,2.6f),Table);
        Floor("Lower hall activity table B",new Vector3(-12.9f,0,23.8f),180,new Vector3(2.5f,1.10f,2.6f),Table);
        Floor("Lower hall activity table C",new Vector3(-23.4f,0,23.4f),0,new Vector3(2.5f,1.10f,2.6f),Table);
        Floor("Lower hall activity table D",new Vector3(-12.9f,0,17.4f),180,new Vector3(2.5f,1.10f,2.6f),Table);
        Floor("Lower hall folded-chair rack",new Vector3(-24.5f,0,27.8f),0,new Vector3(2.0f,1.85f,.85f),ChairRack);
        Floor("Lower hall archive supplies",new Vector3(-11.3f,0,28.7f),0,new Vector3(.92f,1.3f,.85f),Cartons);
        Floor("Lower hall water dispenser",new Vector3(-10.8f,0,14.0f),-90,new Vector3(.58f,1.65f,.60f),WaterCooler);
        Floor("Lower hall waiting bench",new Vector3(-24.9f,0,13.2f),90,new Vector3(2.3f,1.02f,.65f),Bench);
        WallBoard("Lower hall activity board",new Vector3(-24.5f,1.8f,30.86f),Vector3.back,2.4f);
        WallBoard("Lower hall school display",new Vector3(-12.6f,1.8f,30.86f),Vector3.back,2.4f);
        // Rear connecting hall keeps its cross-building centre clear.
        Floor("Rear connector school trophy case",new Vector3(-20.5f,0,39.45f),180,new Vector3(2.3f,1.9f,.66f),Showcase);
        Floor("Rear connector waiting bench",new Vector3(-28.9f,0,31.6f),0,new Vector3(2.3f,1.02f,.65f),Bench);
        Floor("Rear connector old files",new Vector3(-11.3f,0,38.9f),180,new Vector3(.92f,1.3f,.85f),Cartons);
        WallBoard("Rear connector noticeboard",new Vector3(-15.5f,1.8f,39.88f),Vector3.back);
        // Small additions in existing rooms, avoiding clue approaches and all saved battery positions.
        foreach(var p in new[]{new Vector3(-27.7f,3.5f,9.7f),new Vector3(-10f,3.5f,9.7f),new Vector3(-8.8f,0,9.0f),new Vector3(8.8f,0,19.0f)})
            Floor("Room archive cartons "+p.ToString(),p,0,new Vector3(.92f,1.3f,.85f),Cartons);
        Floor("Staff room water dispenser",new Vector3(-10.1f,3.5f,23.5f),-90,new Vector3(.58f,1.65f,.60f),WaterCooler);
        Floor("Restroom maintenance trolley",new Vector3(-27.8f,3.5f,29.5f),0,new Vector3(1.3f,1.6f,.72f),Janitor);
        WallBoard("Classroom 2A student work board",new Vector3(-27.0f,5.25f,.13f),Vector3.forward,1.65f);
        WallBoard("Classroom 2B student work board",new Vector3(-11.0f,5.25f,.13f),Vector3.forward,1.65f);
        DeskDetails();
        CombineStaticDecoration();
        Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        string report="Empty school area dressing saved. Clusters="+placed.Count+", renderers="+root.GetComponentsInChildren<Renderer>().Length+", occupied/protected locations skipped="+skipped.Count+"\nPLACED\n"+string.Join("\n",placed)+"\nSKIPPED\n"+string.Join("\n",skipped);
        System.IO.File.WriteAllText(Folder+"PlacementNotes.txt",report);return report;
    }
}
