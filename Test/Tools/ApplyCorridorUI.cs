using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DwtD.TestScene;
using Object=UnityEngine.Object;

public static class ApplyCorridorUI
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before saving UI styling.");
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(p==null || p.gameObject.scene.path!="Assets/Test/Scenes/WetCorridor_Test.unity") throw new Exception("Open the corridor scene.");
        Undo.SetCurrentGroupName("Apply cel shaded story UI");
        foreach(var root in new[]{p.transform,p.thoughtBubble.transform,p.finalFeedback.transform})
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Apply cel shaded story UI");
        p.friendSpeechHeight=2.78f;
        p.bubble.transform.parent.position=p.friend.position+Vector3.up*p.friendSpeechHeight;
        CorridorUITheme.Apply(p);
        var buttons=p.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Concat(p.finalFeedback.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Distinct().ToArray();
        foreach(var button in buttons) CorridorUITheme.StyleButton(button);
        foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(text.font==p.heading.font && (text.transform.IsChildOf(p.transform) || text.transform.IsChildOf(p.thoughtBubble.transform) || text.transform.IsChildOf(p.finalFeedback.transform)))
                text.fontSharedMaterial=p.storybookTextMaterial;
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        return new {scene=p.gameObject.scene.path,styledButtons=buttons.Length,surfaces=Object.FindObjectsByType<CorridorBubbleGraphic>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length};
    }
}
