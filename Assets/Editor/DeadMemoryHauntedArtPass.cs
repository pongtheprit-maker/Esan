using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Art only: aged surfaces, light hierarchy, haze, atmospheric optics and supported set dressing.</summary>
public static class DeadMemoryHauntedArtPass
{
    const string Folder="Assets/DeadMemory/HauntedArt/";
    static Transform root;
    static System.Random random;
    static Material plaster, undercoat, chalk, timber, iron, paper, cable, dust;
    static Texture2D wallTexture, floorTexture;
    static float R(float a,float b){return a+(float)random.NextDouble()*(b-a);}
    [MenuItem("DEAD MEMORY/Apply Haunted School Art")]
    public static void Menu(){Debug.Log(Apply());}
    public static string Apply()
    {
        if(Application.isPlaying)return "Stop Play Mode before applying art.";
        random=new System.Random(160315);
        Directory.CreateDirectory(Folder);
        var previous=GameObject.Find("DeadMemory_HauntedArt");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
        root=new GameObject("DeadMemory_HauntedArt").transform;
        wallTexture=ImportTexture("AbandonedPlaster.png");floorTexture=ImportTexture("AbandonedTerrazzo.png");
        plaster=Mat("PeelingPaint",new Color(.72f,.75f,.68f),.07f,wallTexture);
        undercoat=Mat("ExposedDampRender",new Color(.31f,.34f,.30f),.035f,wallTexture);
        chalk=Mat("FallenPaint",new Color(.40f,.39f,.31f),.03f,wallTexture);
        timber=Mat("RottenTimber",new Color(.15f,.12f,.075f),.06f,null);
        iron=Mat("BlackenedIron",new Color(.065f,.075f,.064f),.18f,null);
        paper=Mat("WaterloggedPaper",new Color(.38f,.36f,.26f),.02f,wallTexture);
        cable=Mat("RubberCable",new Color(.013f,.019f,.018f),.12f,null);
        AssetDatabase.StartAssetEditing();
        try { UpgradeSurfaces(); } finally { AssetDatabase.StopAssetEditing(); }
        ConfigureOptics();
        ConfigureLights();
        AssetDatabase.StartAssetEditing();
        try { DressUpperHall(); DressRooms(); } finally { AssetDatabase.StopAssetEditing(); }
        AddDust();
        var sanity=UnityEngine.Object.FindFirstObjectByType<SanitySystem>();
        if(sanity!=null){sanity.distortedSaturation=-28;sanity.distortedContrast=20;}
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.16f,.18f,.17f);
        RenderSettings.ambientIntensity=1;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.022f;RenderSettings.fogColor=new Color(.085f,.103f,.098f);
        Physics.SyncTransforms();AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Haunted school art saved: detailed decayed surfaces, layered peel meshes, boarded windows, sagging cables, dark practical pools, dust, fog and distant softness. New decoration colliders="+root.GetComponentsInChildren<Collider>().Length;
    }
    static Texture2D ImportTexture(string file)
    {
        string path=Folder+file;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer!=null){importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Mat(string name,Color color,float smooth,Texture2D texture)
    {
        string p=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}
        m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Cull",0);
        if(texture!=null){m.SetTexture("_BaseMap",texture);m.mainTexture=texture;}
        EditorUtility.SetDirty(m);return m;
    }
    static void UpgradeSurfaces()
    {
        // Preserve each authored material's tiling while replacing its coarse source texture.
        var upgraded=new System.Collections.Generic.Dictionary<Material,Material>();
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(r.GetComponent<TextMesh>()!=null||r.sharedMaterial==null||r.GetComponentInParent<FlashlightController>()!=null)continue;
            var source=r.sharedMaterial;string n=source.name;
            string texturePath=AssetDatabase.GetAssetPath(source.mainTexture);
            string sourcePath=AssetDatabase.GetAssetPath(source);
            string authoredPath=sourcePath.StartsWith(Folder+"Surf_")?AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(sourcePath).Substring(5)):sourcePath;
            string authoredName=Path.GetFileNameWithoutExtension(authoredPath);
            // Unity uses the material asset filename after reload; identify generated surfaces by their source texture.
            if(authoredName=="FO_Stall"||authoredName=="School_Door"||authoredName=="School_TubeLit")n=authoredName;
            else if(texturePath.EndsWith("/PeelingPlaster.asset")||texturePath.EndsWith("/AbandonedPlaster.png"))n=r.name.IndexOf("ceiling",StringComparison.OrdinalIgnoreCase)>=0?"DampCeiling":"PeelingPlaster";
            else if(texturePath.EndsWith("/DampCeiling.asset"))n="DampCeiling";
            else if(texturePath.EndsWith("/WornFloor.asset")||texturePath.EndsWith("/AbandonedTerrazzo.png"))n="WornFloor";
            if(r.name=="Peeling plaster scar"){r.sharedMaterial=undercoat;continue;}
            Material m;
            if(upgraded.TryGetValue(source,out m)){r.sharedMaterial=m;continue;}
            bool relevant=n.StartsWith("PeelingPlaster")||n.StartsWith("DampCeiling")||n=="School_Wall"||n=="School_Ceiling"||n.StartsWith("WornFloor")||n=="School_Floor"||n=="FO_TileFloor"||n=="School_TubeLit"||n=="FO_Stall"||n=="School_Door";
            if(!relevant)continue;
            string originalPath=AssetDatabase.GetAssetPath(source);
            string copyPath=originalPath.StartsWith(Folder+"Surf_")?originalPath:Folder+"Surf_"+AssetDatabase.AssetPathToGUID(originalPath)+".mat";
            m=AssetDatabase.LoadAssetAtPath<Material>(copyPath);
            if(m==null){m=new Material(source);m.name=n;AssetDatabase.CreateAsset(m,copyPath);}
            upgraded[source]=m;r.sharedMaterial=m;
            if(n.StartsWith("PeelingPlaster")||n.StartsWith("DampCeiling")||n=="School_Wall"||n=="School_Ceiling")
            {m.SetTexture("_BaseMap",wallTexture);m.mainTexture=wallTexture;m.color=n.StartsWith("DampCeiling")?new Color(.50f,.55f,.49f):new Color(.75f,.77f,.70f);m.SetFloat("_Smoothness",.06f);}
            else if(n.StartsWith("WornFloor")||n=="School_Floor"||n=="FO_TileFloor")
            {m.SetTexture("_BaseMap",floorTexture);m.mainTexture=floorTexture;m.color=new Color(.8f,.83f,.79f);m.SetFloat("_Smoothness",.13f);}
            else if(n=="School_TubeLit")
            {m.SetColor("_BaseColor",new Color(.56f,.63f,.53f));m.SetColor("_EmissionColor",new Color(1.15f,1.3f,1.1f));m.EnableKeyword("_EMISSION");}
            else if(n=="FO_Stall"||n=="School_Door")
            {m.SetTexture("_BaseMap",wallTexture);m.color=n=="FO_Stall"?new Color(.16f,.26f,.25f):new Color(.27f,.25f,.18f);m.SetFloat("_Smoothness",.1f);}
            EditorUtility.SetDirty(m);
        }
        // Floor top faces use X/Z dimensions, keeping square terrazzo tiles square.
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(r.sharedMaterial==null||r.sharedMaterial.mainTexture!=floorTexture)continue;
            Vector3 size=r.bounds.size;
            if(size.y<.45f&&size.x>1&&size.z>1)
            {r.sharedMaterial.mainTextureScale=new Vector2(size.x/2.4f,size.z/2.4f);EditorUtility.SetDirty(r.sharedMaterial);}
        }
    }
    static T Effect<T>(VolumeProfile p)where T:VolumeComponent
    {T effect;if(!p.TryGet(out effect))effect=p.Add<T>(true);if(!AssetDatabase.Contains(effect))AssetDatabase.AddObjectToAsset(effect,p);effect.active=true;EditorUtility.SetDirty(effect);return effect;}
    static void ConfigureOptics()
    {
        var v=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(x=>x.isGlobal);
        if(v==null){v=new GameObject("Global Horror Volume").AddComponent<Volume>();v.isGlobal=true;}
        string path=Folder+"HauntedVolumeProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null)
        {
            if(v.sharedProfile!=null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(v.sharedProfile),path);
            else AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<VolumeProfile>(),path);
            profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        }
        v.sharedProfile=profile;
        var ca=Effect<ColorAdjustments>(profile);ca.postExposure.Override(1.4f);ca.saturation.Override(-43);ca.contrast.Override(15);ca.colorFilter.Override(new Color(.88f,.94f,.91f));
        var wb=Effect<WhiteBalance>(profile);wb.temperature.Override(-8);wb.tint.Override(-3);
        var split=Effect<SplitToning>(profile);split.shadows.Override(new Color(.47f,.50f,.515f));split.highlights.Override(new Color(.52f,.505f,.475f));split.balance.Override(-16);
        var vig=Effect<Vignette>(profile);vig.intensity.Override(.34f);vig.smoothness.Override(.68f);vig.rounded.Override(false);
        var grain=Effect<FilmGrain>(profile);grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.24f);grain.response.Override(.72f);
        var bloom=Effect<Bloom>(profile);bloom.intensity.Override(.23f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.58f);
        var aberration=Effect<ChromaticAberration>(profile);aberration.intensity.Override(.025f);
        var dof=Effect<DepthOfField>(profile);dof.mode.Override(DepthOfFieldMode.Gaussian);dof.gaussianStart.Override(7);dof.gaussianEnd.Override(22);dof.gaussianMaxRadius.Override(.85f);dof.highQualitySampling.Override(true);
        var tone=Effect<Tonemapping>(profile);tone.mode.Override(TonemappingMode.ACES);
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {var data=camera.GetComponent<UniversalAdditionalCameraData>();if(data!=null){data.renderPostProcessing=true;data.requiresDepthTexture=true;}}
        EditorUtility.SetDirty(profile);EditorUtility.SetDirty(v);
    }
    static void ConfigureLights()
    {
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(l.GetComponentInParent<FlashlightController>()!=null)continue;
            if(l.type==LightType.Directional){l.intensity=.15f;l.color=new Color(.45f,.58f,.64f);continue;}
            string n=l.name;
            bool upper=l.transform.position.y>3.5f;
            if(n.StartsWith("RoomFill")){l.intensity=.75f;l.range=5.2f;l.color=new Color(.60f,.68f,.63f);}
            else if(n.StartsWith("PuzzleLamp")){l.intensity=.55f;l.range=5;l.color=new Color(.77f,.72f,.54f);}
            else if(upper)
            {
                l.intensity=n.Contains("Storage")?3.8f:n.Contains("Classroom")?3.7f:n.Contains("Hall")||n.Contains("UpperHall")?3.8f:3.0f;
                l.range=n.Contains("Storage")?6.5f:7;
                l.color=n.Contains("Storage")||n.Contains("Stair")?new Color(.81f,.75f,.54f):new Color(.62f,.76f,.70f);
            }
            else if(l.transform.position.x>-11&&l.transform.position.x<11)
            {l.intensity=Mathf.Min(l.intensity,6);l.range=Mathf.Min(l.range,7);l.color=new Color(.63f,.75f,.68f);}
            if(l.shadows!=LightShadows.None){l.shadows=LightShadows.Soft;l.shadowStrength=.9f;}
            EditorUtility.SetDirty(l);
        }
    }
    static GameObject Box(string name,Vector3 p,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Cable(Vector3 a,Vector3 b,float sag)
    {
        var g=new GameObject("Sagging service cable (anchored ends)");g.transform.SetParent(root);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=cable;line.useWorldSpace=true;line.positionCount=17;line.startWidth=line.endWidth=.019f;line.numCapVertices=2;line.shadowCastingMode=ShadowCastingMode.On;
        for(int i=0;i<17;i++){float t=i/16f;line.SetPosition(i,Vector3.Lerp(a,b,t)-Vector3.up*(Mathf.Sin(t*Mathf.PI)*sag));}
        foreach(var p in new[]{a,b})Box("Cable fixing",p,new Vector3(.09f,.08f,.05f),iron);
    }
    static void Peel(Vector3 position,Vector3 normal,float width,float height)
    {
        var g=new GameObject("Curled paint attached to wall");g.transform.SetParent(root);g.transform.SetPositionAndRotation(position,Quaternion.LookRotation(normal));
        var v=new[]{new Vector3(-width/2,height/2,0),new Vector3(width/2,height/2,0),new Vector3(-width*.42f,-height*.12f,.025f),new Vector3(width*.46f,-height*.08f,.035f),new Vector3(-width*.31f,-height/2,.09f),new Vector3(width*.21f,-height*.47f,.12f)};
        var mesh=new Mesh();mesh.name="Folded paint flake";mesh.vertices=v;mesh.uv=new[]{new Vector2(0,1),new Vector2(1,1),new Vector2(.05f,.45f),new Vector2(.95f,.48f),new Vector2(.2f,0),new Vector2(.8f,.03f)};mesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5};mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Folder+"Flake_"+g.GetInstanceID()+".asset");g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=plaster;
    }
    static void Scatter(Vector3 centre,float radius,int count)
    {
        for(int i=0;i<count;i++)
        {
            Vector3 p=centre+new Vector3(R(-radius,radius),.009f,R(-radius,radius));
            var g=Box("Grounded paint and plaster fragment",p,new Vector3(R(.04f,.16f),.018f,R(.05f,.22f)),i%4==0?paper:chalk);g.transform.rotation=Quaternion.Euler(0,R(0,360),0);
        }
    }
    static void DressUpperHall()
    {
        foreach(float z in new[]{13.8f,20.8f,28.6f,36.8f})
        {
            for(int i=0;i<6;i++)Peel(new Vector3(-20.755f,R(3.8f,6.2f),z+R(-.55f,.55f)),Vector3.right,R(.08f,.26f),R(.15f,.45f));
            for(int i=0;i<4;i++)Peel(new Vector3(-16.985f,R(3.9f,6.0f),z+R(-.5f,.5f)),Vector3.left,R(.1f,.24f),R(.13f,.4f));
            Scatter(new Vector3(-20.25f,3.5f,z),.4f,18);
        }
        Cable(new Vector3(-20.72f,6.55f,19.1f),new Vector3(-17.02f,6.55f,21.1f),.5f);
        Cable(new Vector3(-20.72f,6.55f,28.2f),new Vector3(-17.02f,6.55f,29.4f),.36f);
        Cable(new Vector3(-20.72f,6.5f,36),new Vector3(-20.72f,6.5f,38),.8f);
        // Frame-mounted rotten boarding over the outer staff/storage windows.
        foreach(float z in new[]{12.5f,17.5f,22.5f,27.5f})
        {
            for(int i=0;i<2;i++)
            {var b=Box("Window boarding fixed to frame",new Vector3(-28.72f,4.87f+i*.65f,z),new Vector3(.055f,.13f,1.9f),timber);b.transform.rotation=Quaternion.Euler(i==0?-11:8,0,0);}
            Scatter(new Vector3(-28.3f,3.5f,z),.3f,12);
        }
        // Torn bulletin papers remain physical decoration with no puzzle text.
        foreach(float z in new[]{20.5f,28.9f})
        {
            Box("Abandoned school noticeboard",new Vector3(-17.01f,5.15f,z),new Vector3(.045f,.8f,1.25f),timber);
            for(int i=0;i<4;i++)Box("Water-stained notice",new Vector3(-17.04f,5.15f+R(-.12f,.12f),z-.45f+i*.27f),new Vector3(.008f,.42f,.23f),paper);
        }
    }
    static void DressRooms()
    {
        foreach(var p in new[]{new Vector3(-27.9f,3.5f,7.8f),new Vector3(-9.8f,3.5f,8.6f),new Vector3(-27.9f,3.5f,16.7f),new Vector3(-9.6f,3.5f,22.4f),new Vector3(-27.8f,3.5f,35.7f),new Vector3(-10f,3.5f,36.1f),new Vector3(-8.9f,0,8.8f),new Vector3(9f,0,17.6f),new Vector3(-8.8f,0,29f),new Vector3(9,0,35.6f)})
        {
            Scatter(p,.45f,24);
            Vector3 normal=p.x< -20||p.x<0&&p.y<1?Vector3.right:Vector3.left;
            float wallX=p.y>3?(p.x< -20?-28.755f:-8.985f):(p.x<0?-9.88f:9.88f);
            for(int i=0;i<5;i++)Peel(new Vector3(wallX,p.y+R(.3f,2.6f),p.z+R(-.8f,.8f)),normal,R(.09f,.3f),R(.12f,.45f));
        }
        // Ceiling flakes remain attached along their upper edge; none falls into the player's capsule.
        foreach(var p in new[]{new Vector3(-19.6f,6.68f,18.8f),new Vector3(-18.1f,6.68f,27.7f),new Vector3(-25.8f,6.68f,6),new Vector3(-11.5f,6.68f,35)})
            Peel(p,Vector3.down,R(.25f,.55f),R(.25f,.65f));
        // A battered empty school chair and fallen timber stay at the edge of circulation.
        foreach(var p in new[]{new Vector3(-20.3f,3.5f,38.3f),new Vector3(-17.5f,3.5f,27.8f)})
        {
            Box("Forgotten chair seat",p+Vector3.up*.46f,new Vector3(.43f,.065f,.43f),timber);
            Box("Forgotten chair back",p+new Vector3(0,.76f,.2f),new Vector3(.43f,.55f,.045f),timber);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.17f,.17f})Box("Chair leg",p+new Vector3(x,.215f,z),new Vector3(.03f,.43f,.03f),iron);
        }
    }
    static Texture2D ParticleMask()
    {
        string path=Folder+"AirParticleMask.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(tex!=null)return tex;
        const int n=64;tex=new Texture2D(n,n,TextureFormat.RGBA32,true);var px=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;float a=Mathf.Pow(Mathf.Clamp01(1-d),2.4f);px[y*n+x]=new Color(1,1,1,a);}
        tex.SetPixels(px);tex.Apply();tex.wrapMode=TextureWrapMode.Clamp;AssetDatabase.CreateAsset(tex,path);return tex;
    }
    static void AddDust()
    {
        string path=Folder+"AirParticles.mat";dust=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(dust==null){dust=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(dust,path);}
        dust.SetTexture("_BaseMap",ParticleMask());dust.SetColor("_BaseColor",Color.white);dust.SetFloat("_Surface",1);dust.SetFloat("_Blend",0);dust.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);dust.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);dust.SetInt("_ZWrite",0);dust.SetFloat("_Cull",0);dust.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");dust.renderQueue=3000;EditorUtility.SetDirty(dust);
        foreach(var p in new[]{new Vector3(-18.9f,5,18),new Vector3(-18.9f,5,31),new Vector3(0,1.5f,14),new Vector3(0,1.5f,31),new Vector3(-24.8f,5,6.5f)})
        {
            var g=new GameObject("Slow suspended dust");g.transform.SetParent(root);g.transform.position=p;var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=true;main.prewarm=true;main.duration=20;main.startLifetime=new ParticleSystem.MinMaxCurve(12,22);main.startSpeed=new ParticleSystem.MinMaxCurve(.01f,.035f);main.startSize=new ParticleSystem.MinMaxCurve(.009f,.027f);main.startColor=new Color(.57f,.61f,.54f,.20f);main.maxParticles=75;main.simulationSpace=ParticleSystemSimulationSpace.World;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(3,2.3f,12);var emission=ps.emission;emission.rateOverTime=2.2f;var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.y=.015f;velocity.x=.007f;var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=dust;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
}
