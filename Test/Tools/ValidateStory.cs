using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DwtD.TestScene;
using UnityEngine;

// Run through Unity Pipeline run_script; kept outside Assets to avoid build dependencies.
public static class ValidateStory
{
    public static async Task<object> Run()
    {
        var checks=new List<string>();
        var errors=new List<string>();
        var p=UnityEngine.Object.FindFirstObjectByType<WetCorridorPrototype>();
        var d=UnityEngine.Object.FindFirstObjectByType<DesktopTestControls>();
        if(p==null || d==null || !Application.isPlaying) throw new Exception("Open the corridor scene in Play mode first.");
        var feedback=p.finalFeedback;
        var thoughts=p.thoughtBubble;
        var startingCameraPosition=p.playerCamera.transform.position;
        void Check(bool valid,string name) { if(!valid) throw new Exception(name); checks.Add(name); }
        void Log(string condition,string stack,LogType type) { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && !errors.Contains(condition)) errors.Add(condition); }
        void Aim(Transform target,Vector3 offset)
        {
            Canvas.ForceUpdateCanvases();
            p.playerCamera.transform.LookAt(target.position+offset);
            Physics.SyncTransforms();
            d.SendMessage("UpdateCenterRay");
        }
        void Click(UnityEngine.UI.Button button)
        {
            Aim(button.transform,Vector3.zero);
            if(d.HoverTargetName!=button.name) throw new Exception("Center ray did not hit "+button.name+"; hit="+d.HoverTargetName);
            d.SelectCenterTarget();
        }
        async Task Until(WetCorridorPrototype.Phase phase)
        {
            float timeout=Time.realtimeSinceStartup+15;
            while(p.CurrentPhase!=phase && Time.realtimeSinceStartup<timeout) await Task.Delay(50);
            Check(p.CurrentPhase==phase,"Reached "+phase);
        }
        async Task RenderFrames()
        {
            int frame=Time.frameCount;
            float timeout=Time.realtimeSinceStartup+3;
            while(Time.frameCount<frame+2 && Time.realtimeSinceStartup<timeout) await Task.Delay(25);
            if(Time.frameCount<frame+2) throw new Exception("Game frames did not advance for the camera-follow check.");
        }
        void Capture(string name,bool keepView=false)
        {
            if(!keepView) p.playerCamera.transform.rotation=Quaternion.identity;
            Canvas.ForceUpdateCanvases();
            var previous=p.playerCamera.targetTexture;
            var active=RenderTexture.active;
            var rt=new RenderTexture(1600,1000,24);
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                p.playerCamera.targetTexture=rt; thoughts.FollowView(); p.playerCamera.Render(); RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
                File.WriteAllBytes("Test/Story_"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                p.playerCamera.targetTexture=previous; RenderTexture.active=active;
                rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(image);
            }
        }
        Application.logMessageReceived+=Log;
        bool success=false;
        string failure=null;
        try
        {
            d.SetMouseLook(false); d.enabled=false;
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Title,"Fresh story starts on title");
            Check(feedback!=null && !feedback.gameObject.activeSelf && feedback.transform.parent==null,"Final feedback is a separate, inactive scene root");
            Check(thoughts!=null && thoughts.gameObject.activeSelf && thoughts.transform.parent==null,"Thought bubble is a separate, active scene root");
            Check(feedback.questionPanel.transform.IsChildOf(feedback.transform) && feedback.completedPanel.transform.IsChildOf(feedback.transform),"Deferred UI belongs to the feedback object");
            Check(thoughts.bubbleRoot.IsChildOf(thoughts.transform),"Cloud UI belongs to the thought object");
            Capture("Title");
            Click(p.startButton);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Observing,"Start button opens observation before choices");
            var water=GameObject.Find("Observe_Water").GetComponent<CorridorObservationTarget>();
            Aim(water.transform,Vector3.zero);
            Check(p.ObservedWater && water.outline.enabled,"Center ray highlights water and shows the thought");
            Check(p.Story.ThoughtText.Contains("미끄럽"),"Water thought explains the visible condition");
            var camera=p.playerCamera;
            var originalRotation=camera.transform.rotation;
            var originalPosition=camera.transform.position;
            foreach(var rotation in new[]{Quaternion.Euler(0,-75,0),Quaternion.Euler(32,48,0),Quaternion.Euler(-25,20,15)})
            {
                camera.transform.SetPositionAndRotation(originalPosition+new Vector3(.15f,.1f,0),rotation);
                await RenderFrames();
                var anchor=camera.WorldToViewportPoint(thoughts.transform.position);
                Check(Vector2.Distance(new Vector2(anchor.x,anchor.y),thoughts.viewportAnchor)<.002f && Mathf.Abs(anchor.z-thoughts.viewDistance)<.01f,"Cloud retains viewport position after view rotation and tracking movement");
                Check(Quaternion.Angle(camera.transform.rotation,thoughts.transform.rotation)<.1f,"Cloud faces the same direction as the player's view");
            }
            Capture("ThoughtFollow",true);
            camera.transform.SetPositionAndRotation(originalPosition,originalRotation);
            thoughts.gameObject.SetActive(false);
            p.Story.Inspect(CorridorObservationTarget.Clue.Water);
            Check(!thoughts.bubbleRoot.gameObject.activeInHierarchy && p.CanObserve,"Disabling thought object hides UI without stopping observation");
            thoughts.gameObject.SetActive(true);
            Check(thoughts.bubbleRoot.gameObject.activeInHierarchy,"Re-enabling thought object restores current thought");
            Aim(water.transform,Vector3.zero);
            await Task.Delay(700);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Observing && p.StampCount==0,"Observation and waiting do not execute an action or award a stamp");
            foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(label.font==p.heading.font)
                    Check(label.text.Where(c=>!char.IsControl(c)).All(c=>label.font.HasCharacter(c)),"Font covers "+label.name);
            Capture("Observation",true);
            var choices=GameObject.Find("OpenSignalChoices").GetComponent<UnityEngine.UI.Button>();
            Click(choices);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Choosing,"Explicit continue opens the three actions");
            Capture("Choices");
            Click(p.stopButton);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.WaitingForPath,"Stop signal waits for a route");
            var route=UnityEngine.Object.FindFirstObjectByType<CorridorActionTarget>();
            Aim(route.transform,Vector3.zero); d.SelectCenterTarget();
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.SafeMotion,"Dry-floor collider accepts center-ray selection");
            await Until(WetCorridorPrototype.Phase.Reward);
            Check(!p.WasGuided && !p.UsedPathHint && p.UnsafeAttempts==0,"Safe-first route is unassisted and does not force an incident");
            Capture("Reward");
            Click(p.stampButton);
            Check(p.StampCount==1,"One safe completion earns one stamp");
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Completed && !feedback.gameObject.activeSelf,"Inactive final feedback is skipped after collecting the stamp");
            Check(!feedback.questionPanel.activeInHierarchy && !feedback.completedPanel.activeInHierarchy && p.CauseFirstAnswer==-1,"Deferred questions and explanation stay hidden and unanswered");
            p.Answer(0); p.NextQuestion();
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Completed && p.CauseFirstAnswer==-1,"Keyboard/API callbacks cannot enter inactive feedback");
            Capture("Completed");
            Click(p.retryButton);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Observing && !p.ObservedWater,"Replay resets observation while preserving the stamp");
            p.RequestHint(); p.RequestHint();
            Check(p.HintCount==2 && p.WasGuided && p.CurrentPhase==WetCorridorPrototype.Phase.Observing,"Hints are recorded without selecting an action");
            p.OpenChoices(); Click(p.hurryButton);
            await Until(WetCorridorPrototype.Phase.UnsafeExplanation);
            Capture("Retry");
            Check(p.StampCount==1 && Time.timeScale==1,"Incident preserves stamp and tracking time scale");
            Click(p.retryButton);
            Check(p.RetryCount==1 && p.CurrentPhase==WetCorridorPrototype.Phase.Choosing,"Retry restores the decision point");
            Click(p.waveButton); await Until(WetCorridorPrototype.Phase.UnsafeExplanation); Click(p.retryButton);
            Check(p.UnsafeAttempts==2 && p.RetryCount==2,"Both unsafe signals finish and support retry");
            feedback.gameObject.SetActive(true);
            Check(!feedback.questionPanel.activeInHierarchy && !feedback.completedPanel.activeInHierarchy,"Enabling the preserved feature does not open it early");
            p.RequestHint(); Click(p.stopButton); p.RequestHint(); Click(p.pathButton); await Until(WetCorridorPrototype.Phase.Reward);
            Click(p.stampButton); p.CollectStamp();
            Check(p.StampCount==1 && p.UsedPathHint,"Guided replay does not duplicate or remove the stamp");
            Check(!p.causeHighlight.activeSelf,"Hint emphasis clears before learning questions");
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.CauseCheck && feedback.questionPanel.activeInHierarchy,"Re-enabled feedback resumes its original post-stamp entry point");
            Click(feedback.answerB); Click(feedback.questionNextButton);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.TransferCheck,"Incorrect cause answer still permits transfer question");
            Check(!GameObject.FindObjectsByType<CorridorObservationTarget>(FindObjectsSortMode.None).Any(),"Transfer question hides observation targets and guidance");
            Click(feedback.answerA); Click(feedback.questionNextButton);
            Check(p.CurrentPhase==WetCorridorPrototype.Phase.Completed && p.StampCount==1 && feedback.completedPanel.activeInHierarchy,"Preserved feedback completes and keeps the stamp after incorrect answers");
            feedback.gameObject.SetActive(false);
            Check(!feedback.completedPanel.activeInHierarchy && p.retryPanel.activeInHierarchy,"Disabling preserved feedback returns to the simple replay action");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"One EventSystem handles all UI");
            Check(errors.Count==0,"No runtime errors during the validated story flows");
            success=true;
        }
        catch(Exception ex) { failure=ex.Message; }
        finally
        {
            Application.logMessageReceived-=Log;
            p.playerCamera.transform.rotation=Quaternion.identity;
            p.playerCamera.transform.position=startingCameraPosition;
            if(feedback!=null) feedback.gameObject.SetActive(false);
            if(thoughts!=null) thoughts.gameObject.SetActive(true);
            d.enabled=true; d.SetMouseLook(false);
        }
        var report=new { date="2026-10-18", unityVersion=Application.unityVersion, success, failure, checks, errors,
            scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, p.StampCount,p.RetryCount,p.UnsafeAttempts,p.HintCount,
            limitations=new[]{"Desktop center-ray/UI integration checked; physical headset and controller not tested."} };
        File.WriteAllText("Test/StoryValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
