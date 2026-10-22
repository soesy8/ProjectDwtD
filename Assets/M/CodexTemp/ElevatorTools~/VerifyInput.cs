using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ProjectWait.M.Elevator;
using Newtonsoft.Json;
using DwtD.TestScene;

public static class VerifyInput
{
    static ElevatorLesson lesson;
    static ElevatorDesktopControls controls;
    static readonly List<object> checks=new List<object>();
    static void Expect(bool result,string name) {checks.Add(new{name,success=result});if(!result)throw new Exception(name);}
    static async Task Key(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));await Task.Delay(160);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());await Task.Delay(160);
    }
    static async Task Aim(Vector3 target)
    {
        controls.SetMouseLook(true);await Task.Delay(100);
        var e=Quaternion.LookRotation(target-lesson.playerCamera.transform.position).eulerAngles;
        var current=controls.LookAngles;
        var delta=new Vector2(Mathf.DeltaAngle(current.x,e.y)/controls.sensitivity,-Mathf.DeltaAngle(current.y,e.x)/controls.sensitivity);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f),delta=delta});
        await Task.Delay(180);
    }
    static async Task Click()
    {
        var p=new Vector2(Screen.width*.5f,Screen.height*.5f);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left,true));await Task.Delay(160);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});await Task.Delay(160);
    }
    static async Task AimChoice(int n)
    {
        var position=lesson.view.ChoiceButtons[n].transform.position;await Aim(position);
        Expect(Vector3.Distance(position,lesson.view.ChoiceButtons[n].transform.position)<.0001f,"Choice remains stationary while aiming "+n);
        Expect(controls.HoverTargetName==lesson.view.ChoiceButtons[n].name,"Center ray hits choice "+n);
        var dot=lesson.playerCamera.WorldToViewportPoint(controls.laserDot.position);
        Expect(Vector2.Distance(new Vector2(dot.x,dot.y),Vector2.one*.5f)<.002f,"Selection dot stays at view center");
    }
    public static async Task<object> Run()
    {
        checks.Clear();lesson=UnityEngine.Object.FindFirstObjectByType<ElevatorLesson>();controls=UnityEngine.Object.FindFirstObjectByType<ElevatorDesktopControls>();
        var original=InputSystem.settings;var temporary=UnityEngine.Object.Instantiate(original);
        temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=temporary;
        object report;
        try
        {
            lesson.Restart();controls.SetMouseLook(true);await Task.Delay(200);
            Expect(Mathf.Abs(lesson.playerCamera.transform.position.y-1.1064756f)<.005f,"Player camera matches measured friend eye height");
            Expect(controls.centerLaser.enabled&&controls.laserDot.gameObject.activeInHierarchy,"Desktop selection beam and dot visible");
            Expect(!lesson.view.actionPanel.GetComponent<CorridorBubbleGraphic>().enabled&&!lesson.view.infoPanel.gameObject.activeSelf&&!lesson.view.dialoguePanel.gameObject.activeSelf,"Observation has no large persistent instruction card");
            await Key(UnityEngine.InputSystem.Key.Digit1);Expect(lesson.Flow.Step==ElevatorStep.Proposal,"Digit1 opens proposal");
            await Key(UnityEngine.InputSystem.Key.Digit3);Expect(lesson.Flow.Step==ElevatorStep.Observe,"Digit3 returns to observation");
            await AimChoice(0);await Click();Expect(lesson.Flow.Step==ElevatorStep.Proposal,"Center-laser click reopens proposal");
            await AimChoice(0);await Click();Expect(lesson.Flow.Step==ElevatorStep.Jump,"Center-laser choice selects warning");
            await Task.Delay(4300);Expect(lesson.Flow.Step==ElevatorStep.Stopped,"Real-time landing stops elevator");
            await Key(UnityEngine.InputSystem.Key.R);Expect(lesson.Flow.Step==ElevatorStep.Observe&&lesson.Flow.History.Count==0,"R restarts lesson");
            await Aim(lesson.friend.position+Vector3.up*controls.eyeHeight);
            Expect(controls.HoverTargetName=="Friend_Jump_Root"&&lesson.Thought.Contains("친구"),"Mouse rotation aims at friend and shows thought");
            var thoughtPosition=lesson.playerCamera.WorldToViewportPoint(lesson.view.thoughtPanel.position);
            Expect(Vector2.Distance(new Vector2(thoughtPosition.x,thoughtPosition.y),new Vector2(.17f,.69f))<.005f,"Thought stays readable at the same viewport position while looking around");
            await Aim(new Vector3(1.65f,2.5f,.25f));
            Expect(controls.HoverTargetName==""&&!lesson.view.thoughtPanel.gameObject.activeSelf,"Thought disappears when gaze leaves target");
            await Key(UnityEngine.InputSystem.Key.Escape);Expect(!controls.MouseLookActive&&!controls.centerLaser.enabled,"Escape releases cursor and hides beam");
            var angles=controls.LookAngles;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{delta=new Vector2(90,30)});await Task.Delay(200);
            Expect(Vector2.Distance(angles,controls.LookAngles)<.001f,"Released cursor does not rotate view");
            await Key(UnityEngine.InputSystem.Key.Escape);Expect(controls.MouseLookActive&&controls.centerLaser.enabled,"Escape restores mouse look and beam");
            await Key(UnityEngine.InputSystem.Key.F8);Expect(lesson.view.developerPanel.gameObject.activeSelf,"F8 opens developer controls");
            var stage=lesson.view.developerPanel.Find("Stage4");await Aim(stage.position);await Click();
            Expect(lesson.Flow.Step==ElevatorStep.Call&&lesson.Flow.Prevention==PreventionChoice.Developer,"Center ray selects saved developer stage button");
            await Key(UnityEngine.InputSystem.Key.F8);
            report=new{success=true,passed=checks.Count,completedUtc=DateTime.UtcNow.ToString("o"),mode="Play Mode: Input System mouse delta and center-click events, keyboard, real-time landing; temporary in-memory background-input settings; no physical HMD",checks};
        }
        catch(Exception e){report=new{success=false,checks,error=e.ToString(),state=lesson.Snapshot(),angles=controls.LookAngles.ToString(),hover=controls.HoverTargetName};}
        finally
        {
            InputSystem.settings=original;UnityEngine.Object.Destroy(temporary);lesson.Restart();controls.SetMouseLook(false);
        }
        File.WriteAllText("Assets/M/CodexTemp/ElevatorQA/InputVerification.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}

