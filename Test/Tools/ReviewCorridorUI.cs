using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using DwtD.TestScene;
using Object=UnityEngine.Object;

public static class ReviewCorridorUI
{
    public static async Task<object> Run()
    {
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(!Application.isPlaying || p==null || p.CurrentPhase!=WetCorridorPrototype.Phase.Title) throw new Exception("Start a fresh corridor Play session.");
        var desktop=Object.FindFirstObjectByType<DesktopTestControls>();
        var camera=p.playerCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;
        bool previousLook=desktop.MouseLookActive;
        var checks=new List<string>();var errors=new List<string>();
        bool success=false;string failure=null;
        void Check(bool valid,string message) { if(!valid) throw new Exception(message);checks.Add(message); }
        void Log(string condition,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(condition); }
        Application.logMessageReceived+=Log;
        desktop.SetMouseLook(false);desktop.enabled=false;
        try
        {
            Capture(p,"Title");p.Begin();p.Story.Inspect(CorridorObservationTarget.Clue.Water);
            var friendPosition=p.friend.position;
            try
            {
                foreach(float aspect in new[]{16f/9f,1.6f,4f/3f})
                {
                    camera.aspect=aspect;
                    foreach(var panel in Object.FindObjectsByType<CorridorWorldPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)) panel.RefreshLayout();
                    foreach(var actorPosition in new[]{friendPosition,CorridorComposition.SafePass,CorridorComposition.SafeEnd})
                    {
                        p.friend.position=actorPosition;p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();
                        var speech=Bounds((RectTransform)p.bubble.transform.parent,camera);
                        var header=p.heading.GetComponentInParent<Canvas>(true);
                        var thought=Bounds(p.thoughtBubble.bubbleRoot,camera);
                        Check(!header.gameObject.activeInHierarchy,"Situation header stays hidden while observing at aspect "+aspect.ToString("0.00"));
                        Check(!speech.Overlaps(thought),"Dialogue avoids inner thought at aspect "+aspect.ToString("0.00")+" and distance "+(actorPosition.z+5).ToString("0.00"));
                    }
                }
            }
            finally {p.friend.position=friendPosition;camera.ResetAspect();p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();}
            await Task.Delay(100);Capture(p,"Observation");
            var hint=GameObject.Find("Hint").GetComponent<UnityEngine.UI.Button>();
            var hintRotation=camera.transform.rotation;
            camera.transform.LookAt(hint.transform.position);desktop.enabled=true;desktop.SetMouseLook(false);desktop.SendMessage("UpdateCenterRay");
            Check(desktop.HoverTargetName==hint.name,"Repositioned hint remains reachable by the desktop center ray");
            desktop.enabled=false;camera.transform.rotation=hintRotation;
            p.OpenChoices();Capture(p,"Choices");
            var rayRotation=camera.transform.rotation;
            camera.transform.LookAt(p.stopButton.transform.position);
            desktop.enabled=true;desktop.SetMouseLook(false);desktop.SendMessage("UpdateCenterRay");
            Check(desktop.centerLaser.enabled && desktop.laserDot.gameObject.activeInHierarchy,"Re-enabling the controller restores both selection beam and dot");
            Check(desktop.HoverTargetName==p.stopButton.name,"Restored laser still hits the real choice button");
            Capture(p,"Laser",true);desktop.enabled=false;camera.transform.rotation=rayRotation;
            var label=p.stopButton.GetComponentInChildren<TMPro.TMP_Text>().rectTransform;
            var resting=label.anchoredPosition;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(p.stopButton.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            Check(label.anchoredPosition.y>resting.y,"Hover raises button content without changing its hit rectangle");Capture(p,"Hover");
            ExecuteEvents.Execute(p.stopButton.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Check(label.anchoredPosition.y<resting.y,"Press recesses the face and content");Capture(p,"Pressed");
            ExecuteEvents.Execute(p.stopButton.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(p.stopButton.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            EventSystem.current.SetSelectedGameObject(null);
            Check(Vector2.Distance(label.anchoredPosition,resting)<.01f,"Release, exit and deselection restore the original content position");
            var choiceFaces=new[]{p.hurryButton,p.stopButton,p.waveButton}.Select(b=>b.targetGraphic.color).ToArray();
            Check(choiceFaces.All(c=>c==choiceFaces[0]),"All three choices use the same face color");
            p.StopFriend();Capture(p,"DryPath");p.ChooseDryPath();
            float timeout=Time.realtimeSinceStartup+14;
            while(p.CurrentPhase!=WetCorridorPrototype.Phase.Reward && Time.realtimeSinceStartup<timeout) await Task.Delay(50);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Reward,"Styled route controls reach the reward");Capture(p,"Reward");
            p.finalFeedback.gameObject.SetActive(true);p.CollectStamp();Capture(p,"Question");p.Answer(0);
            await Task.Delay(50);Capture(p,"AnswerFeedback");
            Check(!p.finalFeedback.answerA.interactable && !p.finalFeedback.answerB.interactable,"Answered options retain their disabled state");
            p.finalFeedback.gameObject.SetActive(false);
            var stampGraphic=p.stampMark.GetComponent<CorridorSymbolGraphic>();
            Check(stampGraphic.color==CorridorUITheme.Teal && p.StampCount==1,"Earned stamp remains visible in the notebook style");Capture(p,"NotebookReward");
            Check(errors.Count==0,"No UI mesh or event errors during the review");success=true;
        }
        catch(Exception ex) { failure=ex.Message; }
        finally
        {
            Application.logMessageReceived-=Log;p.finalFeedback.gameObject.SetActive(false);
            camera.transform.SetPositionAndRotation(position,rotation);desktop.enabled=true;desktop.SetMouseLook(previousLook && Application.isFocused);
        }
        var report=new {date=DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd"),success,failure,checks,errors,
            scene=p.gameObject.scene.path,limitations=new[]{"Desktop UI rendering and pointer events checked; physical headset not tested."}};
        File.WriteAllText("Test/UIReview.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
        foreach(var corner in corners) { var point=(Vector2)camera.WorldToViewportPoint(corner);min=Vector2.Min(min,point);max=Vector2.Max(max,point); }
        return Rect.MinMaxRect(min.x-.006f,min.y-.006f,max.x+.006f,max.y+.006f);
    }
    static void Capture(WetCorridorPrototype p,string name,bool keepView=false)
    {
        var camera=p.playerCamera;if(!keepView) camera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);
        Canvas.ForceUpdateCanvases();p.thoughtBubble.FollowView();
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1600,1000,24){antiAliasing=4};var resolved=new RenderTexture(1600,1000,0);
        var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;
            foreach(var panel in Object.FindObjectsByType<CorridorWorldPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)) panel.RefreshLayout();
            p.thoughtBubble.FollowView();p.Story.RefreshSpeechLayout();camera.Render();Graphics.Blit(target,resolved);RenderTexture.active=resolved;
            texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes("Test/UI_"+name+".png",texture.EncodeToPNG());
        }
        finally {camera.targetTexture=previous;RenderTexture.active=active;target.Release();resolved.Release();Object.Destroy(target);Object.Destroy(resolved);Object.Destroy(texture);}
    }
}
