using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using DwtD.TestScene;
using Object = UnityEngine.Object;

public static class ReviewStorybookArt
{
    public static async Task<object> Run()
    {
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(!Application.isPlaying || p==null) throw new Exception("Play the corridor first.");
        if(p.CurrentPhase!=WetCorridorPrototype.Phase.Title) throw new Exception("Start a fresh Play session for the art review.");
        var d=Object.FindFirstObjectByType<DesktopTestControls>();
        d.SetMouseLook(false); d.enabled=false;
        var camera=p.playerCamera; var position=camera.transform.position; var rotation=camera.transform.rotation;
        camera.transform.rotation=Quaternion.identity;
        if(p.CurrentPhase==WetCorridorPrototype.Phase.Title) Capture(p,"Art_Title");
        p.Begin();
        p.Story.Inspect(CorridorObservationTarget.Clue.Water);
        await Task.Delay(250);
        Capture(p,"Art_Observation");
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None); var enabled=canvases.Select(c=>c.enabled).ToArray();
        try
        {
            foreach(var canvas in canvases) canvas.enabled=false;
            camera.transform.SetPositionAndRotation(p.friend.position+new Vector3(-.12f,1.40f,-3.0f),Quaternion.identity);
            Capture(p,"Art_Character");
            camera.transform.SetPositionAndRotation(position,Quaternion.identity);
            Capture(p,"Art_Environment");
        }
        finally
        {
            for(int i=0;i<canvases.Length;i++) canvases[i].enabled=enabled[i];
            camera.transform.SetPositionAndRotation(position,rotation);
            d.enabled=true; d.SetMouseLook(false);
        }
        var shader=Shader.Find("ProjectWait/Storybook Toon");
        var shaderErrors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
        var renderers=p.GetComponentsInChildren<MeshRenderer>(true);
        var missingMeshes=renderers.Where(r=>r.GetComponent<MeshFilter>()==null || r.GetComponent<MeshFilter>().sharedMesh==null).Select(r=>r.name).ToArray();
        var brokenMaterials=renderers.Where(r=>r.sharedMaterials.Any(m=>m==null || m.shader==null || !m.shader.isSupported)).Select(r=>r.name).ToArray();
        var report=new {date="2026-10-19",success=shaderErrors.Length==0 && missingMeshes.Length==0 && brokenMaterials.Length==0,
            scene=p.gameObject.scene.path,phase=p.CurrentPhase.ToString(),shaderErrors,missingMeshes,brokenMaterials,
            graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),renderers=renderers.Length,
            typographyPreset=p.storybookTextMaterial.name,
            limitations=new[]{"Desktop rendering reviewed. Physical headset, stereo comfort, Android player build and device performance not verified."}};
        File.WriteAllText("Test/ArtReview.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return new {report.date,report.success,report.phase,report.shaderErrors,report.missingMeshes,report.brokenMaterials,captures=new[]{"Test/Art_Title.png","Test/Art_Observation.png","Test/Art_Character.png","Test/Art_Environment.png"}};
    }

    static void Capture(WetCorridorPrototype p,string name)
    {
        Canvas.ForceUpdateCanvases(); p.thoughtBubble.FollowView();
        var camera=p.playerCamera; var previous=camera.targetTexture; var active=RenderTexture.active;
        var rt=new RenderTexture(1600,1000,24){antiAliasing=4};
        var resolved=new RenderTexture(1600,1000,0); var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt; p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();camera.Render(); Graphics.Blit(rt,resolved); RenderTexture.active=resolved;
            image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply(); File.WriteAllBytes("Test/"+name+".png",image.EncodeToPNG());
        }
        finally {camera.targetTexture=previous;RenderTexture.active=active;rt.Release();resolved.Release();Object.Destroy(rt);Object.Destroy(resolved);Object.Destroy(image);}
    }
}
