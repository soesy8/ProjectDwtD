using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DwtD.TestScene;
using Object=UnityEngine.Object;

// Authored reference composition. All geometry and materials are saved as Unity assets.
public static class BuildReferenceCorridor
{
    const string Root="Assets/Test/ReferenceArt";
    static Transform art;
    static readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Color Hex(string s) {ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
    static Material Mat(string name,string color,float ink=.7f,float unlit=0)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("ProjectWait/Storybook Toon"));AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",Hex(color));m.SetColor("_ShadeColor",Hex("BDC8C3"));m.SetColor("_OutlineColor",Hex("493327"));
        m.SetFloat("_OutlineWidth",ink);m.SetFloat("_MidTone",.90f);m.SetFloat("_ShadowThreshold",.02f);m.SetFloat("_LightThreshold",.62f);
        m.SetFloat("_BandSoftness",.012f);m.SetFloat("_EmissionStrength",unlit);m.enableInstancing=true;EditorUtility.SetDirty(m);palette[name]=m;return m;
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Shape(string name,Transform parent,Vector3 position,Vector3 size,string mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=palette[mat];return go;
    }
    static Mesh Save(string name,Mesh mesh)
    {
        string path=Root+"/Meshes/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null) { mesh.name=name;AssetDatabase.CreateAsset(mesh,path);saved=mesh; }
        else {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
        EditorUtility.SetDirty(saved);return saved;
    }
    static GameObject MeshObject(string name,Transform parent,Vector3 position,Mesh mesh,Material mat)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;return go;
    }
    static Mesh Rounded(Vector3 size,float radius)
    {
        string key=FormattableString.Invariant($"Bevel_{size.x:0.000}_{size.y:0.000}_{size.z:0.000}_{radius:0.000}");
        if(meshes.TryGetValue(key,out var existing)) return existing;
        existing=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+key+".asset");
        if(existing!=null) {meshes[key]=existing;return existing;}
        var h=size*.5f;float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.9f);
        float[] Grid(float half)=>new[]{-half,-half+r*.293f,-half+r,half-r,half-r*.293f,half};
        var grids=new[]{Grid(h.x),Grid(h.y),Grid(h.z)};var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();
        for(int axis=0;axis<3;axis++) for(int sign=-1;sign<=1;sign+=2)
        {
            int u=(axis+1)%3,w=(axis+2)%3,offset=v.Count;
            for(int a=0;a<6;a++) for(int b=0;b<6;b++)
            {
                var p=Vector3.zero;p[axis]=h[axis]*sign;p[u]=grids[u][a];p[w]=grids[w][b];
                var inner=new Vector3(Mathf.Clamp(p.x,-h.x+r,h.x-r),Mathf.Clamp(p.y,-h.y+r,h.y-r),Mathf.Clamp(p.z,-h.z+r,h.z-r));
                var normal=(p-inner).normalized;v.Add(inner+normal*r);n.Add(normal);
            }
            for(int a=0;a<5;a++) for(int b=0;b<5;b++)
            {int i=offset+a*6+b;if(sign>0)t.AddRange(new[]{i,i+6,i+1,i+1,i+6,i+7});else t.AddRange(new[]{i,i+1,i+6,i+1,i+7,i+6});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(t,0);mesh.RecalculateBounds();meshes[key]=Save(key,mesh);return meshes[key];
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,string mat,float radius=.025f)
        =>MeshObject(name,parent,position,Rounded(size,radius),palette[mat]);
    static LineRenderer Stroke(string name,Transform parent,Vector3[] points,string mat,float width,bool loop=false)
    {
        var l=new GameObject(name).AddComponent<LineRenderer>();l.transform.SetParent(parent,false);l.useWorldSpace=false;l.positionCount=points.Length;l.SetPositions(points);
        l.sharedMaterial=palette[mat];l.widthMultiplier=width;l.loop=loop;l.numCapVertices=4;l.shadowCastingMode=ShadowCastingMode.Off;return l;
    }
    static void Palette()
    {
        Mat("Cream","F0DFC0",.25f);Mat("Mint","9DC9B7",.35f);Mat("Trim","478E83",.6f);Mat("Skirting","89877B",.25f);
        Mat("Floor","E9D6AF",0,.2f);Mat("Sun","FFF0C5",0,.65f);Mat("Grout","CABB9C",0,.7f);Mat("Ceiling","E9DFC9",0,.25f);
        Mat("White","FFF4D8",.6f);Mat("Window","AFDDE5",0,1);Mat("Sky","87CEEA",0,1);Mat("Leaf","9ABF67",.2f,.8f);Mat("LeafLight","C4D77C",0,.8f);
        Mat("Wood","E2AD66",.7f);Mat("WoodEdge","B07C48",.7f);Mat("Board","579480",.6f);Mat("Note","F4E6B6",.2f,.4f);Mat("PinkNote","E5B8A3",.2f,.4f);
        Mat("Rack","287A72",1.7f);Mat("RackDark","215C56",1.4f);Mat("Orange","FF9C30",2.8f);Mat("OrangeLight","FFBE4B",.8f);Mat("Ink","45332B",.7f,.8f);
        Mat("Skin","FFC08A",2.4f,.45f);Mat("Hoodie","FF8C2D",2.4f,.15f);Mat("Cuff","E87927",1.4f);Mat("Shorts","3B3E48",2.0f);Mat("Sock","FFF1D5",1.0f);
        Mat("WaterEdge","659BA4",0,.9f);Mat("Gleam","F5FFF3",0,1);Mat("Lamp","FFFADE",0,1);
        var orange=palette["Orange"];orange.SetColor("_OutlineColor",Hex("FFF5D9"));orange.SetFloat("_OutlineWidth",4.0f);
    }
    static void Architecture()
    {
        var hall=Group("School_Hallway",art,Vector3.zero);
        Shape("Warm_Tiled_Floor",hall,new Vector3(0,-.065f,2),new Vector3(5.85f,.12f,18),"Floor");
        var floor=hall.Find("Warm_Tiled_Floor").gameObject.AddComponent<BoxCollider>();floor.size=Vector3.one;
        for(float x=-2.9f;x<=2.91f;x+=.97f) Shape("Fine_Grout",hall,new Vector3(x,.001f,2),new Vector3(.009f,.002f,18),"Grout");
        for(float z=-6;z<=11;z+=1.1f) Shape("Fine_Grout",hall,new Vector3(0,.002f,z),new Vector3(5.8f,.002f,.009f),"Grout");
        Shape("Ceiling",hall,new Vector3(0,3.65f,2),new Vector3(5.95f,.10f,18),"Ceiling");
        for(float z=-5;z<11;z+=1.1f) Shape("Ceiling_Joint",hall,new Vector3(0,3.595f,z),new Vector3(5.8f,.005f,.012f),"Grout");
        for(float x=-1.9f;x<2;x+=.97f) Shape("Ceiling_Joint",hall,new Vector3(x,3.595f,2),new Vector3(.012f,.005f,18),"Grout");
        foreach(int side in new[]{-1,1})
        {
            Shape("Mint_Wainscot",hall,new Vector3(side*2.98f,.59f,2),new Vector3(.18f,1.18f,18),"Mint");
            Shape("Cream_Upper_Wall",hall,new Vector3(side*3,2.37f,2),new Vector3(.18f,2.38f,18),"Cream");
            Box("Wall_Rail",hall,new Vector3(side*2.865f,1.18f,2),new Vector3(.07f,.075f,18),"White",.01f);
            Shape("Baseboard",hall,new Vector3(side*2.865f,.10f,2),new Vector3(.07f,.2f,18),"Skirting");
        }
        for(int i=0;i<6;i++)
        {
            float z=-3.4f+i*2.8f;var window=Group("Tall_Window_"+i,hall,new Vector3(-2.855f,0,z));
            Shape("Sky",window,new Vector3(-.015f,2.35f,0),new Vector3(.03f,2.08f,2.35f),"Sky");
            for(int k=0;k<7;k++) Shape("Garden_Canopy",window,new Vector3(.01f,1.5f+(k%3)*.13f,-.9f+k*.30f),new Vector3(.022f,.52f,.55f),k%2==0?"Leaf":"LeafLight",PrimitiveType.Sphere);
            foreach(float dz in new[]{-1.2f,0,1.2f}) Box("Window_Mullion",window,new Vector3(.06f,2.37f,dz),new Vector3(.095f,2.28f,.055f),"White",.009f);
            foreach(float y in new[]{1.25f,2.72f,3.47f}) Box("Window_Frame",window,new Vector3(.065f,y,0),new Vector3(.1f,.065f,2.48f),"White",.009f);
            Box("Deep_Sill",window,new Vector3(.13f,1.27f,0),new Vector3(.31f,.09f,2.52f),"White");
            // Discrete warm window shapes, aligned on the floor rather than continuous gradients.
            for(int k=0;k<1;k++)
            {
                var sun=Shape("Window_Sunlight",hall,new Vector3(-.28f,.006f,z+.55f),new Vector3(4.96f,.003f,1.85f),"Sun");
                sun.transform.localRotation=Quaternion.Euler(0,-12,0);sun.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        for(int i=0;i<4;i++)
        {
            float z=-.2f+i*3.5f;var door=Group("Classroom_2_"+(i+1),hall,new Vector3(2.82f,0,z));
            Box("Door_Frame",door,new Vector3(.025f,1.38f,0),new Vector3(.12f,2.76f,1.44f),"WoodEdge");
            Box("Classroom_Door",door,new Vector3(-.05f,1.34f,0),new Vector3(.055f,2.63f,1.29f),"Wood");
            Box("Glass_Trim",door,new Vector3(-.087f,1.96f,0),new Vector3(.03f,.85f,.89f),"White",.01f);
            Box("Glass",door,new Vector3(-.11f,1.96f,0),new Vector3(.018f,.73f,.77f),"Window",.01f);
            Box("Door_Handle",door,new Vector3(-.15f,1.02f,-.46f),new Vector3(.07f,.33f,.055f),"Skirting",.015f);
            Box("Classroom_Plate",door,new Vector3(-.25f,2.97f,-.86f),new Vector3(.50f,.37f,.07f),"Trim");
            Box("Classroom_Plate_Face",door,new Vector3(-.25f,2.97f,-.904f),new Vector3(.43f,.29f,.025f),"White",.015f);
            var label=Group("Room_Number",door,new Vector3(-.25f,2.97f,-.927f));
            var text=label.gameObject.AddComponent<TextMeshPro>();text.text="2-"+(i+1);text.fontSize=2.2f;text.color=Hex("408A80");text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(.43f,.30f);
            var board=Group("Class_Board",hall,new Vector3(2.82f,2.05f,z-1.4f));
            Box("Notice_Frame",board,Vector3.zero,new Vector3(.12f,1.12f,1.13f),"WoodEdge");
            Box("Notice_Felt",board,new Vector3(-.08f,0,0),new Vector3(.03f,.98f,.99f),"Board");
            Box("Notice_Paper",board,new Vector3(-.11f,.13f,.22f),new Vector3(.016f,.55f,.31f),"Note",.003f);
            Box("Notice_Paper",board,new Vector3(-.11f,.24f,-.22f),new Vector3(.016f,.33f,.29f),"PinkNote",.003f);
            for(int k=0;k<3;k++) Shape("Notice_Line",board,new Vector3(-.122f,.30f-k*.065f,-.22f),new Vector3(.004f,.017f,.19f),"White");
            Box("Fluorescent_Frame",hall,new Vector3(0,3.51f,z+1),new Vector3(1.32f,.065f,.35f),"Skirting");
            Box("Fluorescent_Panel",hall,new Vector3(0,3.469f,z+1),new Vector3(1.18f,.015f,.25f),"Lamp",.006f);
        }
        Shape("End_Wall",hall,new Vector3(0,1.8f,11.05f),new Vector3(5.9f,3.6f,.14f),"Cream");
        Box("End_Window_Frame",hall,new Vector3(0,1.95f,10.94f),new Vector3(3.3f,2.8f,.12f),"White");
        Box("End_Window",hall,new Vector3(0,1.95f,10.86f),new Vector3(3.1f,2.6f,.03f),"Window");
        for(int i=-1;i<=1;i++) Box("End_Mullion",hall,new Vector3(i,1.95f,10.82f),new Vector3(.05f,2.6f,.04f),"White",.005f);
        Box("End_Transom",hall,new Vector3(0,2.5f,10.82f),new Vector3(3.1f,.05f,.04f),"White",.005f);
    }
    static Vector3[] WaterPoints(Vector2 radius,int count=100)
    {
        var pts=new Vector3[count];for(int i=0;i<count;i++) {float a=i*Mathf.PI*2/count;float edge=1+.065f*Mathf.Sin(a*5)+.035f*Mathf.Sin(a*9);pts[i]=new Vector3(Mathf.Cos(a)*radius.x*edge,0,Mathf.Sin(a)*radius.y*edge);}return pts;
    }
    static void Puddle(WetCorridorPrototype p)
    {
        var water=Group("Broad_Wet_Floor",art,CorridorComposition.WaterCenter);
        var edge=WaterPoints(CorridorComposition.WaterRadius);var v=new List<Vector3>{Vector3.zero};v.AddRange(edge);var t=new List<int>();
        for(int i=0;i<edge.Length;i++) t.AddRange(new[]{0,(i+1)%edge.Length+1,i+1});
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"/Materials/IllustratedWater.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null) {mat=new Material(Shader.Find("ProjectWait/Illustrated Water"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",new Color(.32f,.62f,.67f,.72f));EditorUtility.SetDirty(mat);
        MeshObject("Window_Reflections",water,Vector3.zero,Save("IrregularPuddle",mesh),mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        Stroke("Soft_Water_Edge",water,edge,"WaterEdge",.015f,true);
        for(int i=0;i<7;i++)
        {
            float x=-1.5f+i*.44f,z=(i%2==0?-.90f:1.02f);
            Shape("Water_Drop",water,new Vector3(x,.002f,z),new Vector3(.16f+(i%3)*.06f,.006f,.10f),"WaterEdge",PrimitiveType.Sphere);
        }
        for(int i=0;i<3;i++) Stroke("Water_Highlight",water,new[]{new Vector3(-.8f+i*.64f,.008f,-.64f),new Vector3(-.58f+i*.64f,.008f,-.62f),new Vector3(-.51f+i*.64f,.008f,-.57f)},"Gleam",.014f);
        foreach(var line in p.causeHighlight.GetComponentsInChildren<LineRenderer>(true))
        {line.transform.localPosition=CorridorComposition.WaterCenter;line.transform.localRotation=Quaternion.identity;line.transform.localScale=Vector3.one;line.useWorldSpace=false;line.positionCount=edge.Length;line.SetPositions(edge);line.loop=true;}
    }
    static Mesh Tube(string name,Vector3[] points,float radius)
    {
        var v=new List<Vector3>();var t=new List<int>();const int sides=10;
        for(int i=0;i<points.Length;i++)
        {
            var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;var a=Vector3.Cross(tangent,Vector3.forward).normalized;var b=Vector3.Cross(tangent,a);
            for(int j=0;j<sides;j++) {float angle=j*Mathf.PI*2/sides;v.Add(points[i]+(a*Mathf.Cos(angle)+b*Mathf.Sin(angle))*radius);}
            if(i>0) for(int j=0;j<sides;j++) {int k=(i-1)*sides+j,n=(i-1)*sides+(j+1)%sides;t.AddRange(new[]{k,n,k+sides,n,n+sides,k+sides});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Save(name,mesh);
    }
    static void CleaningTools(WetCorridorPrototype p)
    {
        Mat("BucketTeal","58A99A",1.8f,.12f);Mat("BucketInterior","32796F",1.2f,.15f);
        Mat("CleaningAccent","F7AE48",1.2f,.18f);Mat("MopCotton","E7E9D7",1.0f,.25f);Mat("MopCottonShade","B8CCC3",.8f,.25f);
        var tools=Group("Cleaning_Tools",art,new Vector3(-1.55f,0,.45f));
        var bucket=Group("Open_Bucket",tools,Vector3.zero);
        // Continuous outer wall, rounded lip and inner wall: the pail is visibly hollow.
        var profile=new[]{new Vector2(0,.04f),new Vector2(.25f,.04f),new Vector2(.275f,.075f),new Vector2(.345f,.51f),new Vector2(.355f,.55f),new Vector2(.345f,.58f),new Vector2(.315f,.58f),new Vector2(.305f,.55f),new Vector2(.305f,.51f),new Vector2(.235f,.12f),new Vector2(0,.12f)};
        var vertices=new List<Vector3>();var triangles=new List<int>();const int sides=40;
        foreach(var ring in profile) for(int j=0;j<=sides;j++) {float a=j*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*ring.x,ring.y,Mathf.Sin(a)*ring.x));}
        for(int i=0;i<profile.Length-1;i++) for(int j=0;j<sides;j++) {int a=i*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;triangles.AddRange(new[]{a,c,b,b,c,d});}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        MeshObject("Hollow_Pail",bucket,Vector3.zero,Save("HollowCleaningBucket",mesh),palette["BucketTeal"]);
        Shape("Water_Inside",bucket,new Vector3(0,.25f,0),new Vector3(.51f,.008f,.51f),"BucketInterior",PrimitiveType.Cylinder);
        for(int side=-1;side<=1;side+=2) Shape("Handle_Pivot",bucket,new Vector3(side*.34f,.47f,0),new Vector3(.075f,.075f,.055f),"CleaningAccent",PrimitiveType.Sphere);
        var handle=new List<Vector3>();for(int i=0;i<=32;i++) {float a=Mathf.PI-i*Mathf.PI/32;handle.Add(new Vector3(Mathf.Cos(a)*.35f,.49f+Mathf.Sin(a)*.31f,0));}
        MeshObject("Swing_Handle",bucket,Vector3.zero,Tube("BucketSwingHandle",handle.ToArray(),.017f),palette["RackDark"]);
        var grip=Shape("Handle_Grip",bucket,new Vector3(0,.795f,0),new Vector3(.065f,.09f,.065f),"CleaningAccent",PrimitiveType.Capsule);grip.transform.localRotation=Quaternion.Euler(0,0,90);
        Box("Pail_Front_Badge",bucket,new Vector3(0,.32f,-.319f),new Vector3(.17f,.14f,.018f),"White",.04f);
        var mop=Group("String_Mop",tools,Vector3.zero);PlaceMopAtWall(mop);
        Shape("Wooden_Handle",mop,new Vector3(0,.82f,0),new Vector3(.043f,.68f,.043f),"Wood",PrimitiveType.Cylinder);
        Shape("Top_Grip",mop,new Vector3(0,1.50f,0),new Vector3(.062f,.075f,.062f),"CleaningAccent",PrimitiveType.Capsule);
        Box("Mop_Head_Clamp",mop,new Vector3(0,.13f,0),new Vector3(.32f,.085f,.15f),"Trim",.025f);
        Shape("Mop_Collar",mop,new Vector3(0,.19f,0),new Vector3(.095f,.07f,.095f),"CleaningAccent",PrimitiveType.Cylinder);
        for(int i=0;i<13;i++)
        {
            float x=(i-6)*.024f,z=(i%3-1)*.045f;
            var strand=new[]{new Vector3(x,.13f,z),new Vector3(x*1.15f,.07f,z-.03f),new Vector3(x*1.5f,.034f,z-.12f),new Vector3(x*1.7f,.029f,z-.21f-(i%3)*.022f)};
            MeshObject("Cotton_Strand_"+i,mop,Vector3.zero,Tube("MopStrand_"+i,strand,.023f),palette[i%3==0?"MopCottonShade":"MopCotton"]);
            Shape("Soft_Strand_End",mop,strand[3],Vector3.one*.046f,i%3==0?"MopCottonShade":"MopCotton",PrimitiveType.Sphere);
        }
        p.cleaningToolsClue=tools;
    }
    static void PlaceMopAtWall(Transform mop)
    {
        // Keep the mop near the bucket, leaning on the adjacent wall pier.
        mop.SetPositionAndRotation(new Vector3(-2.48f,.025f,.8f),Quaternion.Euler(0,0,10));
    }

    public static object MoveMopToWall()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before moving the mop.");
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(p==null || p.gameObject.scene.path!="Assets/Test/Scenes/WetCorridor_Test.unity") throw new Exception("Open WetCorridor_Test.");
        var mop=p.cleaningToolsClue.Find("String_Mop");if(mop==null) throw new Exception("String mop is missing.");
        Undo.RecordObject(mop,"Lean mop against the left wall");PlaceMopAtWall(mop);
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        return new {mop=mop.position.ToString(),bucket=p.cleaningToolsClue.Find("Open_Bucket").position.ToString()};
    }
    static void Character(WetCorridorPrototype p)
    {
        p.friend.position=CorridorComposition.FriendStart;p.friend.localScale=Vector3.one*1.1f;
        var old=p.friend.Find("Reference_Character");if(old!=null) Object.DestroyImmediate(old.gameObject);
        var details=Group("Reference_Character",p.friend,Vector3.zero);
        foreach(var pair in new[]{("Skin",.35f),("OrangeHoodie",.18f),("Hair",.12f)})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Test/Art/Materials/"+pair.Item1+".mat");
            if(material!=null) {material.SetFloat("_EmissionStrength",pair.Item2);EditorUtility.SetDirty(material);}
        }
        foreach(var arm in new[]{p.leftArm,p.rightArm})
        {
            float side=arm==p.leftArm?-1:1;arm.localRotation=Quaternion.Euler(0,0,-side*8);
            arm.Find("Sleeve").localScale=new Vector3(.27f,.15f,.29f);arm.Find("Sleeve").localPosition=new Vector3(0,-.15f,.01f);
            arm.Find("Hand").localPosition=new Vector3(-side*.08f,-.17f,.25f);arm.Find("Hand").localScale=new Vector3(.19f,.22f,.17f);
            var cuff=arm.Find("Story_Cuff");if(cuff!=null) cuff.localPosition=new Vector3(-side*.06f,-.20f,.18f);
            var thumb=arm.Find("Story_Thumb");if(thumb!=null) thumb.localPosition=new Vector3(-side*.14f,-.13f,.29f);
            var forearm=arm.Find("Bent_Forearm");if(forearm!=null) Object.DestroyImmediate(forearm.gameObject);
            var sleeve=Shape("Bent_Forearm",arm,new Vector3(-side*.035f,-.29f,.12f),new Vector3(.25f,.19f,.27f),"Hoodie",PrimitiveType.Sphere);sleeve.transform.localRotation=Quaternion.Euler(-40,0,side*10);
            Box("Backpack_Strap",details,new Vector3(side*.27f,1.11f,.245f),new Vector3(.085f,.49f,.075f),"RackDark",.03f);
            Box("Strap_Edge",details,new Vector3(side*.27f,1.11f,.288f),new Vector3(.032f,.43f,.018f),"Trim",.007f);
        }
        foreach(var leg in new[]{p.leftLeg,p.rightLeg})
        {
            foreach(string name in new[]{"Bare_Shin","Ankle_Sock"}) {var oldPart=leg.Find(name);if(oldPart!=null) Object.DestroyImmediate(oldPart.gameObject);}
            leg.Find("TrouserLeg").localPosition=new Vector3(0,-.015f,0);leg.Find("TrouserLeg").localScale=new Vector3(.29f,.10f,.30f);
            leg.Find("TrouserLeg").GetComponent<Renderer>().sharedMaterial=palette["Shorts"];
            Shape("Bare_Shin",details,new Vector3(leg.localPosition.x,.315f,.0f),new Vector3(.17f,.25f,.19f),"Skin",PrimitiveType.Capsule).transform.SetParent(leg,true);
            var shin=leg.GetChild(leg.childCount-1);shin.localPosition=new Vector3(0,-.24f,0);shin.localScale=new Vector3(.17f,.10f,.19f);
            Shape("Ankle_Sock",leg,new Vector3(0,-.34f,.015f),new Vector3(.20f,.13f,.23f),"Sock",PrimitiveType.Sphere);
        }
        // Two swept tufts break the regular cap silhouette.
        Shape("Hair_Flick",details,new Vector3(-.14f,2.19f,-.02f),new Vector3(.12f,.24f,.14f),"Ink",PrimitiveType.Sphere).transform.localRotation=Quaternion.Euler(0,0,-42);
        Shape("Hair_Flick",details,new Vector3(.10f,2.23f,-.05f),new Vector3(.10f,.21f,.13f),"Ink",PrimitiveType.Sphere).transform.localRotation=Quaternion.Euler(0,0,35);
    }
    static void Hands(WetCorridorPrototype p)
    {
        var previous=p.playerCamera.transform.Find("Desktop_Story_Hands");if(previous!=null) Object.DestroyImmediate(previous.gameObject);
        var root=Group("Desktop_Story_Hands",p.playerCamera.transform,Vector3.zero);
        var driver=root.gameObject.AddComponent<CorridorFirstPersonHands>();var visuals=Group("Visuals",root,Vector3.zero);driver.visuals=visuals.gameObject;driver.prototype=p;
        foreach(int side in new[]{-1,1})
        {
            var hand=Group(side<0?"Left_Open_Hand":"Right_Open_Hand",visuals,new Vector3(side<0?-.48f:.12f,-.46f,.78f));
            hand.localScale=Vector3.one*.85f;
            hand.localRotation=Quaternion.Euler(-15,side*-12,side*-20);
            Shape("Palm",hand,Vector3.zero,new Vector3(.20f,.21f,.065f),"Skin",PrimitiveType.Sphere);
            for(int i=0;i<4;i++)
            {
                var finger=Shape("Finger",hand,new Vector3(-.068f+i*.045f,.12f+Mathf.Sin((i+1)*Mathf.PI/5)*.027f,0),new Vector3(.051f,.105f,.055f),"Skin",PrimitiveType.Capsule);
                finger.transform.localRotation=Quaternion.Euler(0,0,(1.5f-i)*8);
            }
            Shape("Thumb",hand,new Vector3(-side*.105f,-.012f,0),new Vector3(.065f,.125f,.065f),"Skin",PrimitiveType.Capsule).transform.localRotation=Quaternion.Euler(0,0,side*48);
            Shape("Sleeve",hand,new Vector3(side*.015f,-.28f,.035f),new Vector3(.26f,.28f,.16f),"Hoodie",PrimitiveType.Capsule);
            Box("Ribbed_Cuff",hand,new Vector3(0,-.115f,.014f),new Vector3(.23f,.075f,.13f),"Cuff",.03f);
        }
        foreach(var renderer in root.GetComponentsInChildren<Renderer>()) {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
    }
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before rebuilding the corridor.");
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();if(p==null || p.gameObject.scene.path!="Assets/Test/Scenes/WetCorridor_Test.unity") throw new Exception("Open WetCorridor_Test.");
        System.IO.Directory.CreateDirectory(Root+"/Materials");System.IO.Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();
        Undo.SetCurrentGroupName("Recompose reference corridor");Undo.RegisterFullObjectHierarchyUndo(p.gameObject,"Recompose reference corridor");
        foreach(string name in new[]{"SchoolCorridor","Storybook_Art"}) {var original=p.transform.Find(name);if(original!=null) original.gameObject.SetActive(false);}
        var old=p.transform.Find("Reference_Corridor");if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);
        art=Group("Reference_Corridor",p.transform,Vector3.zero);Undo.RegisterCreatedObjectUndo(art.gameObject,"Build reference corridor");palette.Clear();meshes.Clear();Palette();
        Architecture();Puddle(p);CleaningTools(p);Character(p);Hands(p);
        p.pathHighlight.transform.parent.position=new Vector3(1.1f,.06f,-.7f);
        p.playerCamera.fieldOfView=CorridorComposition.FieldOfView;p.playerCamera.transform.rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(light.type==LightType.Directional) {light.transform.rotation=Quaternion.Euler(48,55,0);light.intensity=1;light.color=Hex("FFF0CF");}
        CorridorUITheme.Apply(p);foreach(var button in p.GetComponentsInChildren<UnityEngine.UI.Button>(true)) CorridorUITheme.StyleButton(button);
        EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        return new {scene=p.gameObject.scene.path,renderers=art.GetComponentsInChildren<Renderer>().Length,friend=p.friend.position.ToString(),cleaningTools=p.cleaningToolsClue.position.ToString(),canvases=p.GetComponentsInChildren<Canvas>(true).Select(c=>new{name=c.name,size=((RectTransform)c.transform).sizeDelta.ToString()}).ToArray()};
    }

    public static object ReplaceCleaningProps()
    {
        if(Application.isPlaying) throw new Exception("Stop Play mode before editing props.");
        var p=Object.FindFirstObjectByType<WetCorridorPrototype>();
        if(p==null || p.gameObject.scene.path!="Assets/Test/Scenes/WetCorridor_Test.unity") throw new Exception("Open WetCorridor_Test.");
        art=p.transform.Find("Reference_Corridor");if(art==null) throw new Exception("Reference corridor is missing.");
        Undo.SetCurrentGroupName("Replace umbrellas with cleaning tools");Undo.RegisterFullObjectHierarchyUndo(p.gameObject,"Replace cleaning props");
        foreach(string name in new[]{"Umbrella_Stand","Cleaning_Tools"}) {var old=art.Find(name);if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);}
        palette.Clear();meshes.Clear();Palette();CleaningTools(p);Undo.RegisterCreatedObjectUndo(p.cleaningToolsClue.gameObject,"Create bucket and mop");
        EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        return new {scene=p.gameObject.scene.path,cleaningTools=p.cleaningToolsClue.position.ToString(),umbrellaRemoved=art.Find("Umbrella_Stand")==null};
    }
}
