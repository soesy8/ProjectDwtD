using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using DwtD.TestScene;
public static class InspectFriendUnity
{
    public static object Run()
    {
        var owners=Object.FindObjectsByType<WetCorridorPrototype>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var lines=new System.Collections.Generic.List<string>();
        lines.Add("Playing="+EditorApplication.isPlaying);
        for(int i=0;i<SceneManager.sceneCount;i++) {var s=SceneManager.GetSceneAt(i);lines.Add("Scene="+s.path+" Dirty="+s.isDirty);}
        foreach(var o in owners) lines.Add("Actor="+o.friend.name+" Position="+o.friend.position+" Scale="+o.friend.localScale+" Bounds="+o.friend.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(o.friend.position,Vector3.zero),(b,r)=>{b.Encapsulate(r.bounds);return b;}).size+" Camera="+o.playerCamera.transform.position+" CameraEuler="+o.playerCamera.transform.eulerAngles);
        AssetDatabase.Refresh();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/M/Arts/Models/Friend/Wait_Friend_Edited.fbx");
        if(model!=null) foreach(var t in model.GetComponentsInChildren<Transform>()) lines.Add("FBX="+t.name+" localP="+t.localPosition+" localR="+t.localEulerAngles+" Scale="+t.localScale);
        return string.Join("\n",lines);
    }
}
