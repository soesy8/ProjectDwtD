using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using Newtonsoft.Json;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifyElevator
{
    static ElevatorLesson l;
    static List<object> checks;
    static void Check(string name,bool success)
    {
        checks.Add(new {name,success});
        if(!success)throw new Exception("Verification failed: "+name);
    }
    static void Click(int n) {l.view.InvokeChoice(n);}
    static void Land(){l.Advance(l.settings.friendReplySeconds+l.settings.jumpSeconds+.01f);}
    public static object Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");
        l=Object.FindFirstObjectByType<ElevatorLesson>();checks=new List<object>();
        try
        {
            l.simulateNoResponse=false;l.Restart();
            Check("Fresh run: Observe, clear history and floor 2",l.Flow.Step==ElevatorStep.Observe&&l.Flow.History.Count==0&&l.Floor==2);
            Check("Six neutral observation targets",l.GetComponentsInChildren<ElevatorObservation>().Length==6&&l.GetComponentsInChildren<ElevatorObservation>().Select(o=>o.outline.sharedMaterial).Distinct().Count()==1);
            Check("Exactly one EventSystem",Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1);
            var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            Check("No missing scripts",roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0));
            l.Inspect(ElevatorClue.Display);string before=l.Thought;
            l.Offer();Click(2);Check("Look more returns to observation without a jump",l.Flow.Step==ElevatorStep.Observe&&l.Flow.JumpCount==0);
            l.Advance(120);Check("Look-more waits for player's explicit return",l.Flow.Step==ElevatorStep.Observe);
            Click(0);Check("Proposal can reopen",l.Flow.Step==ElevatorStep.Proposal);
            Click(0);l.Choose(0);Check("Warning accepted once; duplicate choice is ignored",l.Flow.Prevention==PreventionChoice.Warned&&l.Flow.JumpCount==1);
            l.Advance(l.settings.friendReplySeconds+l.settings.jumpSeconds*.5f);
            Check("Friend leaves floor before breakdown; motor still runs",l.friend.localPosition.y>.15f&&l.Flow.Step==ElevatorStep.Jump&&l.motor.isPlaying);
            l.Advance(l.settings.jumpSeconds*.51f);
            Check("Landing causes common breakdown and stops motor",l.Flow.Step==ElevatorStep.Stopped&&!l.motor.isPlaying&&Mathf.Abs(l.friend.localPosition.y)<.001f);
            int floor=l.Floor;l.Advance(l.settings.firstHintSeconds+.01f);
            Check("First hint, no automatic rescue before help",l.Flow.HintLevel==1&&l.Flow.Step==ElevatorStep.Stopped&&!l.Flow.HelpRequested);
            l.Advance(l.settings.secondHintSeconds);Check("Second hint and frozen floor display",l.Flow.HintLevel==2&&l.Flow.Step==ElevatorStep.Stopped&&l.Floor==floor);
            l.Inspect(ElevatorClue.Display);Check("Thought changes after stop",before!=l.Thought&&l.Thought.Contains("정확히"));
            l.UnsafeDoor();Check("Unsafe door action interrupted before execution",l.Flow.Step==ElevatorStep.Retry&&!l.Flow.HelpRequested);
            Click(0);Check("Retry returns to stopped coping, preserves initial warning",l.Flow.Step==ElevatorStep.Stopped&&l.Flow.Prevention==PreventionChoice.Warned&&l.Flow.History.Any(h=>h.Contains("첫 시도")));
            Click(0);l.Intercom();Check("Intercom request starts only one connection",l.Flow.Step==ElevatorStep.Connecting&&l.Flow.History.Count(h=>h.Contains("장치 작동"))==1);
            l.Advance(l.settings.connectionSeconds+.01f);Check("Intercom connects",l.Flow.Step==ElevatorStep.Call&&!l.Flow.UsedPhone);
            l.Advice();Check("Cannot wait for rescue before sharing location",l.Flow.Step==ElevatorStep.Call&&!l.Flow.HelpRequested);
            Click(0);l.Report();Check("Location report is idempotent",l.Flow.LocationReported&&l.Flow.History.Count(h=>h.Contains("갇힘 전달"))==1);
            Click(0);Check("Acknowledged advice starts rescue wait",l.Flow.Step==ElevatorStep.Waiting&&l.Flow.HelpRequested);
            Click(0);Check("Reassure friend",l.Flow.Reassured);
            l.UnsafeExit();Check("Unsafe exit also intercepted after help",l.Flow.Step==ElevatorStep.Retry);
            l.Retry();Check("Retry preserves request and reassurance",l.Flow.Step==ElevatorStep.Waiting&&l.Flow.HelpRequested&&l.Flow.Reassured);
            l.Advance(l.settings.rescueWaitSeconds+.01f);Check("Rescue follows requested wait",l.Flow.Step==ElevatorStep.Rescue);
            l.Advance(l.settings.rescueSubtitleSeconds+.01f);Check("Warning route completes with separate prevention record",l.Flow.Step==ElevatorStep.Complete&&l.Flow.Prevention==PreventionChoice.Warned);
            var warned=l.Snapshot();
            l.Restart();l.Offer();Click(1);Land();
            Check("Agreement route has same event and different initial judgment",l.Flow.Step==ElevatorStep.Stopped&&l.Flow.Prevention==PreventionChoice.Agreed&&l.Flow.History.Any(h=>h.Contains("동조")));
            l.Intercom();l.Advance(l.settings.connectionSeconds+.01f);l.Report();l.Advice();l.Advance(l.settings.rescueWaitSeconds+.01f);l.Advance(l.settings.rescueSubtitleSeconds+.01f);
            Check("Agreement does not loop back to force a correct prevention answer",l.Flow.Step==ElevatorStep.Complete&&l.Flow.Prevention==PreventionChoice.Agreed);
            var agreed=l.Snapshot();
            l.simulateNoResponse=true;l.Restart();l.Offer();l.Choose(0);Land();l.Intercom();l.Advance(l.settings.connectionSeconds+.01f);
            Check("No-response branch",l.Flow.Step==ElevatorStep.Unanswered&&!l.Flow.HelpRequested);
            l.Advance(500);Check("No rescue without help in no-response branch",l.Flow.Step==ElevatorStep.Unanswered);
            Click(0);Check("Phone fallback opens simulated 119 dialogue",l.Flow.Step==ElevatorStep.Call&&l.Flow.UsedPhone);
            Click(0);Click(0);Click(0);l.Advance(l.settings.rescueWaitSeconds+.01f);l.Advance(l.settings.rescueSubtitleSeconds+.01f);
            Check("119 fallback completes",l.Flow.Step==ElevatorStep.Complete&&l.Flow.HelpRequested&&l.Flow.LocationReported&&l.Flow.UsedPhone);
            var fallback=l.Snapshot();
            l.simulateNoResponse=false;l.Restart();l.Offer();l.Choose(0);Land();l.Intercom();l.Restart();
            Check("Restart clears connection, history, sound and timer",l.Flow.Step==ElevatorStep.Observe&&l.Flow.History.Count==0&&!l.signal.isPlaying&&l.Flow.Elapsed==0&&l.Floor==2);
            l.Advance(l.settings.connectionSeconds+.01f);Check("Old connection cannot fire after reset",l.Flow.Step==ElevatorStep.Observe);
            for(int i=0;i<=6;i++) {l.DeveloperJump(i);Check("Developer stage "+i+" resets previous state",l.Floor==2&&!l.signal.isPlaying&&l.Flow.Elapsed==0&&l.Flow.History.Count==(i==0?0:1));}
            l.Restart();l.Offer();l.Choose(0);l.Advance(l.settings.friendReplySeconds+.3f);l.Restart();Check("Restart mid-jump restores ground and zero jumps",Mathf.Abs(l.friend.localPosition.y)<.001f&&l.Flow.JumpCount==0&&l.Flow.Prevention==PreventionChoice.None);
            l.Restart();
            var report=new {completedUtc=DateTime.UtcNow.ToString("o"),mode="Unity Editor Play Mode, PC, real UI callbacks; timer advances compressed via lesson.Advance; no HMD",passed=checks.Count,checks,warned,agreed,fallback};
            File.WriteAllText("Assets/M/CodexTemp/ElevatorQA/RuntimeVerification.json",JsonConvert.SerializeObject(report,Formatting.Indented));
            return report;
        }
        catch(Exception e)
        {
            File.WriteAllText("Assets/M/CodexTemp/ElevatorQA/RuntimeVerification.json",JsonConvert.SerializeObject(new{checks,error=e.ToString(),snapshot=l.Snapshot()},Formatting.Indented));throw;
        }
    }
}
