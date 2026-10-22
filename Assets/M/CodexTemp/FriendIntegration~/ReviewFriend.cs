using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M;
using Object=UnityEngine.Object;
public static class ReviewFriend
{
    const string Out="Assets/M/CodexTemp/FriendReview/";
    public static object Inspect()
    {
        var demo=Object.FindFirstObjectByType<FriendAnimationPreview>();
        var actor=demo.actor;var r=actor.GetComponentInChildren<SkinnedMeshRenderer>();
        var lines=new List<string>();
        lines.Add("Actor scale="+actor.transform.lossyScale+" Renderer scale="+r.transform.lossyScale+" RendererBounds="+r.bounds+" MeshBounds="+r.sharedMesh.bounds+" weights="+r.sharedMesh.boneWeights.Length+" bones="+r.bones.Length);
        var counts=new int[r.bones.Length];foreach(var w in r.sharedMesh.boneWeights){if(w.weight0>0)counts[w.boneIndex0]++;if(w.weight1>0)counts[w.boneIndex1]++;if(w.weight2>0)counts[w.boneIndex2]++;if(w.weight3>0)counts[w.boneIndex3]++;}
        lines.Add("WEIGHTS="+string.Join(",",r.bones.Select((b,i)=>b.name+":"+counts[i])));
        var m=new Mesh();r.BakeMesh(m,false);lines.Add("BakeFalse="+m.bounds);Object.DestroyImmediate(m);
        m=new Mesh();r.BakeMesh(m,true);lines.Add("BakeTrue="+m.bounds);Object.DestroyImmediate(m);
        foreach(var t in actor.GetComponentsInChildren<Transform>()) lines.Add("Path="+AnimationUtility.CalculateTransformPath(t,actor.transform)+" R="+t.localEulerAngles);
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/M/Arts/Animations/Friend/Friend_Fall.anim");
        foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(5))lines.Add("Binding="+b.path+" "+b.propertyName+" Found="+(actor.transform.Find(b.path)!=null));
        actor.avatar=null;actor.Rebind();clip.SampleAnimation(actor.gameObject,1.4f);
        lines.Add("AFTER root="+actor.GetComponentsInChildren<Transform>().First(t=>t.name=="root").localEulerAngles);
        return string.Join("\n",lines);
    }
    public static object Run()
    {
        Directory.CreateDirectory(Out);
        EditorSceneManager.OpenScene("Assets/M/Scene/Friend_Animation_Test.unity",OpenSceneMode.Single);
        var demo=Object.FindFirstObjectByType<FriendAnimationPreview>();
        if(demo==null)throw new Exception("Open the preview scene first");
        foreach(var a in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None)) a.enabled=a.gameObject==demo.previewCamera.gameObject;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var camera=demo.previewCamera;
        var originalTarget=camera.targetTexture;var originalActive=RenderTexture.active;
        var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var info=new List<object>();
        AnimationMode.StartAnimationMode();
        try
        {
            camera.targetTexture=target;
            foreach(var p in new[]{(motion:"Idle",time:.2f),(motion:"Walk",time:.18f),(motion:"Walk",time:.54f),(motion:"Fall",time:.55f),(motion:"Fall",time:1.40f)})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/M/Arts/Animations/Friend/Friend_"+p.motion+".anim");
                AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(demo.actor.gameObject,clip,p.time);AnimationMode.EndSampling();
                float minY=float.MaxValue,maxY=float.MinValue;
                foreach(var r in demo.actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();r.BakeMesh(mesh,true);
                    foreach(var v in mesh.vertices){float y=r.transform.TransformPoint(v).y;minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                    Object.DestroyImmediate(mesh);
                }
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                string name=p.motion+"_"+p.time.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png";
                File.WriteAllBytes(Out+name,texture.EncodeToPNG());
                info.Add(new{motion=p.motion,time=p.time,minY,maxY,capture=Out+name});
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();camera.targetTexture=originalTarget;RenderTexture.active=originalActive;
            target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);
        }
        var renderers=demo.actor.GetComponentsInChildren<Renderer>();
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/FriendStorybookToon.shader");
        var report=new{samples=info,rendererCount=renderers.Length,missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>m==null)),shaderErrors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray()};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Out+"Validation.json",json);
        return json;
    }
}
