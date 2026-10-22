using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using ProjectWait.M;
using Object=UnityEngine.Object;
public static class FinalizeFriend
{
    public static object Run()
    {
        const string fontPath="Assets/M/Arts/Materials/Friend/FriendPreviewKorean.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        font.material.SetFloat("_FaceDilate",.18f);
        EditorUtility.SetDirty(font.material);AssetDatabase.SaveAssetIfDirty(font.material);
        foreach(var path in new[]{"Assets/M/Scene/WetCorridor_Friend_Test.unity","Assets/M/Scene/Friend_Animation_Test.unity"})
        {
            EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var visual=Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>a.runtimeAnimatorController!=null && a.runtimeAnimatorController.name=="Friend").transform;
            visual.position=new Vector3(visual.position.x,.05f,visual.position.z);
            var story=Object.FindFirstObjectByType<DwtD.TestScene.WetCorridorPrototype>();
            if(story!=null) foreach(var r in story.friend.GetComponentsInChildren<Renderer>(true))
                if(!r.transform.IsChildOf(visual))r.enabled=false;
            var demo=Object.FindFirstObjectByType<FriendAnimationPreview>();
            if(demo!=null)
            {
                foreach(var text in GameObject.Find("Friend_Preview_UI").GetComponentsInChildren<TMP_Text>())text.fontStyle=FontStyles.Bold;
                var card=(RectTransform)GameObject.Find("Paper_Card").transform;card.sizeDelta=new Vector2(650,180);
                var row=(RectTransform)card.Find("Buttons");row.sizeDelta=new Vector2(606,54);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        return "Readable Korean controls and floor height saved in both M scenes.";
    }
}
