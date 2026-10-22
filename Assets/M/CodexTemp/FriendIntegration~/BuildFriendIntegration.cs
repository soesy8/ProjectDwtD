using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using DwtD.TestScene;
using ProjectWait.M;
using Object=UnityEngine.Object;

public static class BuildFriendIntegration
{
    const string Model="Assets/M/Arts/Models/Friend/Wait_Friend_Edited.fbx";
    const string Prefab="Assets/M/Arts/Models/Friend/Wait_Friend.prefab";
    const string Anim="Assets/M/Arts/Animations/Friend/";
    const string StoryScene="Assets/M/Scene/WetCorridor_Friend_Test.unity";
    const string PreviewScene="Assets/M/Scene/Friend_Animation_Test.unity";
    const string Work="Assets/M/CodexTemp/FriendIntegration~/";
    static GameObject sample;
    static Transform[] transforms;
    static Dictionary<string,Transform> bones;
    static Dictionary<Transform,Vector3> positions;
    static Dictionary<Transform,Quaternion> rotations,worldRotations;
    static Dictionary<Transform,Vector3> worldPositions;
    static Transform root;

    public static object Rebuild()
    {
        EditorSceneManager.OpenScene(PreviewScene,OpenSceneMode.Single);
        return Run();
    }

    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Requires Edit Mode");
        for(int i=0;i<SceneManager.sceneCount;i++)
            if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("An open scene contains unsaved work. Preserve it before switching scenes.");
        Directory.CreateDirectory(Anim);
        AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
        importer.isReadable=true; importer.optimizeGameObjects=false;
        importer.importNormals=ModelImporterNormals.Import;
        importer.SaveAndReimport();
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/FriendStorybookToon.shader");
        if(shader==null || !shader.isSupported) throw new Exception("M toon shader unavailable");
        var records=(JArray)JObject.Parse(File.ReadAllText("Assets/M/Arts/Models/Friend/Friend_MaterialManifest.json"))["materials"];
        var recordMap=records.ToDictionary(r=>(string)r["name"]);
        var mats=new Dictionary<string,Material>();
        foreach(var record in records)
        {
            var name=(string)record["name"];
            var safe=System.Text.RegularExpressions.Regex.Replace(name,@"[^\p{L}\p{N}_-]+","_");
            var path="Assets/M/Arts/Materials/Friend/"+safe+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
            m.name=name; m.shader=shader;
            var c=(JArray)record["srgb"];
            m.SetColor("_BaseColor",new Color((float)c[0],(float)c[1],(float)c[2],1));
            m.SetColor("_ShadeColor",new Color(.741f,.784f,.765f,1));
            m.SetColor("_OutlineColor",new Color(73/255f,51/255f,39/255f,1));
            m.SetFloat("_MidTone",.90f);m.SetFloat("_ShadowThreshold",.02f);
            m.SetFloat("_LightThreshold",.62f);m.SetFloat("_BandSoftness",.012f);
            bool flat=(bool)record["flat"];
            m.SetFloat("_EmissionStrength",flat?1:0);
            m.SetFloat("_Cull",flat?0:2);
            m.SetFloat("_OutlineWidth",flat?0:1.8f);
            m.enableInstancing=true;
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);mats[name]=m;
        }
        // Map embedded FBX material slots to the M-owned URP materials.
        foreach(var m in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Material>())
            if(mats.TryGetValue(m.name,out var replacement))
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),replacement);
        importer.SaveAndReimport();
        var fbx=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        sample=new GameObject("Wait_Friend");
        var model=(GameObject)PrefabUtility.InstantiatePrefab(fbx);model.name="Model";model.transform.SetParent(sample.transform,false);
        foreach(var r in model.GetComponentsInChildren<Renderer>())
        {
            var rm=r.sharedMaterials;
            for(int i=0;i<rm.Length;i++) if(rm[i]!=null && mats.TryGetValue(rm[i].name,out var m)) rm[i]=m;
            r.sharedMaterials=rm;
            if(r is SkinnedMeshRenderer skinned) {skinned.updateWhenOffscreen=true;skinned.quality=SkinQuality.Bone4;}
        }
        foreach(var a in model.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);
        var box=SkinBounds(sample);model.transform.localPosition-=Vector3.up*box.min.y;
        var animator=sample.AddComponent<Animator>();animator.applyRootMotion=false;
        animator.avatar=null; // Curves bind to this prefab's Model/ hierarchy, not the source FBX Avatar paths.
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        transforms=sample.GetComponentsInChildren<Transform>();
        positions=transforms.ToDictionary(t=>t,t=>t.localPosition);
        rotations=transforms.ToDictionary(t=>t,t=>t.localRotation);
        worldRotations=transforms.ToDictionary(t=>t,t=>t.rotation);
        worldPositions=transforms.ToDictionary(t=>t,t=>t.position);
        bones=transforms.Where(t=>new[]{"root","pelvis","spine","neck","head","upper_arm.L","upper_arm.R","forearm.L","forearm.R","hand.L","hand.R","thigh.L","thigh.R","shin.L","shin.R","foot.L","foot.R"}.Contains(t.name)).ToDictionary(t=>t.name);
        if(bones.Count!=17) throw new Exception("Expected all 17 source bones, got "+bones.Count);
        root=bones["root"];
        var idle=Clip("Idle",1.6f,true,t=>{
            root.position+=Vector3.up*(.002f*Mathf.Sin(t/1.6f*Mathf.PI*2));
            Grip(); Delta("head",new Vector3(0,1.2f*Mathf.Sin(t/1.6f*Mathf.PI*2),0));
        });
        var walk=Clip("Walk",.72f,true,t=>{
            float phase=t/.72f, wave=Mathf.Sin(phase*2*Mathf.PI);
            root.position+=Vector3.up*(-.035f+.004f*Mathf.Cos(phase*4*Mathf.PI));
            Delta("spine",new Vector3(3,0,-1.8f*wave));Delta("head",new Vector3(-2,0,1.8f*wave));
            Delta("upper_arm.L",new Vector3(-14*wave,0,-3));Delta("upper_arm.R",new Vector3(14*wave,0,3));
            Delta("forearm.L",new Vector3(-10,0,0));Delta("forearm.R",new Vector3(-10,0,0));
            Step("L",phase);Step("R",phase+.5f);
        });
        var fall=Clip("Fall",1.65f,false,t=>{
            float p=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1.12f,t));
            root.position+=new Vector3(-.12f*p,-.16f*p,.06f*p);
            Delta("root",new Vector3(-18*p,0,78*p));
            Delta("spine",new Vector3(-8*p,8*p,0));Delta("head",new Vector3(6*p,-5*p,-9*p));
            Delta("thigh.L",new Vector3(-42*p,0,-12*p));Delta("shin.L",new Vector3(52*p,0,0));
            Delta("thigh.R",new Vector3(-18*p,0,9*p));Delta("shin.R",new Vector3(38*p,0,0));
            Delta("upper_arm.L",new Vector3(-48*p,0,-32*p));Delta("forearm.L",new Vector3(-28*p,0,0));
            Delta("upper_arm.R",new Vector3(-64*p,0,24*p));Delta("forearm.R",new Vector3(-22*p,0,0));
            Ground();
        });
        Reset();
        var controllerPath=Anim+"Friend.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var sm=controller.layers[0].stateMachine;
        foreach(var s in sm.states) sm.RemoveState(s.state);
        foreach(var pair in new[]{(name:"Idle",clip:idle),(name:"Walk",clip:walk),(name:"Fall",clip:fall)})
        {var state=sm.AddState(pair.name);state.motion=pair.clip;state.writeDefaultValues=true;if(pair.name=="Idle")sm.defaultState=state;}
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        animator.runtimeAnimatorController=controller;
        var prefab=PrefabUtility.SaveAsPrefabAsset(sample,Prefab);Object.DestroyImmediate(sample);
        BuildScenes(prefab);
        File.WriteAllText(Work+"Build_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source=Model,prefab=Prefab,storyScene=StoryScene,previewScene=PreviewScene,materialCount=mats.Count,bones=bones.Count,clips=new[]{idle.name,walk.name,fall.name}},Newtonsoft.Json.Formatting.Indented));
        return "Created M prefab, 3 animation clips/controller, copied corridor and animation-preview scenes.";
    }

    static Bounds BoundsOf(GameObject go)
    {var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
    static Bounds SkinBounds(GameObject go)
    {
        var b=new Bounds();bool first=true;
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {var m=new Mesh();r.BakeMesh(m,true);foreach(var v in m.vertices){var p=r.transform.TransformPoint(v);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}Object.DestroyImmediate(m);}
        return b;
    }
    static void Reset() {foreach(var t in transforms){t.localPosition=positions[t];t.localRotation=rotations[t];}}
    static void Delta(string name,Vector3 euler)
    {
        var b=bones[name];var parent=worldRotations[b.parent];
        b.localRotation=Quaternion.Inverse(parent)*Quaternion.Euler(euler)*parent*rotations[b];
    }
    static void Aim(Transform b,Transform child,Vector3 target)
    { b.rotation=Quaternion.FromToRotation(child.position-b.position,target-b.position)*b.rotation; }
    static void IK(Transform a,Transform b,Transform c,Vector3 target,Vector3 pole)
    {
        var line=target-a.position;float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
        float d=Mathf.Clamp(line.magnitude,.001f,(l1+l2)*.999f);var dir=line.normalized;
        float along=(l1*l1-l2*l2+d*d)/(2*d);
        var perp=(pole-dir*Vector3.Dot(pole,dir)).normalized;
        var elbow=a.position+dir*along+perp*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
        Aim(a,b,elbow);Aim(b,c,a.position+dir*d);
    }
    static void Grip()
    {
        foreach(var side in new[]{"L","R"})
        {
            var a=bones["upper_arm."+side];var b=bones["forearm."+side];var c=bones["hand."+side];
            float sign=Mathf.Sign(a.position.x);
            IK(a,b,c,new Vector3(sign*.204f,.6105f,.156f),Vector3.right*sign);
        }
    }
    static void Step(string side,float phase)
    {
        phase=Mathf.Repeat(phase,1);float z,y;
        if(phase<.5f){z=Mathf.Lerp(.115f,-.115f,phase*2);y=0;}
        else {float p=(phase-.5f)*2;z=Mathf.Lerp(-.115f,.115f,Mathf.SmoothStep(0,1,p));y=.065f*Mathf.Sin(p*Mathf.PI);}
        var foot=bones["foot."+side];var target=worldPositions[foot]+new Vector3(0,y,z);
        IK(bones["thigh."+side],bones["shin."+side],foot,target,Vector3.forward);
        foot.rotation=worldRotations[foot];
    }
    static void Ground()
    {
        float min=float.MaxValue;
        foreach(var r in sample.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m=new Mesh();r.BakeMesh(m,true);
            foreach(var v in m.vertices) min=Mathf.Min(min,r.transform.TransformPoint(v).y);
            Object.DestroyImmediate(m);
        }
        root.position-=Vector3.up*min;
    }
    static AnimationClip Clip(string name,float duration,bool loop,Action<float> pose)
    {
        var clip=new AnimationClip{name=name,frameRate=30};
        var tracks=bones.Values.ToDictionary(b=>b,b=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});
        var rootPosition=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
        int frames=Mathf.CeilToInt(duration*30);
        for(int i=0;i<=frames;i++)
        {
            float t=duration*i/frames;Reset();pose(t);
            foreach(var b in bones.Values)
            {
                var q=b.localRotation;var c=tracks[b];c[0].AddKey(t,q.x);c[1].AddKey(t,q.y);c[2].AddKey(t,q.z);c[3].AddKey(t,q.w);
            }
            var p=root.localPosition;rootPosition[0].AddKey(t,p.x);rootPosition[1].AddKey(t,p.y);rootPosition[2].AddKey(t,p.z);
        }
        foreach(var b in bones.Values)
        {
            var path=AnimationUtility.CalculateTransformPath(b,sample.transform);
            for(int i=0;i<4;i++) AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[i]),tracks[b][i]);
        }
        for(int i=0;i<3;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(root,sample.transform),typeof(Transform),"m_LocalPosition."+"xyz"[i]),rootPosition[i]);
        clip.EnsureQuaternionContinuity();
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string asset=Anim+"Friend_"+name+".anim";
        var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
        if(old==null)AssetDatabase.CreateAsset(clip,asset);else{EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);clip=old;}
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);return clip;
    }

    static void BuildScenes(GameObject prefab)
    {
        if(!File.Exists(StoryScene)) AssetDatabase.CopyAsset("Assets/Test/Scenes/WetCorridor_Test.unity",StoryScene);
        EditorSceneManager.OpenScene(StoryScene,OpenSceneMode.Single);
        var owner=Object.FindFirstObjectByType<WetCorridorPrototype>();
        var previous=owner.friend.Find("Friend_Edited_Visual");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var renderers=owner.friend.GetComponentsInChildren<Renderer>(true);
        var oldBounds=renderers[0].bounds;foreach(var r in renderers)oldBounds.Encapsulate(r.bounds);
        foreach(var r in renderers)r.enabled=false;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);visual.name="Friend_Edited_Visual";
        var height=SkinBounds(visual).size.y;
        float scale=oldBounds.size.y/height/owner.friend.lossyScale.y;
        visual.transform.SetParent(owner.friend,false);visual.transform.localScale=Vector3.one*scale;
        var bridge=visual.AddComponent<FriendCorridorAnimation>();bridge.story=owner;bridge.visual=visual.transform;bridge.actor=visual.GetComponent<Animator>();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),StoryScene);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),PreviewScene,true);
        EditorSceneManager.OpenScene(PreviewScene,OpenSceneMode.Single);
        owner=Object.FindFirstObjectByType<WetCorridorPrototype>();
        var font=PreviewFont();
        var previewActor=Object.FindFirstObjectByType<FriendCorridorAnimation>();
        var actor=previewActor.actor;var actorVisual=previewActor.visual;
        Object.DestroyImmediate(previewActor);
        var xr=owner.xrRig;if(xr!=null)xr.SetActive(false);
        foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.enabled=false;
        foreach(var a in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None))a.enabled=false;
        foreach(var m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(m!=null && m.GetType().Namespace=="DwtD.TestScene")Object.DestroyImmediate(m);
        var cameraGo=new GameObject("Friend_Preview_Camera",typeof(Camera),typeof(AudioListener));
        var camera=cameraGo.GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=1.85f;
        var focus=actorVisual.position+Vector3.up*1.16f;
        camera.transform.position=focus+new Vector3(-.75f,.65f,-4.0f);camera.transform.LookAt(focus);
        camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.Skybox;
        var driverGo=new GameObject("Friend_Animation_Preview");var driver=driverGo.AddComponent<FriendAnimationPreview>();
        driver.actor=actor;driver.previewCamera=camera;
        UI(driver,font);
        // Keep the preview clickable with the project's Input System.
        foreach(var es in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None))es.gameObject.SetActive(false);
        new GameObject("Preview_EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(InputSystemUIInputModule));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),PreviewScene);
        Selection.activeGameObject=actorVisual.gameObject;
    }

    static TMP_FontAsset PreviewFont()
    {
        const string path="Assets/M/Arts/Materials/Friend/FriendPreviewKorean.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(font!=null)return font;
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Test/Fonts/NotoSansKR.ttf");
        font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        font.name="FriendPreviewKorean";AssetDatabase.CreateAsset(font,path);
        AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);
        font.TryAddCharacters("친구 움직임 미리보기기본자세걷기넘어지기자동재생반복123Space ·");
        EditorUtility.SetDirty(font);AssetDatabase.SaveAssetIfDirty(font);return font;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size)
    {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.sizeDelta=size;return r;}
    static TMP_Text Label(string name,Transform parent,string text,float size)
    {var r=Rect(name,parent,new Vector2(610,32));var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.font=uiFont;t.fontSize=size;t.color=new Color(.28f,.19f,.14f);t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;return t;}
    static TMP_FontAsset uiFont;
    static void UI(FriendAnimationPreview driver,TMP_FontAsset font)
    {
        uiFont=font;
        var go=new GameObject("Friend_Preview_UI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var card=Rect("Paper_Card",go.transform,new Vector2(700,180));card.anchorMin=card.anchorMax=Vector2.zero;card.pivot=Vector2.zero;card.anchoredPosition=new Vector2(24,24);
        var paper=card.gameObject.AddComponent<UnityEngine.UI.Image>();paper.color=new Color(1,.965f,.86f,.98f);
        var shadow=card.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.28f,.19f,.14f,.25f);shadow.effectDistance=new Vector2(4,-5);
        var title=Label("Title",card,"친구 움직임 미리보기",25);title.rectTransform.anchorMin=title.rectTransform.anchorMax=new Vector2(0,1);title.rectTransform.pivot=new Vector2(0,1);title.rectTransform.anchoredPosition=new Vector2(22,-15);
        var badge=Label("Motion",card,"걷기",23);badge.color=new Color(.16f,.48f,.44f);badge.rectTransform.anchorMin=badge.rectTransform.anchorMax=new Vector2(1,1);badge.rectTransform.pivot=new Vector2(1,1);badge.rectTransform.sizeDelta=new Vector2(160,32);badge.rectTransform.anchoredPosition=new Vector2(-20,-15);badge.alignment=TextAlignmentOptions.MidlineRight;driver.status=badge;
        var row=Rect("Buttons",card,new Vector2(656,54));row.anchorMin=row.anchorMax=new Vector2(0,0);row.pivot=Vector2.zero;row.anchoredPosition=new Vector2(22,62);
        var layout=row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();layout.spacing=10;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=true;
        Button(row,"Idle","1  기본 자세",driver.Idle);Button(row,"Walk","2  걷기",driver.ManualWalk);Button(row,"Fall","3  넘어지기",driver.ManualFall);Button(row,"Auto","자동 재생",driver.ToggleAutomatic);
        var hint=Label("Hint",card,"Space  ·  자동 재생 / 멈춤",19);hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=Vector2.zero;hint.rectTransform.pivot=Vector2.zero;hint.rectTransform.anchoredPosition=new Vector2(22,17);
    }
    static void Button(Transform parent,string name,string text,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(name,parent,new Vector2(156,54));var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.79f,.89f,.76f);
        var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        var label=Label("Label",rect,text,22);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;label.alignment=TextAlignmentOptions.Center;
    }
}
