using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using DwtD.TestScene;
using Object = UnityEngine.Object;

// Run in Edit mode through Unity Pipeline. Generated meshes and materials are real assets.
public static class BuildStorybookArt
{
    const string Root = "Assets/Test/Art";
    static Transform art;
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Shader toon;
    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var c); return c; }

    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before authoring art.");
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(p==null || p.gameObject.scene.path!="Assets/Test/Scenes/WetCorridor_Test.unity") throw new Exception("Open WetCorridor_Test first.");
        AssetDatabase.Refresh();
        toon=Shader.Find("ProjectWait/Storybook Toon");
        if(toon==null || ShaderUtil.ShaderHasError(toon)) throw new Exception("Storybook Toon shader is missing or has compiler errors.");
        System.IO.Directory.CreateDirectory(Root+"/Materials");
        System.IO.Directory.CreateDirectory(Root+"/Meshes");
        AssetDatabase.Refresh();
        materials.Clear(); meshes.Clear();
        Undo.SetCurrentGroupName("Apply storybook corridor art");
        Undo.RegisterFullObjectHierarchyUndo(p.gameObject,"Apply storybook corridor art");
        var old=p.transform.Find("Storybook_Art");
        if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);
        art=new GameObject("Storybook_Art").transform; art.SetParent(p.transform,false);
        Undo.RegisterCreatedObjectUndo(art.gameObject,"Create storybook environment");
        Palette();
        RestyleExisting(p);
        Architecture(p);
        Puddle(p);
        Character(p);
        Lighting(p);
        Typography(p);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        EditorSceneManager.SaveScene(p.gameObject.scene);
        return new {scene=p.gameObject.scene.path,shader=toon.name,generatedMaterials=materials.Count,generatedMeshes=meshes.Count,artObjects=art.GetComponentsInChildren<Renderer>().Length};
    }

    static Material Mat(string name,string color,float ink=1.3f,float unlit=0,string shade="B3C4CB")
    {
        var path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(toon); AssetDatabase.CreateAsset(m,path); }
        Undo.RecordObject(m,"Storybook palette");
        m.shader=toon; m.SetColor("_BaseColor",Hex(color)); m.SetColor("_ShadeColor",Hex(shade));
        m.SetColor("_OutlineColor",Hex("49352D")); m.SetFloat("_OutlineWidth",ink*1.4f);
        m.SetFloat("_MidTone",.86f); m.SetFloat("_ShadowThreshold",.06f); m.SetFloat("_LightThreshold",.62f);
        m.SetFloat("_BandSoftness",.012f); m.SetFloat("_EmissionStrength",unlit);
        m.enableInstancing=true; EditorUtility.SetDirty(m); materials[name]=m; return m;
    }

    static void Palette()
    {
        Mat("CreamWalls","F4DFAD",.65f,0,"CAD0C3"); Mat("MintWainscot","98CDB5",.9f);
        Mat("DeepTealTrim","267E78",1.4f); Mat("WarmFloor","E8D2A4",0,0,"BAB7AA");
        Mat("FloorJoint","B7A184",0,.65f); Mat("SoftWhite","FFF2D3",1.2f);
        Mat("OrangeHoodie","FF8D32",2.1f,0,"D2A58C"); Mat("Skin","FFC18A",1.6f,0,"E0B79F");
        Mat("Hair","654332",1.8f,0,"B29684"); Mat("Charcoal","342D29",.6f,.45f);
        Mat("Blush","FA926E",0,.7f); Mat("NavyTrousers","216B70",1.7f);
        Mat("BlueBackpack","429F9B",1.6f); Mat("SchoolYellow","F8AA46",1.5f);
        Mat("Terracotta","ED8A41",1.5f,0,"D1AB90"); Mat("Leaves","5FAD45",1.5f,0,"A0B28E");
        Mat("Locker0","369A94",1.3f); Mat("Locker1","9ECBA9",1.3f); Mat("Locker2","64B6A0",1.3f);
        Mat("Water","2DBDDD",0,.9f); Mat("WaterGleam","BBF8F5",0,1);
        Mat("Cork","B97940",1.2f); Mat("WarmWindow","83D9ED",0,1);
        Mat("Ink","49352D",0,1); Mat("WaterEdge","157B9E",0,1);
        Mat("WaterLight","66D8E6",0,1); Mat("WaterShade","25A7CF",0,1);
        Mat("SunlitFloor","FFE5A8",0,1); Mat("Ceiling","F2DEB7",0,.4f);
        Mat("LeafLight","A9CF55",1.1f,0,"8B9F75"); Mat("LeafDark","3E8A50",1.1f);
        Mat("Sole","C1C5B8",.8f); Mat("HoodRib","E8792C",1.1f);
        Mat("Lip","944334",.65f,.3f); Mat("Tongue","ED7B6E",0,.7f);
        Mat("Paper","FFF5DA",.8f,1); Mat("Lamp","FFF7CC",0,1);
    }

    static GameObject Shape(string name,Transform parent,Vector3 pos,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Sphere)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
        go.transform.localPosition=pos; go.transform.localScale=size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial=m; return go;
    }
    static Transform Group(string name,Transform parent,Vector3 pos)
    {
        var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=pos; return t;
    }
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        var path=Root+"/Meshes/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null) { mesh.name=name; AssetDatabase.CreateAsset(mesh,path); existing=mesh; }
        else { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); }
        EditorUtility.SetDirty(existing); meshes[name]=existing; return existing;
    }
    static GameObject MeshObject(string name,Transform parent,Vector3 pos,Mesh mesh,Material mat)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
        go.transform.localPosition=pos; go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=mat; return go;
    }

    // Rounded cuboids with bevel normals, rather than stretched primitive cubes.
    static Mesh RoundedMesh(Vector3 size,float radius)
    {
        string key=FormattableString.Invariant($"Bevel_{size.x:0.000}_{size.y:0.000}_{size.z:0.000}_{radius:0.000}");
        if(meshes.TryGetValue(key,out var cached)) return cached;
        var asset=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+key+".asset");
        if(asset!=null) { meshes[key]=asset; return asset; }
        var h=size*.5f; float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.9f);
        float[] Grid(float half) => new[]{-half,-half+r*.293f,-half+r,half-r,half-r*.293f,half};
        var grids=new[]{Grid(h.x),Grid(h.y),Grid(h.z)};
        var v=new List<Vector3>(); var n=new List<Vector3>(); var tris=new List<int>();
        for(int axis=0;axis<3;axis++) for(int sign=-1;sign<=1;sign+=2)
        {
            int u=(axis+1)%3,w=(axis+2)%3,offset=v.Count;
            for(int a=0;a<6;a++) for(int b=0;b<6;b++)
            {
                var point=Vector3.zero; point[axis]=h[axis]*sign; point[u]=grids[u][a]; point[w]=grids[w][b];
                var inner=new Vector3(Mathf.Clamp(point.x,-h.x+r,h.x-r),Mathf.Clamp(point.y,-h.y+r,h.y-r),Mathf.Clamp(point.z,-h.z+r,h.z-r));
                var normal=(point-inner).normalized; v.Add(inner+normal*r); n.Add(normal);
            }
            for(int a=0;a<5;a++) for(int b=0;b<5;b++)
            {
                int i=offset+a*6+b;
                if(sign>0) tris.AddRange(new[]{i,i+6,i+1,i+1,i+6,i+7});
                else tris.AddRange(new[]{i,i+1,i+6,i+1,i+7,i+6});
            }
        }
        var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(tris,0); mesh.RecalculateBounds(); return SaveMesh(key,mesh);
    }
    static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,float radius=.035f)
        => MeshObject(name,parent,pos,RoundedMesh(size,radius),mat);

    static void RestyleExisting(WetCorridorPrototype p)
    {
        foreach(var r in p.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(r.sharedMaterial!=null && materials.TryGetValue(r.sharedMaterial.name,out var m)) { Undo.RecordObject(r,"Apply toon material"); r.sharedMaterial=m; }
            if(r.transform.IsChildOf(p.friend)) continue;
            if(new[]{"LeftWall","RightWall","EndWall","LowerWall","FloorJoint","SunlightPatch","WindowFrame","WindowGlass","WindowBar","CeilingBeam"}.Contains(r.name)) r.shadowCastingMode=ShadowCastingMode.Off;
            if(new[]{"FloorJoint","SunlightPatch","CeilingBeam","Leaf","PlantPot"}.Contains(r.name)) r.enabled=false;
            if(new[]{"Locker","LockerDoor","ClassroomDoorFrame","ClassroomDoor","DoorWindow","NoticeBoard"}.Contains(r.name))
            {
                var sizes=new Dictionary<string,Vector3> {
                    {"Locker",new Vector3(.62f,1.94f,.76f)}, {"LockerDoor",new Vector3(.045f,1.83f,.69f)},
                    {"ClassroomDoorFrame",new Vector3(2.1f,2.7f,.18f)}, {"ClassroomDoor",new Vector3(1.86f,2.5f,.1f)},
                    {"DoorWindow",new Vector3(1.1f,.67f,.06f)}, {"NoticeBoard",new Vector3(.08f,1.2f,1.6f)} };
                var size=sizes[r.name];
                r.GetComponent<MeshFilter>().sharedMesh=RoundedMesh(size,.045f); r.transform.localScale=Vector3.one;
            }
        }
    }

    static void Architecture(WetCorridorPrototype p)
    {
        var env=Group("School_Illustration",art,Vector3.zero);
        // Fine grout is readable in perspective without outlining every tile.
        var joints=new List<CombineInstance>();
        void Joint(Vector3 position,Vector3 scale)
        {
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);
            joints.Add(new CombineInstance{mesh=primitive.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(position,Quaternion.identity,scale)}); Object.DestroyImmediate(primitive);
        }
        for(float x=-3.66f;x<3.8f;x+=1.22f) Joint(new Vector3(x,.022f,2.4f),new Vector3(.011f,.004f,17));
        for(float z=-5.8f;z<11;z+=1.2f) Joint(new Vector3(0,.022f,z),new Vector3(7.4f,.004f,.011f));
        var grid=new Mesh(); grid.CombineMeshes(joints.ToArray());
        MeshObject("Tiled_Floor_Grout",env,Vector3.zero,SaveMesh("FloorGrout",grid),materials["FloorJoint"]).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        Box("Ceiling",env,new Vector3(0,3.82f,2.5f),new Vector3(7.6f,.12f,17),materials["Ceiling"],.01f).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        for(float z=-2.8f;z<10;z+=2.8f)
        {
            Box("Teal_Light_Frame",env,new Vector3(0,3.62f,z),new Vector3(3.5f,.10f,.54f),materials["DeepTealTrim"]);
            Box("Warm_Light_Panel",env,new Vector3(0,3.557f,z),new Vector3(3.28f,.024f,.38f),materials["Lamp"],.015f);
        }
        foreach(var glass in p.GetComponentsInChildren<MeshRenderer>().Where(x=>x.name=="WindowGlass"))
        {
            float z=glass.transform.position.z;
            Box("Deep_Window_Sill",env,new Vector3(-3.34f,1.32f,z),new Vector3(.38f,.10f,2.05f),materials["SoftWhite"]);
            // Shallow, framed dioramas: sky and leafy silhouettes are clipped by their own composition.
            for(int i=0;i<5;i++)
            {
                var leaf=Shape("Outside_Tree",env,new Vector3(-3.455f,1.73f+(i%2)*.09f,z-.66f+i*.32f),new Vector3(.025f,.50f+(i%3)*.06f,.48f),materials[i%2==0?"LeafLight":"LeafDark"]);
                leaf.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            Shape("Cloud",env,new Vector3(-3.456f,2.65f,z-.35f),new Vector3(.022f,.23f,.6f),materials["Paper"]).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            Shape("Cloud",env,new Vector3(-3.456f,2.73f,z-.2f),new Vector3(.022f,.26f,.3f),materials["Paper"]).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Large, clean sun shapes retain the illustrated window rhythm.
            var patch=FlatPolygon("Sunbeam",env,new Vector3(0,.012f,0),new[]{new Vector2(-3.52f,z-.78f),new Vector2(-3.52f,z+.65f),new Vector2(.4f,z+2.3f),new Vector2(.4f,z+.85f)},materials["SunlitFloor"]);
            patch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        for(int i=0;i<9;i++)
        {
            float z=-1.3f+i*.82f;
            Box("Locker_Label_Plate",env,new Vector3(2.87f,1.83f,z),new Vector3(.02f,.11f,.21f),materials["Paper"],.012f);
            Box("Locker_Handle_Backplate",env,new Vector3(2.875f,.98f,z-.21f),new Vector3(.04f,.23f,.12f),materials["DeepTealTrim"],.022f);
        }
        Box("Classroom_Sign_Frame",env,new Vector3(.1f,2.94f,10.32f),new Vector3(1.48f,.28f,.08f),materials["DeepTealTrim"]);
        Box("Classroom_Sign",env,new Vector3(.1f,2.94f,10.267f),new Vector3(1.32f,.18f,.025f),materials["Paper"],.018f);
        Plant(new Vector3(-2.85f,0,-.15f),1.25f,env); Plant(new Vector3(-2.7f,0,8.9f),1.0f,env);
    }

    static void Plant(Vector3 position,float scale,Transform parent)
    {
        var g=Group("Potted_Plant",parent,position); g.localScale=Vector3.one*scale;
        Shape("Pot",g,new Vector3(0,.25f,0),new Vector3(.56f,.25f,.56f),materials["Terracotta"],PrimitiveType.Cylinder);
        Shape("Pot_Rim",g,new Vector3(0,.49f,0),new Vector3(.63f,.045f,.63f),materials["Terracotta"],PrimitiveType.Cylinder);
        Shape("Soil",g,new Vector3(0,.538f,0),new Vector3(.49f,.008f,.49f),materials["Hair"],PrimitiveType.Cylinder);
        for(int i=0;i<9;i++)
        {
            float a=i*137.5f*Mathf.Deg2Rad; float spread=i<3?.10f:.24f;
            var leaf=Shape("Broad_Leaf",g,new Vector3(Mathf.Cos(a)*spread,.86f+(i<3?.17f:0),Mathf.Sin(a)*spread),new Vector3(.24f,.77f,.12f),materials[i%3==0?"LeafLight":"Leaves"]);
            leaf.transform.localRotation=Quaternion.Euler(Mathf.Sin(a)*32,0,-Mathf.Cos(a)*32);
        }
    }

    static GameObject FlatPolygon(string name,Transform parent,Vector3 position,Vector2[] points,Material mat)
    {
        var vertices=new List<Vector3>(); vertices.Add(new Vector3(points.Average(v=>v.x),0,points.Average(v=>v.y)));
        vertices.AddRange(points.Select(v=>new Vector3(v.x,0,v.y))); var tri=new List<int>();
        float area=0; for(int i=0;i<points.Length;i++) { var a=points[i]; var b=points[(i+1)%points.Length]; area+=a.x*b.y-b.x*a.y; }
        for(int i=0;i<points.Length;i++)
            if(area>0) tri.AddRange(new[]{0,(i+1)%points.Length+1,i+1});
            else tri.AddRange(new[]{0,i+1,(i+1)%points.Length+1});
        var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(tri,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string key=FormattableString.Invariant($"{name}_{points.Length}_{points[0].x:0.000}_{points[0].y:0.000}");
        return MeshObject(name,parent,position,SaveMesh(key,mesh),mat);
    }
    static Vector2[] PuddlePoints(float sx=1,float sz=1)
    {
        var points=new Vector2[96];
        for(int i=0;i<points.Length;i++)
        {
            float t=i*Mathf.PI*2/points.Length;
            float r=1+.16f*Mathf.Sin(t*3+.6f)+.10f*Mathf.Cos(t*5-.8f)+.035f*Mathf.Sin(t*7);
            points[i]=new Vector2(Mathf.Cos(t)*r*1.18f*sx,Mathf.Sin(t)*r*.77f*sz);
        }
        return points;
    }
    static void Stroke(string name,Transform parent,Vector3[] points,Material mat,float width,bool loop=false)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        var line=go.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=points.Length; line.SetPositions(points);
        line.sharedMaterial=mat; line.startWidth=line.endWidth=width; line.loop=loop;
        line.numCornerVertices=3; line.numCapVertices=3; line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false;
    }
    static void Puddle(WetCorridorPrototype p)
    {
        var old=p.transform.Find("StoryProps/WetFloor");
        foreach(var r in old.GetComponentsInChildren<Renderer>()) r.enabled=false;
        var g=Group("Illustrated_Water",art,new Vector3(-.45f,.032f,2.1f));
        FlatPolygon("Puddle_Ink_Edge",g,Vector3.zero,PuddlePoints(1.025f,1.04f),materials["WaterEdge"]);
        FlatPolygon("Puddle_Blue",g,new Vector3(0,.004f,0),PuddlePoints(),materials["Water"]);
        FlatPolygon("Puddle_Light_Band",g,new Vector3(-.2f,.007f,-.2f),PuddlePoints(.60f,.52f),materials["WaterLight"]);
        for(int i=0;i<3;i++)
        {
            float x=-.64f+i*.54f;
            Stroke("Water_Glint",g,new[]{new Vector3(x-.16f,.016f,.04f+i*.17f),new Vector3(x,.016f,.09f+i*.17f),new Vector3(x+.22f,.016f,.08f+i*.17f)},materials["WaterGleam"],.025f);
        }
        for(int i=0;i<2;i++)
        {
            var drop=Group("Droplet",g,new Vector3(1.30f-i*.38f,.002f,-.45f-i*.55f)); drop.localScale=new Vector3(.14f,1,.13f);
            FlatPolygon("Drop_Edge",drop,Vector3.zero,PuddlePoints(),materials["WaterEdge"]);
            FlatPolygon("Drop_Fill",drop,new Vector3(0,.003f,0),PuddlePoints(.82f,.78f),materials["Water"]);
        }
    }

    static void Character(WetCorridorPrototype p)
    {
        var f=p.friend;
        var old=f.Find("Storybook_Character_Details"); if(old!=null) Object.DestroyImmediate(old.gameObject);
        var details=Group("Storybook_Character_Details",f,Vector3.zero);
        void Set(string name,Vector3 position,Vector3 size)
        {
            var t=f.Find(name); t.localPosition=position; t.localScale=size;
        }
        Set("Body",new Vector3(0,.99f,0),new Vector3(.78f,.43f,.53f));
        Set("Hood",new Vector3(0,1.27f,-.055f),new Vector3(.72f,.34f,.55f));
        Set("Head",new Vector3(0,1.76f,.035f),new Vector3(.94f,.86f,.80f));
        Set("HairCap",new Vector3(0,2.005f,-.04f),new Vector3(1.00f,.46f,.85f));
        Set("Nose",new Vector3(0,1.65f,.444f),new Vector3(.09f,.075f,.085f));
        foreach(var t in f.Cast<Transform>().ToArray())
        {
            if(t.name=="HairFringe") t.GetComponent<Renderer>().enabled=false;
            if(t.name=="Ear") { float side=Mathf.Sign(t.localPosition.x); t.localPosition=new Vector3(side*.47f,1.73f,.03f); t.localScale=new Vector3(.21f,.26f,.18f); Shape("Inner_Ear",details,new Vector3(side*.494f,1.73f,.112f),new Vector3(.09f,.13f,.035f),materials["Blush"]); }
            if(t.name=="EyeWhite") t.gameObject.SetActive(false);
            if(t.name=="Eye")
            {
                float side=Mathf.Sign(t.localPosition.x); t.localPosition=new Vector3(side*.175f,1.78f,.423f); t.localScale=new Vector3(.09f,.145f,.038f);
                Shape("Eye_Glint",details,new Vector3(side*.175f-.014f,1.815f,.444f),new Vector3(.023f,.032f,.012f),materials["Paper"]);
                var brow=Shape("Eyebrow",details,new Vector3(side*.18f,1.915f,.397f),new Vector3(.14f,.037f,.035f),materials["Hair"]); brow.transform.localRotation=Quaternion.Euler(0,0,side*10);
            }
            if(t.name=="Cheek") { float side=Mathf.Sign(t.localPosition.x); t.localPosition=new Vector3(side*.305f,1.646f,.344f); t.localScale=new Vector3(.145f,.095f,.03f); }
        }
        // Preserve the existing mouth references used by the safe and unsafe branches.
        var smile=p.smile.transform; smile.localPosition=new Vector3(0,1.573f,.42f); smile.localScale=Vector3.one;
        var mouthPoints=new Vector3[26]; mouthPoints[0]=new Vector3(0,-.012f,0);
        for(int i=0;i<=24;i++) { float a=Mathf.PI+i*Mathf.PI/24; mouthPoints[i+1]=new Vector3(Mathf.Cos(a)*.112f,.035f+Mathf.Sin(a)*.13f,0); }
        var mouthTriangles=new List<int>(); for(int i=1;i<25;i++) mouthTriangles.AddRange(new[]{0,i,i+1}); mouthTriangles.AddRange(new[]{0,25,1});
        var mouthMesh=new Mesh{vertices=mouthPoints,triangles=mouthTriangles.ToArray()}; mouthMesh.RecalculateNormals(); mouthMesh.RecalculateBounds();
        smile.GetComponent<MeshFilter>().sharedMesh=SaveMesh("HappyMouth",mouthMesh); smile.GetComponent<Renderer>().sharedMaterial=materials["Lip"];
        var tongueOld=smile.Find("Tongue"); if(tongueOld!=null) Object.DestroyImmediate(tongueOld.gameObject);
        Shape("Tongue",smile,new Vector3(.015f,-.048f,.008f),new Vector3(.11f,.045f,.012f),materials["Tongue"]);
        var teethOld=smile.Find("Teeth"); if(teethOld!=null) Object.DestroyImmediate(teethOld.gameObject);
        Box("Teeth",smile,new Vector3(-.012f,.021f,.008f),new Vector3(.12f,.025f,.012f),materials["Paper"],.007f);
        Set("SurprisedMouth",new Vector3(0,1.57f,.42f),new Vector3(.12f,.15f,.035f));
        foreach(var arm in new[]{p.leftArm,p.rightArm})
        {
            float side=arm==p.leftArm?-1:1;
            arm.localPosition=new Vector3(side*.37f,1.23f,0);
            arm.Find("Sleeve").localScale=new Vector3(.25f,.205f,.25f);
            var hand=arm.Find("Hand"); hand.localPosition=new Vector3(0,-.45f,.02f); hand.localScale=new Vector3(.21f,.22f,.17f);
            var oldCuff=arm.Find("Story_Cuff"); if(oldCuff!=null) Object.DestroyImmediate(oldCuff.gameObject);
            var oldThumb=arm.Find("Story_Thumb"); if(oldThumb!=null) Object.DestroyImmediate(oldThumb.gameObject);
            Shape("Story_Cuff",arm,new Vector3(0,-.36f,0),new Vector3(.25f,.09f,.25f),materials["HoodRib"]);
            Shape("Story_Thumb",arm,new Vector3(-side*.09f,-.42f,.065f),new Vector3(.10f,.13f,.105f),materials["Skin"]);
        }
        foreach(var leg in new[]{p.leftLeg,p.rightLeg})
        {
            leg.Find("TrouserLeg").localScale=new Vector3(.29f,.20f,.30f);
            var shoe=leg.Find("Shoe"); shoe.localScale=Vector3.one; shoe.localPosition=new Vector3(0,-.42f,.075f);
            shoe.GetComponent<MeshFilter>().sharedMesh=RoundedMesh(new Vector3(.31f,.19f,.44f),.07f);
            var soleOld=leg.Find("Story_Sole"); if(soleOld!=null) Object.DestroyImmediate(soleOld.gameObject);
            Box("Story_Sole",leg,new Vector3(0,-.495f,.075f),new Vector3(.32f,.045f,.45f),materials["Sole"],.02f);
        }
        Set("Backpack",new Vector3(0,1.05f,-.3f),new Vector3(.54f,.3f,.27f));
        Box("Hoodie_Hem",details,new Vector3(0,.70f,.018f),new Vector3(.66f,.10f,.46f),materials["HoodRib"],.04f);
        Box("Kangaroo_Pocket",details,new Vector3(0,.91f,.263f),new Vector3(.40f,.20f,.045f),materials["HoodRib"],.07f);
        Stroke("Pocket_Stitch",details,new[]{new Vector3(-.15f,.98f,.291f),new Vector3(-.11f,.87f,.299f),new Vector3(.11f,.87f,.299f),new Vector3(.15f,.98f,.291f)},materials["OrangeHoodie"],.014f);
        Shape("Hair_Tuft",details,new Vector3(.18f,2.19f,-.02f),new Vector3(.18f,.21f,.13f),materials["Hair"]).transform.localRotation=Quaternion.Euler(0,0,-30);
        for(int i=0;i<5;i++)
        {
            float x=(i-2)*.172f;
            var bang=Shape("Sculpted_Bang",details,new Vector3(x,1.987f+Mathf.Abs(i-2)*.012f,.352f-Mathf.Abs(i-2)*.05f),new Vector3(.30f,.31f,.28f),materials["Hair"]);
            bang.transform.localRotation=Quaternion.Euler(0,0,(i-2)*-9);
        }
    }

    static void Lighting(WetCorridorPrototype p)
    {
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(light,"Storybook lighting");
            if(light.type==LightType.Directional) { light.transform.rotation=Quaternion.Euler(50,55,0); light.color=Hex("FFF0D3"); light.intensity=1; light.shadows=LightShadows.Soft; light.shadowBias=.035f; light.shadowNormalBias=.15f; }
            else light.intensity=0;
        }
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=Hex("BCCBD1");
        RenderSettings.fog=false;
        Undo.RecordObject(p.playerCamera,"Storybook view"); p.playerCamera.fieldOfView=58; p.playerCamera.allowHDR=false;
    }

    static void Typography(WetCorridorPrototype p)
    {
        // Reuse the static Korean atlas through a material preset; no duplicated font texture.
        string path=Root+"/Materials/StorybookKoreanInk.mat";
        var preset=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(preset==null) { preset=new Material(p.heading.font.material); AssetDatabase.CreateAsset(preset,path); }
        Undo.RecordObject(preset,"Readable storybook text"); preset.SetFloat("_FaceDilate",.18f);
        EditorUtility.SetDirty(preset); p.storybookTextMaterial=preset;
    }

    public static object CleanUnusedGeneratedAssets()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before cleaning generated assets.");
        const string scene="Assets/Test/Scenes/WetCorridor_Test.unity";
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=scene) throw new Exception("Open the corridor scene first.");
        var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(scene,true));
        var unused=AssetDatabase.FindAssets("",new[]{Root})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path=>!AssetDatabase.IsValidFolder(path) && !dependencies.Contains(path)).ToArray();
        foreach(var path in unused) AssetDatabase.DeleteAsset(path);
        // Early review captures were accidentally imported. The final captures live under Test/.
        foreach(var path in new[]{"Assets/Test/Art_Title_First.png","Assets/Art_Observation_First.png"})
            if(AssetDatabase.LoadMainAssetAtPath(path)!=null) AssetDatabase.DeleteAsset(path);
        return new {removedUnusedGeneratedAssets=unused.Length};
    }
}
