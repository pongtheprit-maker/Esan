using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DeadMemoryRealityFinishing
{
    static string Blocker(SchoolDoor door, Vector3 origin, float angle)
    {
        var leaf=door.leaf;var col=leaf.GetComponent<BoxCollider>();
        Quaternion turn=door.transform.rotation*Quaternion.Euler(0,angle,0);
        Vector3 offset=door.transform.InverseTransformPoint(leaf.position);
        Vector3 position=origin+turn*Vector3.Scale(offset,door.transform.lossyScale);
        Quaternion rotation=turn*(Quaternion.Inverse(door.transform.rotation)*leaf.rotation);
        Vector3 scale=leaf.lossyScale,half=Vector3.Scale(col.size,scale)*.5f;
        half=new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z));
        Vector3 center=position+rotation*Vector3.Scale(col.center,scale);
        foreach(var other in Physics.OverlapBox(center,half+Vector3.one*.002f,rotation,~0,QueryTriggerInteraction.Ignore))
        {
            if(other.transform.IsChildOf(door.transform)||other.GetComponentInParent<FirstPersonController>()!=null)continue;
            Vector3 direction;float depth;
            if(Physics.ComputePenetration(col,position,rotation,other,other.transform.position,other.transform.rotation,out direction,out depth)&&depth>.004f)
                return DeadMemoryRealityPass.PathOf(other.transform)+" ("+depth.ToString("F3")+"m)";
        }
        return null;
    }
    static string Arc(SchoolDoor door,Vector3 origin)
    {
        for(int step=0;step<=88;step++)
        {string collision=Blocker(door,origin,door.openAngle*step/88f);if(collision!=null)return collision;}
        return null;
    }
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var log=new List<string>();Physics.SyncTransforms();
        foreach(var door in UnityEngine.Object.FindObjectsByType<SchoolDoor>(FindObjectsSortMode.None).Where(d=>d.enabled&&d.leaf!=null))
        {
            Vector3 original=door.transform.position;string blocked=Arc(door,original);
            if(blocked==null){log.Add("CLEAR ARC | "+door.name);continue;}
            bool fitted=false;
            for(int step=1;step<=25;step++)
            {
                Vector3 candidate=original+door.transform.right*(Mathf.Sign(door.openAngle)*step*.01f);
                if(Arc(door,candidate)!=null)continue;
                door.transform.position=candidate;EditorUtility.SetDirty(door.transform);Physics.SyncTransforms();
                log.Add("FITTED HINGE | "+door.name+" shift="+(step*.01f).ToString("F2")+"m; previous contact="+blocked);fitted=true;break;
            }
            if(!fitted)log.Add("UNRESOLVED ARC | "+door.name+" | "+blocked);
        }
        int attached=0,unattached=0;
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Peeling plaster scar"||r.name=="Curled paint attached to wall"))
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            if(renderer.name=="Peeling plaster scar"&&mesh.normals.Length>0&&mesh.normals.Average(n=>n.z)<0)
            {
                var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
                mesh.triangles=triangles;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
            }
            Vector3 normal=renderer.transform.forward,position=renderer.transform.position;
            var hit=Physics.RaycastAll(position+normal*.3f,-normal,.85f,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.normal.normalized==normal.normalized||Vector3.Dot(h.normal,normal)>.98f)
                .Where(h=>{var r=h.collider.GetComponent<Renderer>();return DeadMemoryRealityPass.Structural(r);})
                .OrderBy(h=>h.distance).FirstOrDefault();
            if(hit.collider==null){renderer.gameObject.SetActive(false);unattached++;continue;}
            renderer.transform.position=hit.point+normal*.003f;
            if(renderer.name=="Curled paint attached to wall")
            {var s=renderer.transform.localScale;s.z=.3f;renderer.transform.localScale=s;}
            EditorUtility.SetDirty(renderer.transform);attached++;
        }
        log.Add("Decay patches fixed to actual wall/ceiling within 3mm: "+attached+"; unsupported patches at openings deactivated: "+unattached);
        Physics.SyncTransforms();AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(DeadMemoryRealityPass.Folder+"Finishing.txt",log);return string.Join("\n",log);
    }
}
