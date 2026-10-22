#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;
using TMPro;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorPrototypeBuilder
    {
        public const string ScenePath="Assets/M/Scene/Elevator_Safety_Prototype.unity";
        const string Shared="Assets/Shared Assets/Models/Friend/";
        static Transform environment;
        static TMP_FontAsset font;
        static ElevatorLesson lesson;
        static Material cream,mint,teal,ink,paper,wood;

        [MenuItem("Wait/M/Build Elevator Prototype")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Preserve unsaved scene changes before building.");
            var originalRig=Object.FindFirstObjectByType<XROrigin>();
            if(!originalRig) throw new Exception("Open Assets/Test/Scenes/WetCorridor_Friend_Test.unity to copy the existing XR rig.");
            Directory.CreateDirectory(Shared+"Materials");Directory.CreateDirectory(Shared+"Animations");Directory.CreateDirectory(Shared+"Source~");
            AssetDatabase.Refresh();
            var friendPrefab=SaveSharedFriend();
            font=BuildFont();
            var settings=AssetDatabase.LoadAssetAtPath<ElevatorSettings>("Assets/M/Scene/ElevatorSettings.asset");
            if(!settings) { settings=ScriptableObject.CreateInstance<ElevatorSettings>();AssetDatabase.CreateAsset(settings,"Assets/M/Scene/ElevatorSettings.asset"); }
            cream=Mat("Cream",new Color32(240,223,192,255),.25f);mint=Mat("Mint",new Color32(157,201,183,255),.35f);
            teal=Mat("Teal",new Color32(71,142,131,255),.6f);ink=Mat("Ink",new Color32(73,51,39,255),.5f);
            paper=Mat("Paper",new Color32(255,246,219,255),.4f);wood=Mat("Floor",new Color32(233,214,175,255),.25f);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var rig=Object.Instantiate(originalRig.gameObject);rig.name="Stationary_XR_Origin";
            rig.transform.position=new Vector3(0,0,-1.9f);rig.transform.rotation=Quaternion.identity;
            foreach(var m in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(m==null) continue;
                string type=m.GetType().FullName;
                if(type.StartsWith("DwtD.TestScene.")) Object.DestroyImmediate(m);
                else if(type.Contains("Locomotion") || type.EndsWith("DynamicMoveProvider")) m.enabled=false;
            }
            var origin=rig.GetComponent<XROrigin>(); origin.CameraYOffset=ElevatorPresentationSetup.FriendEyeHeight;
            var camera=origin.Camera;camera.nearClipPlane=.05f;camera.farClipPlane=30;camera.fieldOfView=60;
            var oldHands=camera.transform.Find("Desktop_Story_Hands");if(oldHands)Object.DestroyImmediate(oldHands.gameObject);
            camera.transform.position=new Vector3(0,ElevatorPresentationSetup.FriendEyeHeight,-1.9f);camera.transform.rotation=Quaternion.identity;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(240,223,192,255);
            var owner=new GameObject("Elevator_Safety_Lesson");lesson=owner.AddComponent<ElevatorLesson>();lesson.settings=settings;lesson.playerCamera=camera;
            environment=new GameObject("Cabin").transform;environment.SetParent(owner.transform);
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(XRUIInputModule));
            events.GetComponent<XRUIInputModule>().enableMouseInput=true;
            var light=new GameObject("Warm cabin light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.color=new Color(1,.94f,.82f);light.intensity=1.6f;light.shadows=LightShadows.Soft;lesson.cabinLight=light;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.78f,.79f,.73f);RenderSettings.skybox=null;
            Box("Floor",new Vector3(0,-.08f,0),new Vector3(3.6f,.16f,4.8f),wood);
            Box("Ceiling",new Vector3(0,2.85f,0),new Vector3(3.6f,.12f,4.8f),cream);
            Box("LeftWall",new Vector3(-1.8f,1.42f,0),new Vector3(.12f,2.85f,4.8f),cream);
            Box("RightWall",new Vector3(1.8f,1.42f,0),new Vector3(.12f,2.85f,4.8f),cream);
            Box("BackWall",new Vector3(0,1.42f,-2.45f),new Vector3(3.6f,2.85f,.12f),cream);
            Box("FrontWall",new Vector3(0,1.42f,1.8f),new Vector3(3.6f,2.85f,.12f),cream);
            Box("LeftMint",new Vector3(-1.72f,.48f,0),new Vector3(.04f,.96f,4.7f),mint);
            Box("RightMint",new Vector3(1.72f,.48f,0),new Vector3(.04f,.96f,4.7f),mint);
            Box("FrontMint",new Vector3(0,.48f,1.7f),new Vector3(3.5f,.96f,.04f),mint);
            Box("DoorFrame",new Vector3(-.05f,1.14f,1.59f),new Vector3(1.62f,2.3f,.13f),teal);
            var doors=Box("ClosedDoors",new Vector3(-.05f,1.09f,1.5f),new Vector3(1.45f,2.12f,.08f),mint);
            Box("DoorSeam",new Vector3(-.05f,1.09f,1.448f),new Vector3(.015f,2.12f,.015f),ink);
            Observe(doors,ElevatorClue.Door,new Vector3(1.48f,2.14f,.01f));
            var display=Box("FloorDisplay",new Vector3(-.05f,2.48f,1.53f),new Vector3(.8f,.28f,.12f),ink);
            lesson.floorLabel=WorldText("FloorNumber",new Vector3(-.05f,2.48f,1.455f),new Vector2(.76f,.25f),"↑  02",1.4f,paper.GetColor("_BaseColor"));
            Observe(display,ElevatorClue.Display,new Vector3(.84f,.32f,.01f));
            var intercom=Box("EmergencyIntercom",new Vector3(1.14f,1.47f,1.54f),new Vector3(.45f,.7f,.12f),teal);
            WorldText("IntercomLabel",new Vector3(1.14f,1.69f,1.459f),new Vector2(.43f,.22f),"비상통화",.65f,Color.white);
            var bell=GameObject.CreatePrimitive(PrimitiveType.Sphere);bell.name="BellButton";bell.transform.SetParent(intercom.transform);bell.transform.localPosition=new Vector3(0,-.16f,-.10f);bell.transform.localScale=new Vector3(.16f,.16f,.07f);bell.GetComponent<Renderer>().sharedMaterial=paper;
            WorldText("BellSymbol",new Vector3(1.14f,1.31f,1.36f),new Vector2(.13f,.13f),"☎",.8f,ink.GetColor("_BaseColor"));
            for(int i=0;i<4;i++) Box("SpeakerSlot",new Vector3(1.14f,1.54f-i*.034f,1.465f),new Vector3(.25f,.012f,.012f),ink);
            Observe(intercom,ElevatorClue.Intercom,new Vector3(.49f,.74f,.01f));
            var notice=Box("SafetyNotice",new Vector3(-1.27f,1.68f,1.53f),new Vector3(.73f,1.02f,.08f),paper);
            WorldText("SafetyRules",new Vector3(-1.27f,1.68f,1.474f),new Vector2(.66f,.95f),settings.safetyNotice,.51f,ink.GetColor("_BaseColor"));Observe(notice,ElevatorClue.Notice,new Vector3(.77f,1.06f,.01f));
            var identity=Box("LiftIdentity",new Vector3(1.14f,2.14f,1.53f),new Vector3(.85f,.32f,.08f),paper);
            WorldText("IdentityText",new Vector3(1.14f,2.14f,1.474f),new Vector2(.8f,.28f),settings.building+"\n"+settings.liftId+" · 가상 정보",.53f,ink.GetColor("_BaseColor"));Observe(identity,ElevatorClue.Identity,new Vector3(.89f,.36f,.01f));
            Box("SafetyRailLeft",new Vector3(-1.64f,.94f,.1f),new Vector3(.06f,.06f,3.5f),teal);
            Box("SafetyRailRight",new Vector3(1.64f,.94f,.1f),new Vector3(.06f,.06f,3.5f),teal);
            var friendRoot=new GameObject("Friend_Jump_Root");friendRoot.transform.SetParent(owner.transform);friendRoot.transform.position=new Vector3(-.95f,0,.65f);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(friendPrefab);actor.transform.SetParent(friendRoot.transform,false);actor.transform.localRotation=Quaternion.Euler(0,180,0);
            var bounds=BoundsOf(actor);float scale=1.43f/bounds.size.y;actor.transform.localScale*=scale;
            bounds=BoundsOf(actor);actor.transform.position+=Vector3.up*(-bounds.min.y);
            var hit=friendRoot.AddComponent<BoxCollider>();hit.center=new Vector3(0,.73f,0);hit.size=new Vector3(.65f,1.48f,.45f);
            lesson.friend=friendRoot.transform;Observe(friendRoot,ElevatorClue.Friend,new Vector3(.72f,1.52f,.01f),new Vector3(0,.76f,-.26f));
            lesson.motor=owner.AddComponent<AudioSource>();lesson.motor.playOnAwake=false;lesson.signal=owner.AddComponent<AudioSource>();lesson.signal.playOnAwake=false;
            lesson.motorClip=MakeAudio("ElevatorMotor",true);lesson.stopClip=MakeAudio("ElevatorStop",false);
            var ui=new GameObject("Elevator_World_UI",typeof(RectTransform)).AddComponent<ElevatorView>();ui.font=font;ui.playerCamera=camera;ui.lesson=lesson;ui.Build();lesson.view=ui;
            var desktop=new GameObject("PC_Test_Controls").AddComponent<ElevatorDesktopControls>();desktop.lesson=lesson;desktop.xrOrigin=rig.transform;desktop.targetCamera=camera;
            ElevatorPresentationSetup.Configure(lesson);
            for(int i=SceneManager.sceneCount-1;i>=0;i--) {var s=SceneManager.GetSceneAt(i);if(s!=scene) EditorSceneManager.CloseScene(s,true);}
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Could not save elevator scene.");
            AssetDatabase.SaveAssets(); Selection.activeGameObject=owner;
            Debug.Log("Elevator prototype ready: "+ScenePath);
        }
        static GameObject SaveSharedFriend()
        {
            string source="Assets/Test/Art/Models/Friend/";
            string targetModel=Shared+"Wait_Friend_Edited.fbx";
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(targetModel)) AssetDatabase.CopyAsset(source+"Wait_Friend_Edited.fbx",targetModel);
            string sourceBlend=source+"Source~/Wait_Friend_Edited.blend";
            if(!File.Exists(Shared+"Source~/Wait_Friend_Edited.blend")) File.Copy(sourceBlend,Shared+"Source~/Wait_Friend_Edited.blend");
            string shaderPath=Shared+"FriendStorybookToon.shader";
            if(!File.Exists(shaderPath)) File.WriteAllText(shaderPath,File.ReadAllText("Assets/Test/Art/Shaders/FriendStorybookToon.shader").Replace("ProjectWait/M/Friend Storybook Toon","ProjectWait/Shared/Friend Storybook Toon"));
            AssetDatabase.ImportAsset(shaderPath);var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(source+"Wait_Friend.prefab");
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(original);PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var map=new Dictionary<Material,Material>();
            foreach(var mat in copy.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())
            {
                string path=Shared+"Materials/"+mat.name+".mat";var shared=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!shared) {shared=Object.Instantiate(mat);shared.shader=shader;AssetDatabase.CreateAsset(shared,path);}map[mat]=shared;
            }
            var meshes=AssetDatabase.LoadAllAssetsAtPath(targetModel).OfType<Mesh>().ToDictionary(m=>m.name);
            foreach(var r in copy.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>m?map[m]:null).ToArray();
                if(r is SkinnedMeshRenderer skin && skin.sharedMesh && meshes.TryGetValue(skin.sharedMesh.name,out var m))skin.sharedMesh=m;
                var filter=r.GetComponent<MeshFilter>();if(filter && filter.sharedMesh && meshes.TryGetValue(filter.sharedMesh.name,out var mesh))filter.sharedMesh=mesh;
            }
            var clipPath=Shared+"Animations/Friend_Idle.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(!clip) {clip=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Test/Art/Animations/Friend/Friend_Idle.anim"));AssetDatabase.CreateAsset(clip,clipPath);}
            var ctrlPath=Shared+"Animations/Friend.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if(!controller) {controller=AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);var state=controller.layers[0].stateMachine.AddState("Idle");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;}
            copy.GetComponent<Animator>().runtimeAnimatorController=controller;
            var prefab=PrefabUtility.SaveAsPrefabAsset(copy,Shared+"Wait_Friend.prefab");Object.DestroyImmediate(copy);
            // Remove the copied importer remaps that still point to Test materials.
            var importer=(ModelImporter)AssetImporter.GetAtPath(targetModel);
            foreach(var item in importer.GetExternalObjectMap().ToArray()) importer.RemoveRemap(item.Key);
            foreach(var pair in map) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key.name),pair.Value);
            importer.SaveAndReimport();
            return prefab;
        }
        static TMP_FontAsset BuildFont()
        {
            string path="Assets/M/Arts/Fonts/ElevatorKorean.asset";var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(f)return f;
            f=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Test/Fonts/NotoSansKR.ttf"),48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            f.name="ElevatorKorean";f.isMultiAtlasTexturesEnabled=true;
            string text=string.Join("",Directory.GetFiles("Assets/M/Scripts/Elevator","*.cs").Select(File.ReadAllText))+"0123456789햇살배움센터교육용☎↑—";
            f.TryAddCharacters(new string(text.Distinct().ToArray()),out string missing);
            AssetDatabase.CreateAsset(f,path);f.material.name="ElevatorKorean Material";AssetDatabase.AddObjectToAsset(f.material,f);
            foreach(var atlas in f.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,f);
            EditorUtility.SetDirty(f);return f;
        }
        static Material Mat(string name,Color color,float outline)
        {
            string path="Assets/M/Arts/Materials/Elevator/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/ElevatorStorybookToon.shader"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetColor("_OutlineColor",new Color32(73,51,39,255));m.SetColor("_ShadeColor",new Color32(189,200,195,255));
            m.SetFloat("_MidTone",.9f);m.SetFloat("_ShadowThreshold",.02f);m.SetFloat("_LightThreshold",.62f);m.SetFloat("_BandSoftness",.012f);m.SetFloat("_OutlineWidth",outline);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(environment);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;return g;
        }
        static TMP_Text WorldText(string name,Vector3 pos,Vector2 size,string value,float point,Color color)
        {
            var g=new GameObject(name);g.transform.SetParent(environment);g.transform.position=pos;var text=g.AddComponent<TextMeshPro>();text.font=font;text.fontSize=point;text.fontStyle=FontStyles.Bold;text.color=color;text.text=value;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;text.overflowMode=TextOverflowModes.Overflow;return text;
        }
        static void Observe(GameObject g,ElevatorClue clue,Vector3 size,Vector3 offset=default)
        {
            var interact=g.AddComponent<XRSimpleInteractable>();var target=g.AddComponent<ElevatorObservation>();target.clue=clue;target.lesson=lesson;
            var edge=new GameObject("ObservationOutline").AddComponent<LineRenderer>();edge.transform.SetParent(g.transform,false);
            // World-sized rectangle ignores the primitive's non-uniform scale.
            edge.useWorldSpace=true;edge.loop=true;edge.positionCount=4;
            Vector3 center=g.transform.position+offset;if(offset==Vector3.zero)center.z-=g.transform.lossyScale.z*.5f+.018f;
            var d=size*.5f;edge.SetPositions(new[]{center+new Vector3(-d.x,-d.y,0),center+new Vector3(-d.x,d.y,0),center+new Vector3(d.x,d.y,0),center+new Vector3(d.x,-d.y,0)});
            edge.widthMultiplier=.006f;edge.numCornerVertices=4;edge.numCapVertices=4;
            edge.sharedMaterial=Mat("ObservationTeal",new Color(.16f,.48f,.44f),0);edge.sharedMaterial.SetFloat("_EmissionStrength",1);target.outline=edge;
        }
        static Bounds BoundsOf(GameObject g) {var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
        static AudioClip MakeAudio(string name,bool hum)
        {
            string path="Assets/M/Arts/Materials/Elevator/"+name+".wav";
            if(!File.Exists(path))
            {
                int rate=22050,count=hum?rate:rate/2;
                using(var w=new BinaryWriter(File.Create(path)))
                {
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
                    for(int i=0;i<count;i++){float t=(float)i/rate;float s=hum?Mathf.Sin(2*Mathf.PI*90*t)*.25f:Mathf.Sin(2*Mathf.PI*(t<.18f?480:330)*t)*Mathf.Exp(-t*7)*.45f;w.Write((short)(s*short.MaxValue));}
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
#endif
