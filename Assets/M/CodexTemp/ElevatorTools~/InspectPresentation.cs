using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectWait.M.Elevator;
public static class InspectPresentation
{
    public static object Run()
    {
        var l=UnityEngine.Object.FindFirstObjectByType<ElevatorLesson>();
        var eyePoints=new System.Collections.Generic.List<Vector3>();
        foreach(var skin in l.friend.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=new Mesh();skin.BakeMesh(mesh);var vertices=mesh.vertices;
            for(int i=0;i<skin.sharedMaterials.Length;i++)if(skin.sharedMaterials[i]&&skin.sharedMaterials[i].name.Contains("Eyes"))
                foreach(int n in mesh.GetIndices(i).Distinct())eyePoints.Add(skin.transform.TransformPoint(vertices[n]));
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        return new{
            eyeHeight=eyePoints.Count>0?(eyePoints.Min(p=>p.y)+eyePoints.Max(p=>p.y))*.5f:0,
            camera=l.playerCamera.transform.position.ToString(),friend=l.friend.position.ToString(),
            eyes=l.friend.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.ToLower().Contains("eye")||r.sharedMaterials.Any(m=>m&&m.name.ToLower().Contains("eyes"))).Select(r=>new{name=r.name,center=r.bounds.center.ToString(),size=r.bounds.size.ToString()}).ToArray(),
            heads=l.friend.GetComponentsInChildren<Transform>(true).Where(t=>t.name.ToLower().Contains("head")||t.name.ToLower().Contains("eye")).Select(t=>new{t.name,position=t.position.ToString()}).ToArray(),
            ui=l.view.GetComponentsInChildren<RectTransform>(true).Take(15).Select(t=>new{t.name,size=t.sizeDelta.ToString(),position=t.anchoredPosition.ToString()}).ToArray()};
    }
}
