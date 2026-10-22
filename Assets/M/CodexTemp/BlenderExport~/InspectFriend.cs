using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class InspectFriendForBlender
{
    public static object Run()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Test/Scenes/WetCorridor_Test.unity");
        try
        {
            var friend = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Friend_Milo");
            return new {
                source = scene.path,
                rootPosition = friend.position.ToString(), rootScale = friend.localScale.ToString(),
                parts = friend.GetComponentsInChildren<Transform>(true).Select(t => new {
                    name = t.name, parent = t == friend ? null : t.parent.name,
                    active = t.gameObject.activeInHierarchy,
                    components = t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray(),
                    mesh = t.GetComponent<MeshFilter>() != null && t.GetComponent<MeshFilter>().sharedMesh != null ? t.GetComponent<MeshFilter>().sharedMesh.name : null,
                    materials = t.GetComponent<Renderer>() == null ? null : t.GetComponent<Renderer>().sharedMaterials.Select(m => m == null ? null : m.name).ToArray()
                }).ToArray()
            };
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
