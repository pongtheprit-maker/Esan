using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Refines the existing level, keeping scene references and puzzle IDs intact.</summary>
public static class DeadMemoryRealityPass
{
    public const string Folder = "Assets/DeadMemory/RealityPass/";
    static readonly List<string> log = new List<string>();
    static Material paperWall, greenWall, utilityWall, ceiling, stairs, timber, steel;
    static Transform detailRoot;
    public static string PathOf(Transform t) { string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path; }
    public static bool Structural(Renderer r)
    {
        if(r==null||r.GetComponent<MeshFilter>()==null||r.GetComponentInParent<SchoolDoor>()!=null||r.GetComponentInParent<SchoolPuzzleObject>()!=null||r.GetComponentInParent<HidingLocker>()!=null)return false;
        string n=r.name;
        return Regex.IsMatch(n,"(?i)(wall|ceiling|floor|closure|jamb|landing|perimeter|divider|partition|lintel|stair side passage)") &&
            !Regex.IsMatch(n,"(?i)(chair|desk|paper|support|stain|curled|scar|notice|board|tube|light|skirting|sign|bracket|handle)") &&
            r.GetComponent<BoxCollider>()!=null;
    }
    public static bool WalkSurface(Collider c)
    {
        return c!=null && Regex.IsMatch(c.name,"(?i)(floor|tread|landing|stair side passage)") && !Regex.IsMatch(c.name,"(?i)(paper|carton|sheet|door|cabinet)");
    }
    [MenuItem("DEAD MEMORY/Refine Surfaces and Physical Scene")]
    public static void Menu() { Debug.Log(Apply()); }
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Reality pass requires Edit Mode.");
        log.Clear(); Directory.CreateDirectory(Folder);
        var old=GameObject.Find("DeadMemory_RealityDetails");if(old!=null)Object.DestroyImmediate(old);
        detailRoot=new GameObject("DeadMemory_RealityDetails").transform;
        MakeMaterials(); RepairDoors(); SnapPuzzleCabinets(); GroundFurniture();
        RemoveExactDuplicates(); AssignSurfaces(); RepairSurfaceMeshes(); AddStairNosing();
        log.Add(DeadMemoryRealityFinishing.Apply());
        Physics.SyncTransforms(); AssetDatabase.SaveAssets();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Folder+"Changes.txt",log);return string.Join("\n",log);
    }
    static Texture2D Texture(string file)
    {
        string path=Folder+file+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Missing texture "+path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        importer.maxTextureSize=2048;importer.anisoLevel=8;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name,Texture2D texture,Color tint,Vector2 scale,float seed,float bump,float smooth)
    {
        var shader=Shader.Find("DeadMemory/WeatheredSurface");if(shader==null)throw new InvalidOperationException("Weathered surface shader missing");
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",tint);
        m.SetVector("_WorldScale",new Vector4(scale.x,scale.y,0,0));m.SetFloat("_Seed",seed);m.SetFloat("_Variation",.22f);m.SetFloat("_BumpStrength",bump);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static void MakeMaterials()
    {
        var wallTexture=Texture("Wallpaper");var ceilingTexture=Texture("Ceiling");var stairTexture=Texture("StairTerrazzo");
        paperWall=Material("Wall_IvoryPaper",wallTexture,new Color(.73f,.73f,.65f),new Vector2(.19f,.38f),1.7f,.09f,.07f);
        greenWall=Material("Wall_FadedSagePaper",wallTexture,new Color(.57f,.66f,.59f),new Vector2(.19f,.38f),5.3f,.08f,.07f);
        utilityWall=Material("Wall_DampUtilityRender",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DeadMemory/HauntedArt/AbandonedPlaster.png"),new Color(.65f,.68f,.62f),new Vector2(.28f,.28f),8.9f,.1f,.06f);
        ceiling=Material("Ceiling_CrackedLimePlaster",ceilingTexture,new Color(.68f,.69f,.65f),new Vector2(.33f,.33f),3.2f,.07f,.035f);
        stairs=Material("Stair_WornTerrazzo",stairTexture,new Color(.68f,.68f,.64f),new Vector2(1.35f,1.35f),4.5f,.14f,.19f);
        timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/WornWood.mat");
        steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeadMemory/Environment/CorrodedMetal.mat");
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void RepairDoors()
    {
        int obsolete=0,restored=0,fitted=0;
        foreach(var door in Object.FindObjectsByType<SchoolDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(door.name=="Door_L0") {door.enabled=false;continue;} // Reception opening is intentionally doorless.
            foreach(Transform child in door.transform.Cast<Transform>().ToArray())
            {
                if(child.GetComponent<BoxCollider>()!=null&&Mathf.Abs(child.localPosition.z)>2)
                {child.gameObject.SetActive(false);child.name="Obsolete duplicate slab (inactive)";obsolete++;}
            }
            if(door.leaf==null)
            {
                var body=door.GetComponentsInChildren<BoxCollider>().FirstOrDefault();
                if(body!=null)door.leaf=body.transform;
                else if(door.name=="Door_L3")
                {
                    var leaf=GameObject.CreatePrimitive(PrimitiveType.Cube);leaf.name=door.name+"_Leaf";leaf.transform.SetParent(door.transform,false);
                    leaf.transform.localPosition=new Vector3(0,1.05f,.55f);leaf.transform.localScale=new Vector3(.045f,2.1f,1.1f);leaf.GetComponent<Renderer>().sharedMaterial=timber;door.leaf=leaf.transform;restored++;
                }
            }
            if(door.leaf==null){door.enabled=false;continue;}
            door.enabled=true;
            var col=door.leaf.GetComponent<BoxCollider>();if(col==null)col=door.leaf.gameObject.AddComponent<BoxCollider>();col.enabled=true;col.isTrigger=false;
            // Authored panels touched both jambs and the floor. Keep actual visible fitting gaps.
            if(door.leaf.localPosition.y-door.leaf.localScale.y*.5f<.015f)
            {var scale=door.leaf.localScale;scale.y-=.04f;door.leaf.localScale=scale;}
            if(door.leaf.localPosition.z-door.leaf.localScale.z*.5f<.014f)
            {var scale=door.leaf.localScale;scale.z-=.04f;door.leaf.localScale=scale;}
            var renderer=door.leaf.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=timber;
            if(!door.name.Contains("Stall"))door.openAngle=Mathf.Sign(door.openAngle)*88f;
            door.rotationSpeed=135;door.smoothTime=.24f;
            var previous=door.transform.Find("Reality hinge hardware");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var hardware=new GameObject("Reality hinge hardware").transform;hardware.SetParent(door.transform,false);
            float height=door.leaf.localScale.y;
            foreach(float fraction in new[]{.15f,.5f,.85f})
            {
                float y=door.leaf.localPosition.y-height*.5f+height*fraction;
                Box(hardware,"Hinge plate",new Vector3(-.035f,y,.035f),new Vector3(.018f,.11f,.075f),steel);
                var pin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pin.name="Hinge pin";pin.transform.SetParent(hardware,false);pin.transform.localPosition=new Vector3(0,y,0);pin.transform.localScale=new Vector3(.025f,.057f,.025f);pin.GetComponent<Renderer>().sharedMaterial=steel;Object.DestroyImmediate(pin.GetComponent<Collider>());
            }
            EditorUtility.SetDirty(door);fitted++;
        }
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(Regex.IsMatch(r.name,"(?i)(doorjamb|doorframe|connector jamb|connector header)"))r.sharedMaterial=timber;
        log.Add("Doors: fitted "+fitted+", restored missing leaf "+restored+", isolated remote duplicate slabs "+obsolete+". Door_L0 remains a doorless foyer opening.");
    }
    static void SnapPuzzleCabinets()
    {
        int count=0;Physics.SyncTransforms();
        var nodes=Object.FindObjectsByType<SchoolPuzzleObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var node in nodes.Where(n=>n.kind==SchoolPuzzleKind.PhotoLocker||n.kind==SchoolPuzzleKind.Toolbox||n.kind==SchoolPuzzleKind.RecordsDrawer||n.kind==SchoolPuzzleKind.SupplyLocker||n.kind==SchoolPuzzleKind.Vent||n.kind==SchoolPuzzleKind.FusePanel))
        {
            var back=node.transform.Find("Back");if(back==null)continue;var renderer=back.GetComponent<Renderer>();if(renderer==null)continue;
            Vector3 front=node.transform.forward;Bounds bounds=renderer.bounds;
            float extent=Mathf.Abs(front.x)*bounds.extents.x+Mathf.Abs(front.y)*bounds.extents.y+Mathf.Abs(front.z)*bounds.extents.z;
            Vector3 backPoint=bounds.center-front*extent;
            var hit=Physics.RaycastAll(backPoint+front*.08f,-front,2.1f,~0,QueryTriggerInteraction.Ignore).Where(h=>Structural(h.collider.GetComponent<Renderer>())).OrderBy(h=>h.distance).FirstOrDefault();
            if(hit.collider==null){log.Add("Cabinet requires inspection, no backing wall: "+node.id);continue;}
            float gap=Vector3.Dot(backPoint-hit.point,front);Vector3 shift=front*(.016f-gap);
            node.transform.position+=shift;
            if(node.id=="photo_locker") {var photograph=nodes.FirstOrDefault(n=>n.id=="photograph");if(photograph!=null)photograph.transform.position+=shift;}
            Physics.SyncTransforms();count++;log.Add("Cabinet "+node.id+": moved "+shift.ToString("F3")+", backing clearance 0.016 m.");
        }
        log.Add("Puzzle assemblies backed by real walls: "+count+".");
    }
    static bool Furniture(MeshRenderer r)
    {
        if(r.GetComponentInParent<SchoolDoor>()!=null||r.GetComponentInParent<SchoolPuzzleObject>()!=null||r.GetComponentInParent<HidingLocker>()!=null||r.GetComponentInParent<Animator>()!=null)return false;
        if(PathOf(r.transform).StartsWith("DeadMemory_EmptyAreaDressing/")||PathOf(r.transform).StartsWith("DeadMemory_HauntedArt/"))return false;
        return Regex.IsMatch(r.name,"(?i)(desk|chair|locker|bench|table|shelf|seat|leg|^back(?: |$)|^top(?: |$)|^rail(?: |$)|starleg|worktop|cabinet|reception counter|^body$|_spine$|^bowlback$|^bowlfront$)") && !Regex.IsMatch(r.name,"(?i)(wall|bracket|light|ceiling|door|sign|paper|sheet)");
    }
    public static List<List<MeshRenderer>> FurnitureGroups()
    {
        var all=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(Furniture).ToArray();var parent=Enumerable.Range(0,all.Length).ToArray();
        Func<int,int> find=null;find=i=>parent[i]==i?i:(parent[i]=find(parent[i]));
        var assemblies=new Dictionary<Transform,int>();
        for(int i=0;i<all.Length;i++)
        {
            for(var t=all[i].transform.parent;t!=null;t=t.parent)
            {
                if(!Regex.IsMatch(t.name,"(?i)(^(desk|teacherdesk|chair|bench|locker)$|_chair$)"))continue;
                int first;if(assemblies.TryGetValue(t,out first))parent[find(i)]=find(first);else assemblies[t]=i;break;
            }
        }
        for(int i=0;i<all.Length;i++)for(int j=0;j<i;j++)
        {
            Bounds a=all[i].bounds,b=all[j].bounds;a.Expand(.12f);
            if(a.Intersects(b)){int x=find(i),y=find(j);if(x!=y)parent[x]=y;}
        }
        return Enumerable.Range(0,all.Length).GroupBy(find).Select(g=>g.Select(i=>all[i]).ToList()).ToList();
    }
    public static bool FloorBelow(Bounds bounds,out float floor)
    {
        floor=0;
        foreach(var x in new[]{.15f,.5f,.85f})foreach(var z in new[]{.15f,.5f,.85f})
        {
            Vector3 origin=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,x),bounds.min.y+.12f,Mathf.Lerp(bounds.min.z,bounds.max.z,z));
            var hit=Physics.RaycastAll(origin,Vector3.down,1.4f,~0,QueryTriggerInteraction.Ignore).Where(h=>WalkSurface(h.collider)&&h.normal.y>.8f).OrderBy(h=>h.distance).FirstOrDefault();
            if(hit.collider!=null){floor=hit.point.y;return true;}
        }
        return false;
    }
    static void GroundFurniture()
    {
        int moved=0,total=0;float largest=0;
        var casters=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Office chair caster").ToArray();
        foreach(var group in FurnitureGroups())
        {
            Bounds bounds=group[0].bounds;foreach(var r in group.Skip(1))bounds.Encapsulate(r.bounds);
            float floor;if(!FloorBelow(bounds,out floor))continue;total++;
            float delta=floor-bounds.min.y;
            if(Mathf.Abs(delta)<.004f||Mathf.Abs(delta)>.08f)continue;
            // Office chair casters are separate visible parts in the authored scene.
            bool casterSupport=casters.Any(r=>r.bounds.min.y<floor+.01f&&r.bounds.max.y>=bounds.min.y-.01f&&r.bounds.max.x>=bounds.min.x&&r.bounds.min.x<=bounds.max.x&&r.bounds.max.z>=bounds.min.z&&r.bounds.min.z<=bounds.max.z);
            if(casterSupport)continue;
            var targets=group.Select(r=>r.transform).Distinct().Where(t=>!group.Any(r=>r.transform!=t&&t.IsChildOf(r.transform))).ToArray();
            foreach(var t in targets)t.position+=Vector3.up*delta;
            largest=Mathf.Max(largest,Mathf.Abs(delta));moved++;
        }
        Physics.SyncTransforms();log.Add("Furniture contact groups inspected "+total+", grounded assemblies "+moved+", largest vertical correction "+largest.ToString("F3")+" m.");
    }
    static bool AxisAligned(Transform t)
    {var v=t.eulerAngles;return Mathf.Abs(Mathf.DeltaAngle(v.x,Mathf.Round(v.x/90)*90))<.02f&&Mathf.Abs(Mathf.DeltaAngle(v.y,Mathf.Round(v.y/90)*90))<.02f&&Mathf.Abs(Mathf.DeltaAngle(v.z,Mathf.Round(v.z/90)*90))<.02f;}
    static void RemoveExactDuplicates()
    {
        var groups=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>Structural(r)&&AxisAligned(r.transform)).GroupBy(r=>string.Join("/",new[]{r.bounds.min.x,r.bounds.min.y,r.bounds.min.z,r.bounds.max.x,r.bounds.max.y,r.bounds.max.z}.Select(v=>Mathf.RoundToInt(v*1000))));int count=0;
        foreach(var g in groups.Where(g=>g.Count()>1))foreach(var r in g.OrderBy(r=>PathOf(r.transform),StringComparer.Ordinal).Skip(1))
        {r.gameObject.SetActive(false);log.Add("Removed exact structural duplicate from active scene: "+PathOf(r.transform));count++;}
        log.Add("Exact duplicate structural assemblies deactivated: "+count+".");
    }
    static void AssignSurfaces()
    {
        int walls=0,ceilings=0,steps=0;
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(r.name.StartsWith("StairsUp_Tread")){r.sharedMaterial=stairs;steps++;continue;}
            if(!Structural(r))continue;Bounds b=r.bounds;
            if(r.name.IndexOf("ceiling",StringComparison.OrdinalIgnoreCase)>=0&&b.size.x>2&&b.size.z>2){r.sharedMaterial=ceiling;ceilings++;}
            else if(Regex.IsMatch(r.name,"(?i)(wall|closure|perimeter|divider|partition|lintel)")&&!r.name.Contains("Jamb"))
            {r.sharedMaterial=b.center.y>3.4f&&b.center.z>=20?utilityWall:b.center.x<-20?paperWall:greenWall;walls++;}
        }
        log.Add("Distinct scanned surfaces: "+walls+" wall sections, "+ceilings+" ceiling slabs, "+steps+" stair treads. Wall-space stochastic sampling removes repeated stain stamps.");
    }
    struct Face
    {
        public MeshRenderer renderer;public int axis,side;public float plane;public Rect rect;
        public float Area {get{return rect.width*rect.height;}}
    }
    static List<Rect> Cut(Rect original,Rect cutter)
    {
        float x0=Mathf.Max(original.xMin,cutter.xMin),x1=Mathf.Min(original.xMax,cutter.xMax),y0=Mathf.Max(original.yMin,cutter.yMin),y1=Mathf.Min(original.yMax,cutter.yMax);
        if(x1-x0<.0005f||y1-y0<.0005f)return new List<Rect>{original};
        var result=new List<Rect>();
        if(x0-original.xMin>.0005f)result.Add(Rect.MinMaxRect(original.xMin,original.yMin,x0,original.yMax));
        if(original.xMax-x1>.0005f)result.Add(Rect.MinMaxRect(x1,original.yMin,original.xMax,original.yMax));
        if(y0-original.yMin>.0005f)result.Add(Rect.MinMaxRect(x0,original.yMin,x1,y0));
        if(original.yMax-y1>.0005f)result.Add(Rect.MinMaxRect(x0,y1,x1,original.yMax));return result;
    }
    static void RepairSurfaceMeshes()
    {
        var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>StaticCuboid(r)&&AxisAligned(r.transform)).OrderBy(r=>PathOf(r.transform),StringComparer.Ordinal).ThenBy(r=>r.GetInstanceID()).ToArray();
        var faces=new List<Face>();
        foreach(var r in renderers)for(int axis=0;axis<3;axis++)for(int side=0;side<2;side++)
        {
            var b=r.bounds;int u=(axis+1)%3,v=(axis+2)%3;
            faces.Add(new Face{renderer=r,axis=axis,side=side,plane=side==0?b.min[axis]:b.max[axis],rect=Rect.MinMaxRect(b.min[u],b.min[v],b.max[u],b.max[v])});
        }
        var order=renderers.Select((r,i)=>new{r,i}).ToDictionary(x=>x.r,x=>x.i);
        var buckets=faces.GroupBy(f=>f.axis+"/"+f.side+"/"+Mathf.RoundToInt(f.plane*1000)).ToDictionary(g=>g.Key,g=>g.ToList());
        int clipped=0,rectangles=0;Directory.CreateDirectory(Folder+"Meshes");
        foreach(var r in renderers)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var main=new List<int>();var underside=new List<int>();
            bool floor=Regex.IsMatch(r.name,"(?i)(floor|stair side passage)")&&r.bounds.size.y<.5f;
            foreach(var face in faces.Where(f=>f.renderer==r))
            {
                var pieces=new List<Rect>{face.rect};
                var nearby=new List<Face>();
                for(int k=-2;k<=2;k++){List<Face> set;if(buckets.TryGetValue(face.axis+"/"+face.side+"/"+(Mathf.RoundToInt(face.plane*1000)+k),out set))nearby.AddRange(set);}
                foreach(var other in nearby.Where(f=>f.renderer!=r&&Mathf.Abs(f.plane-face.plane)<.0015f))
                {
                    bool owns=other.Area>face.Area+.0001f || (Mathf.Abs(other.Area-face.Area)<.0001f&&order[other.renderer]<order[r]);
                    if(!owns)continue;
                    var next=new List<Rect>();foreach(var part in pieces)next.AddRange(Cut(part,other.rect));
                    if(next.Count!=pieces.Count||next.Sum(p=>p.width*p.height)<pieces.Sum(p=>p.width*p.height)-.0001f)clipped++;
                    pieces=next;if(pieces.Count==0)break;
                }
                foreach(var piece in pieces)
                {
                    int u=(face.axis+1)%3,v=(face.axis+2)%3;Vector3 normal=Vector3.zero;normal[face.axis]=face.side==0?-1:1;
                    Vector3 a=Vector3.zero;a[face.axis]=face.plane;a[u]=piece.xMin;a[v]=piece.yMin;
                    Vector3 b=a;b[u]=piece.xMax;Vector3 c=b;c[v]=piece.yMax;Vector3 d=a;d[v]=piece.yMax;
                    int start=vertices.Count;
                    foreach(var p in new[]{a,b,c,d})
                    {
                        Vector3 local=r.transform.InverseTransformPoint(p);vertices.Add(local);normals.Add(r.transform.InverseTransformDirection(normal));
                        Vector3 ln=r.transform.InverseTransformDirection(normal);int dominant=Mathf.Abs(ln.x)>.5f?0:Mathf.Abs(ln.y)>.5f?1:2;
                        uv.Add(dominant==0?new Vector2(local.z+.5f,local.y+.5f):dominant==1?new Vector2(local.x+.5f,local.z+.5f):new Vector2(local.x+.5f,local.y+.5f));
                    }
                    var target=floor&&face.axis==1&&face.side==0?underside:main;
                    if(face.side==1)target.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});else target.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});rectangles++;
                }
            }
            string key=Hash128.Compute(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).ToString();string meshPath=Folder+"Meshes/Surface_"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
            mesh.name="Coplanar-clean "+r.name;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=underside.Count>0?2:1;
            mesh.SetTriangles(main,0);if(underside.Count>0)mesh.SetTriangles(underside,1);mesh.RecalculateBounds();
            var first=r.sharedMaterials[0];r.GetComponent<MeshFilter>().sharedMesh=mesh;r.sharedMaterials=underside.Count>0?new[]{first,ceiling}:new[]{first};EditorUtility.SetDirty(mesh);
        }
        log.Add("Static cuboid coplanar intersections removed by mesh face clipping: "+clipped+"; render sections "+renderers.Length+", retained surface rectangles "+rectangles+". Original solid collision envelopes retained.");
    }
    public static bool StaticCuboid(MeshRenderer r)
    {
        var filter=r.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)return false;
        if(filter.sharedMesh.name!="Cube"&&!filter.sharedMesh.name.StartsWith("Coplanar-clean"))return false;
        if(r.GetComponentInParent<SchoolDoor>()!=null||r.GetComponentInParent<HidingLocker>()!=null||r.GetComponentInParent<FirstPersonController>()!=null||r.GetComponentInParent<Animator>()!=null)return false;
        var puzzle=r.GetComponentInParent<SchoolPuzzleObject>();if(puzzle!=null&&puzzle.doorLeaf!=null&&r.transform.IsChildOf(puzzle.doorLeaf))return false;
        return true;
    }
    public static string RestoreFurnitureSnapshotAndResetMeshes()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var original=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Folder+"Scene_Before.json"));int restored=0,reset=0;
        foreach(var entry in original["renderers"])
        {
            var r=EditorUtility.InstanceIDToObject((int)entry["id"]) as MeshRenderer;
            if(r==null||r.name!=(string)entry["name"]||!Furniture(r))continue;
            var p=entry["p"];r.transform.position=new Vector3((float)p[0],(float)p[1],(float)p[2]);restored++;
        }
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temporary.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temporary);
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var filter=r.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||!filter.sharedMesh.name.StartsWith("Coplanar-clean"))continue;
            filter.sharedMesh=cube;r.sharedMaterials=new[]{r.sharedMaterials[0]};reset++;
        }
        Physics.SyncTransforms();return "Restored original furniture parts "+restored+", reset cuboid topology "+reset+" before corrected assembly pass.";
    }
    static void AddStairNosing()
    {
        int count=0;
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("StairsUp_Tread")))
        {
            Bounds b=r.bounds;
            var lip=Box(detailRoot,"Worn stair edge nosing",new Vector3(b.center.x,b.max.y+.004f,b.min.z+.019f),new Vector3(b.size.x-.045f,.008f,.036f),steel);
            lip.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            count++;
        }
        log.Add("Supported stair nosings fitted: "+count+". No new stair collision obstacles.");
    }
}
