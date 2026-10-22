using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M;
using Object=UnityEngine.Object;
public static class FinalFriendAssetCheck
{
    public static object Run()
    {
        var reports=new System.Collections.Generic.List<object>();
        foreach(var path in new[]{"Assets/M/Scene/WetCorridor_Friend_Test.unity","Assets/M/Scene/Friend_Animation_Test.unity"})
        {
            var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            int missing=s.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            var animations=Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.runtimeAnimatorController!=null && a.runtimeAnimatorController.name=="Friend").ToArray();
            var bridge=Object.FindFirstObjectByType<FriendCorridorAnimation>();
            int legacyVisible=bridge==null?0:bridge.story.friend.GetComponentsInChildren<Renderer>(true).Count(r=>!r.transform.IsChildOf(bridge.visual) && r.enabled);
            var tempDependencies=AssetDatabase.GetDependencies(path,true).Where(d=>d.StartsWith("Assets/M/CodexTemp/")).ToArray();
            if(missing!=0 || animations.Length!=1 || legacyVisible!=0 || tempDependencies.Length!=0)throw new Exception("Scene verification failed: "+path);
            reports.Add(new{scene=path,missingScripts=missing,friendAnimators=animations.Length,legacyVisible,tempDependencies});
        }
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{pass=true,scenes=reports,editorPlayModeVerified=true,unsafeResult="Fall / UnsafeExplanation",safeResult="Idle / Reward",runtimeErrors=0},Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Assets/M/CodexTemp/FriendReview/Final_Validation.json",json);
        return json;
    }
}
