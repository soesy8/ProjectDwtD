#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using ProjectWait.M.Elevator;

namespace ProjectWait.M.Editor
{
    public static class ElevatorPresentationSetup
    {
        public const float FriendEyeHeight=1.1064756f;
        [MenuItem("Wait/M/Update Elevator Presentation")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode before updating the saved scene.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ElevatorPrototypeBuilder.ScenePath)throw new Exception("Open the M elevator scene first.");
            if(scene.isDirty)throw new Exception("Preserve unsaved changes before applying presentation updates.");
            var lesson=UnityEngine.Object.FindFirstObjectByType<ElevatorLesson>();
            Configure(lesson);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        }
        public static void Configure(ElevatorLesson lesson)
        {
            var controls=UnityEngine.Object.FindFirstObjectByType<ElevatorDesktopControls>();
            var origin=controls.xrOrigin.GetComponent<XROrigin>();origin.CameraYOffset=FriendEyeHeight;
            controls.eyeHeight=FriendEyeHeight;
            controls.targetCamera.transform.position=new Vector3(controls.xrOrigin.position.x,FriendEyeHeight,controls.xrOrigin.position.z);
            controls.targetCamera.transform.rotation=Quaternion.identity;
            string path="Assets/M/Arts/Materials/Elevator/DesktopSelectionRay.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(1,.67f,.20f));
                AssetDatabase.CreateAsset(material,path);
            }
            if(!controls.centerLaser)
            {
                var line=new GameObject("Desktop_Center_Selection_Ray").AddComponent<LineRenderer>();line.transform.SetParent(controls.transform,false);controls.centerLaser=line;
            }
            controls.centerLaser.positionCount=2;controls.centerLaser.useWorldSpace=true;
            controls.centerLaser.widthMultiplier=.0012f;controls.centerLaser.numCapVertices=4;
            controls.centerLaser.sharedMaterial=material;controls.centerLaser.enabled=false;
            if(!controls.laserDot)
            {
                var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Center_Selection_Dot";dot.transform.SetParent(controls.transform,false);
                UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());controls.laserDot=dot.transform;
            }
            controls.laserDot.GetComponent<Renderer>().sharedMaterial=material;controls.laserDot.gameObject.SetActive(false);
            controls.laserDot.localScale=Vector3.one*.012f;
            lesson.view.PrepareCompactPresentation();lesson.view.Reanchor(controls.targetCamera.transform.position);
            EditorUtility.SetDirty(controls);EditorUtility.SetDirty(origin);EditorUtility.SetDirty(lesson.view);
        }
    }
}
#endif
