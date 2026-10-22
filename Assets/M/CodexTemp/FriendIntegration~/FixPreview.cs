using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M;
using Object=UnityEngine.Object;
public static class FixFriendPreview
{
    public static object Run()
    {
        const string prefab="Assets/M/Arts/Models/Friend/Wait_Friend.prefab";
        var contents=PrefabUtility.LoadPrefabContents(prefab);contents.GetComponent<Animator>().avatar=null;
        PrefabUtility.SaveAsPrefabAsset(contents,prefab);PrefabUtility.UnloadPrefabContents(contents);
        foreach(var path in new[]{"Assets/M/Scene/WetCorridor_Friend_Test.unity","Assets/M/Scene/Friend_Animation_Test.unity"})
        {
            EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            foreach(var a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(a.runtimeAnimatorController!=null && a.runtimeAnimatorController.name=="Friend")a.avatar=null;
            var demo=Object.FindFirstObjectByType<FriendAnimationPreview>();
            if(demo!=null)
            {
                var focus=demo.actor.transform.position+Vector3.up*1.2f;
                demo.previewCamera.transform.position=focus+new Vector3(1.45f,1.1f,-4.6f);
                demo.previewCamera.transform.LookAt(focus);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        return "Generic transform animation uses the prefab hierarchy; preview camera moved inside corridor walls.";
    }
}
