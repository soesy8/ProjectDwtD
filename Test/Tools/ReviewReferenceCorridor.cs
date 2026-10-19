using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using TMPro;
using DwtD.TestScene;
using Object=UnityEngine.Object;

public static class ReviewReferenceCorridor
{
    public static async Task<object> Run()
    {
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();var d=Object.FindFirstObjectByType<DesktopTestControls>();
        if(p==null || !Application.isPlaying || p.CurrentPhase!=WetCorridorPrototype.Phase.Title) throw new Exception("Start a fresh corridor Play session.");
        var camera=p.playerCamera;var checks=new List<string>();var errors=new List<string>();var textDetails=new List<object>();
        bool success=false;string failure=null;
        void Check(bool value,string message) {if(!value) throw new Exception(message);checks.Add(message);}
        void Log(string value,string stack,LogType type) {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(value);}
        Application.logMessageReceived+=Log;d.SetMouseLook(false);d.enabled=false;
        void Reflow() {foreach(var panel in Object.FindObjectsByType<CorridorWorldPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)) panel.RefreshLayout();p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();Canvas.ForceUpdateCanvases();}
        void Aim(Vector3 point) {camera.transform.LookAt(point);Physics.SyncTransforms();d.SendMessage("UpdateCenterRay");}
        try
        {
            Check(p.transform.Find("Reference_Corridor").gameObject.activeInHierarchy,"Reference corridor is the active environment");
            var art=p.transform.Find("Reference_Corridor");
            Check(art.Find("Umbrella_Stand")==null,"Umbrellas and their stand are removed");
            Check(p.cleaningToolsClue!=null && p.cleaningToolsClue.Find("Open_Bucket")!=null && p.cleaningToolsClue.Find("String_Mop")!=null,"Bucket and string mop are saved in the scene");
            Check(p.cleaningToolsClue.position.z>CorridorComposition.WaterCenter.z+CorridorComposition.WaterRadius.y,"Cleaning tools sit behind the far edge of the puddle");
            Check(!p.transform.Find("SchoolCorridor").gameObject.activeSelf && !p.transform.Find("Storybook_Art").gameObject.activeSelf,"Previous layouts are preserved and inactive");
            Check(Vector3.Distance(p.friend.position,CorridorComposition.FriendStart)<.01f,"Friend starts at the shared hero position");
            foreach(string shader in new[]{"ProjectWait/Storybook Toon","ProjectWait/Illustrated Water"}) Check(!ShaderUtil.ShaderHasError(Shader.Find(shader)),shader+" compiles without errors");
            p.Begin();camera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);Reflow();
            Check(p.Story.ThoughtText=="어떻게\n지나갈까?","Opening thought matches the new composition");
            var action=GameObject.Find("OpenSignalChoices").GetComponent<UnityEngine.UI.Button>();
            var label=action.GetComponentInChildren<TMP_Text>();label.ForceMeshUpdate();
            textDetails.Add(new{label.text,characters=label.textInfo.characterCount,visible=label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>c.isVisible).Select(c=>c.character.ToString()).ToArray(),size=label.rectTransform.rect.ToString()});
            foreach(float aspect in new[]{16f/9f,1.6f,4f/3f})
            {
                camera.aspect=aspect;Reflow();
                var actionBounds=Bounds((RectTransform)action.transform,camera);var thoughtBounds=Bounds(p.thoughtBubble.bubbleRoot,camera);
                Check(actionBounds.xMin>.01f && actionBounds.xMax<.99f && actionBounds.yMin>.01f,"Action stays entirely in view at aspect "+aspect.ToString("0.00"));
                Check(!actionBounds.Overlaps(thoughtBounds),"Thought and action are separate at aspect "+aspect.ToString("0.00"));
                var position=action.transform.position;camera.transform.rotation=Quaternion.Euler(20,30,0);Reflow();
                Check(Vector3.Distance(position,action.transform.position)<.001f,"Looking does not move the action hit target at aspect "+aspect.ToString("0.00"));
                camera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);
            }
            camera.ResetAspect();Capture(p,"Reference_Composition");
            var cleaning=Object.FindObjectsByType<CorridorObservationTarget>(FindObjectsSortMode.None).Single(c=>c.name=="Observe_CleaningTools");
            Aim(p.cleaningToolsClue.TransformPoint(new Vector3(0,.40f,0)));
            Check(d.HoverTargetName==cleaning.name && cleaning.outline.enabled,"Real center ray reaches and highlights the cleaning tools");
            Check(p.Story.LastClue=="CleaningTools" && p.Story.ThoughtText.Contains("청소"),"Cleaning tools show their contextual thought");
            d.SelectCenterTarget();Check(p.CurrentPhase==WetCorridorPrototype.Phase.Observing && p.StampCount==0,"Cleaning observation does not select an action or award a stamp");
            Capture(p,"Reference_CleaningTools");
            p.thoughtBubble.thought.ForceMeshUpdate();
            Check(p.thoughtBubble.thought.textInfo.lineCount==2 && !p.thoughtBubble.thought.isTextOverflowing,"Cleaning thought fits on two lines inside the cloud");
            var mop=p.cleaningToolsClue.Find("String_Mop");Aim(mop.TransformPoint(new Vector3(0,.9f,0)));
            Check(d.HoverTargetName=="Observe_Mop","Center ray reaches the mop at the left wall");
            Check(mop.position.x< -2.4f && mop.TransformPoint(new Vector3(0,1.5f,0)).x< mop.position.x,"Mop leans toward the left wall");
            d.enabled=true;d.SetMouseLook(false);Check(d.centerLaser.enabled && d.laserDot.gameObject.activeInHierarchy,"Beam and dot restore after disable and re-enable");d.enabled=false;
            Aim(action.transform.position);Check(d.HoverTargetName==action.name,"Action square is reachable by center-ray selection");d.SelectCenterTarget();
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Choosing,"Action square opens the three existing choices");
            Check(!camera.GetComponentInChildren<CorridorFirstPersonHands>().visuals.activeInHierarchy,"Foreground hands clear the choice controls");
            Capture(p,"Reference_Choices");
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) {text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Visible text fits: "+text.name);}
            p.StopFriend();Capture(p,"Reference_DryPath");p.ChooseDryPath();
            float timeout=Time.realtimeSinceStartup+12;while(p.CurrentPhase!=WetCorridorPrototype.Phase.Reward && Time.realtimeSinceStartup<timeout) await Task.Delay(50);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Reward && Vector3.Distance(p.friend.position,CorridorComposition.SafeEnd)<.01f,"Safe route reaches the authored arrival position");
            Capture(p,"Reference_Reward");
            var speech=Bounds((RectTransform)p.bubble.transform.parent,camera);
            Check(speech.yMax<1 && speech.yMin>0,"Arrival dialogue remains inside the view");
            Check(errors.Count==0,"No runtime errors during reference review");success=true;
        }
        catch(Exception ex) {failure=ex.Message;}
        finally {Application.logMessageReceived-=Log;camera.ResetAspect();camera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);d.enabled=true;d.SetMouseLook(false);}
        var report=new{success,failure,checks,errors,textDetails,limitations="Desktop review; physical XR headset and controllers not tested. Water reflections are authored shapes."};
        File.WriteAllText("Test/ReferenceReview.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
        foreach(var c in corners) {var v=(Vector2)camera.WorldToViewportPoint(c);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static void Capture(WetCorridorPrototype p,string name)
    {
        var camera=p.playerCamera;camera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);
        var target=camera.targetTexture;var previous=RenderTexture.active;var rt=new RenderTexture(1672,941,24){antiAliasing=4};var image=new Texture2D(1672,941,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;foreach(var panel in Object.FindObjectsByType<CorridorWorldPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)) panel.RefreshLayout();
            Canvas.ForceUpdateCanvases();p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1672,941),0,0);image.Apply();File.WriteAllBytes("Test/"+name+".png",image.EncodeToPNG());
        }
        finally {camera.targetTexture=target;RenderTexture.active=previous;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
    }
}
