using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DeadMemoryRealityValidation
{
    class Triangle {public int owner,axis,side;public float plane;public Vector2[] p;public Rect bounds;public string name;}
    static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
    static float Area(List<Vector2> polygon)
    {float a=0;for(int i=0;i<polygon.Count;i++)a+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);return Mathf.Abs(a)*.5f;}
    static float Intersection(Vector2[] first,Vector2[] clip)
    {
        var polygon=first.ToList();float sign=Mathf.Sign(Cross(clip[1]-clip[0],clip[2]-clip[0]));
        for(int edge=0;edge<3&&polygon.Count>0;edge++)
        {
            Vector2 a=clip[edge],b=clip[(edge+1)%3];var output=new List<Vector2>();
            Vector2 previous=polygon[polygon.Count-1];float pd=sign*Cross(b-a,previous-a);
            foreach(var point in polygon)
            {
                float cd=sign*Cross(b-a,point-a);bool inside=cd>=-0.00001f,wasInside=pd>=-0.00001f;
                if(inside!=wasInside){float t=pd/(pd-cd);output.Add(Vector2.Lerp(previous,point,t));}
                if(inside)output.Add(point);previous=point;pd=cd;
            }
            polygon=output;
        }
        return polygon.Count>=3?Area(polygon):0;
    }
    public static string Run()
    {
        var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var triangles=new List<Triangle>();int meshes=0;
        foreach(var r in renderers)
        {
            var mf=r.GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null||!mf.sharedMesh.name.StartsWith("Coplanar-clean"))continue;
            meshes++;var mesh=mf.sharedMesh;var vertices=mesh.vertices;var indexes=mesh.triangles;
            for(int i=0;i<indexes.Length;i+=3)
            {
                Vector3 a=r.transform.TransformPoint(vertices[indexes[i]]),b=r.transform.TransformPoint(vertices[indexes[i+1]]),c=r.transform.TransformPoint(vertices[indexes[i+2]]);
                Vector3 n=Vector3.Cross(b-a,c-a).normalized;int axis=Mathf.Abs(n.x)>.999f?0:Mathf.Abs(n.y)>.999f?1:Mathf.Abs(n.z)>.999f?2:-1;
                if(axis<0)continue;int u=(axis+1)%3,v=(axis+2)%3;var points=new[]{new Vector2(a[u],a[v]),new Vector2(b[u],b[v]),new Vector2(c[u],c[v])};
                triangles.Add(new Triangle{owner=r.GetInstanceID(),axis=axis,side=n[axis]>0?1:0,plane=a[axis],p=points,name=DeadMemoryRealityPass.PathOf(r.transform),bounds=Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y))});
            }
        }
        var buckets=new Dictionary<string,List<Triangle>>();var overlaps=new List<string>();int comparisons=0;
        foreach(var triangle in triangles)
        {
            int coordinate=Mathf.RoundToInt(triangle.plane*1000);
            for(int k=-2;k<=2;k++)
            {
                List<Triangle> previous;if(!buckets.TryGetValue(triangle.axis+"/"+triangle.side+"/"+(coordinate+k),out previous))continue;
                foreach(var other in previous)
                {
                    if(other.owner==triangle.owner||Mathf.Abs(other.plane-triangle.plane)>=.0015f||!triangle.bounds.Overlaps(other.bounds))continue;
                    comparisons++;float area=Intersection(triangle.p,other.p);
                    if(area>.0001f)overlaps.Add(triangle.name+" <> "+other.name+" / area="+area.ToString("F6"));
                }
            }
            string key=triangle.axis+"/"+triangle.side+"/"+coordinate;List<Triangle> group;if(!buckets.TryGetValue(key,out group)){group=new List<Triangle>();buckets.Add(key,group);}group.Add(triangle);
        }
        int supported=0;var floating=new List<string>();
        var casters=renderers.Where(r=>r.name=="Office chair caster").ToArray();
        foreach(var group in DeadMemoryRealityPass.FurnitureGroups())
        {
            Bounds bounds=group[0].bounds;foreach(var r in group.Skip(1))bounds.Encapsulate(r.bounds);float floor;
            if(!DeadMemoryRealityPass.FloorBelow(bounds,out floor))continue;
            float gap=bounds.min.y-floor;
            bool caster=casters.Any(r=>r.bounds.min.y<floor+.01f&&r.bounds.max.y>=bounds.min.y-.01f&&r.bounds.max.x>=bounds.min.x&&r.bounds.min.x<=bounds.max.x&&r.bounds.max.z>=bounds.min.z&&r.bounds.min.z<=bounds.max.z);
            if(Mathf.Abs(gap)<=.008f||caster)supported++;else if(gap>.008f)floating.Add(group[0].name+" / gap="+gap.ToString("F3")+" / parts="+group.Count);
        }
        var report=new List<string>{"Reality pass static inspection","Processed static cuboid meshes: "+meshes,"Axis-aligned triangles inspected: "+triangles.Count,"Triangle intersection comparisons: "+comparisons,"Coplanar overlap pairs > 0.0001 square metres within 1.5 mm: "+overlaps.Count,"Supported furniture contact groups: "+supported,"Unresolved furniture/component contact groups: "+floating.Count};
        report.AddRange(overlaps.Take(60));report.Add("FURNITURE CONTACT DETAILS");report.AddRange(floating);
        File.WriteAllLines(DeadMemoryRealityPass.Folder+"Static_Validation.txt",report);return string.Join("\n",report.Take(8));
    }
}
