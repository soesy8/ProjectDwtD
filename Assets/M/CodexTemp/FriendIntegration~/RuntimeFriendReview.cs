using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DwtD.TestScene;
using ProjectWait.M;
using Object=UnityEngine.Object;
public static class RuntimeFriendReview
{
    public static object OpenStory(){EditorSceneManager.OpenScene("Assets/M/Scene/WetCorridor_Friend_Test.unity",OpenSceneMode.Single);return "Story scene ready";}
    public static object OpenPreview(){EditorSceneManager.OpenScene("Assets/M/Scene/Friend_Animation_Test.unity",OpenSceneMode.Single);return "Preview scene ready";}
    public static object Unsafe(){var s=Object.FindFirstObjectByType<WetCorridorPrototype>();s.Begin();s.OpenChoices();s.ChooseUnsafe(true);return Read();}
    public static object Safe(){var s=Object.FindFirstObjectByType<WetCorridorPrototype>();s.Retry();s.StopFriend();s.ChooseDryPath();return Read();}
    public static object Read()
    {
        var bridge=Object.FindFirstObjectByType<FriendCorridorAnimation>();
        var demo=Object.FindFirstObjectByType<FriendAnimationPreview>();
        var a=bridge!=null?bridge.actor:demo.actor;
        var s=Object.FindFirstObjectByType<WetCorridorPrototype>();
        var state=a.GetCurrentAnimatorStateInfo(0);
        var result=new{frame=Time.frameCount,time=Time.time,phase=s==null?"Preview":s.CurrentPhase.ToString(),motion=bridge!=null?bridge.CurrentMotion:demo.CurrentMotion,animatorState=state.shortNameHash,normalizedTime=state.normalizedTime};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(result);
        string path="Assets/M/CodexTemp/FriendReview/Runtime_"+result.phase+".json";File.WriteAllText(path,json);
        return json;
    }
    public static object ClickWalk(){var b=GameObject.Find("Friend_Preview_UI/Paper_Card/Buttons/Walk").GetComponent<UnityEngine.UI.Button>();b.onClick.Invoke();return Read();}
    public static object ClickFall(){var b=GameObject.Find("Friend_Preview_UI/Paper_Card/Buttons/Fall").GetComponent<UnityEngine.UI.Button>();b.onClick.Invoke();return Read();}
}
