using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MoveTestAssets
{
    const string Work="Assets/M/CodexTemp/AssetMigration~/";
    const string Source="Assets/M";
    const string Temp="Assets/M/CodexTemp";
    class Move { public string source,destination; }
    class Asset { public string source,destination,guid,hash; }
    class Product { public string path,hash; }
    class PlanData { public List<Move> moves=new List<Move>();public List<string> mergedFolders=new List<string>();public List<Asset> assets=new List<Asset>(); public List<Product> products=new List<Product>(); }
    static string Slash(string s)=>s.Replace('\\','/');
    static string Hash(string path) {using(var sha=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","");}
    static string Destination(string path)
    {
        if(path==Source+"/Arts"||path.StartsWith(Source+"/Arts/"))return "Assets/Test/Art"+path.Substring((Source+"/Arts").Length);
        if(path==Source+"/Scene"||path.StartsWith(Source+"/Scene/"))return "Assets/Test/Scenes"+path.Substring((Source+"/Scene").Length);
        if(path==Source+"/Scripts"||path.StartsWith(Source+"/Scripts/"))return "Assets/Test/Scripts"+path.Substring((Source+"/Scripts").Length);
        return "Assets/Test"+path.Substring(Source.Length);
    }
    static void CheckPaths(string from,string to)
    {
        var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var a=Path.GetFullPath(Path.Combine(project,from));var b=Path.GetFullPath(Path.Combine(project,to));
        var scope=Path.GetFullPath(Path.Combine(project,"Assets/M"))+Path.DirectorySeparatorChar;
        var target=Path.GetFullPath(Path.Combine(project,"Assets/Test"))+Path.DirectorySeparatorChar;
        if(!a.StartsWith(scope,StringComparison.OrdinalIgnoreCase)||from==Temp||from.StartsWith(Temp+"/")||!b.StartsWith(target,StringComparison.OrdinalIgnoreCase))throw new Exception("Move outside authorized paths: "+from+" → "+to);
    }
    static void Add(PlanData p,string from,string to)
    {
        CheckPaths(from,to);
        if(Directory.Exists(from)&&Directory.Exists(to))
        {
            p.mergedFolders.Add(from);
            foreach(var child in Directory.GetFileSystemEntries(from).Select(Slash).Where(x=>!x.EndsWith(".meta")).OrderBy(x=>x))Add(p,child,to+"/"+Path.GetFileName(child));
        }
        else
        {
            if(File.Exists(to)||Directory.Exists(to)||File.Exists(to+".meta"))throw new Exception("Preserving existing destination; collision at "+to);
            var error=AssetDatabase.ValidateMoveAsset(from,to);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
            p.moves.Add(new Move{source=from,destination=to});
        }
    }
    static PlanData MakePlan()
    {
        var p=new PlanData();
        foreach(var path in Directory.GetFileSystemEntries(Source).Select(Slash).Where(x=>x!=Temp&&!x.EndsWith(".meta")).OrderBy(x=>x))Add(p,path,Destination(path));
        foreach(var path in Directory.GetFiles(Source,"*",SearchOption.AllDirectories).Select(Slash).Where(x=>!x.StartsWith(Temp+"/")&&!x.EndsWith(".meta")).OrderBy(x=>x))
            p.assets.Add(new Asset{source=path,destination=Destination(path),guid=AssetDatabase.AssetPathToGUID(path),hash=Hash(path)});
        foreach(var path in Directory.GetFiles(Temp,"*",SearchOption.AllDirectories).Select(Slash).Where(x=>!x.StartsWith(Work)).OrderBy(x=>x))
            p.products.Add(new Product{path=path,hash=Hash(path)});
        return p;
    }
    public static object Plan()
    {
        Directory.CreateDirectory(Work);
        var p=MakePlan();
        File.WriteAllText(Work+"Plan.json",JsonConvert.SerializeObject(p,Formatting.Indented));
        return JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{path=s.path,dirty=s.isDirty}).ToArray(),moves=p.moves,formalFiles=p.assets.Count,productsKept=p.products.Count,mergedFolders=p.mergedFolders},Formatting.Indented);
    }
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before moving assets");
        for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty&&s.path.StartsWith(Source+"/")&&!s.path.StartsWith(Temp+"/"))EditorSceneManager.SaveScene(s);}
        var p=MakePlan();File.WriteAllText(Work+"Plan.json",JsonConvert.SerializeObject(p,Formatting.Indented));
        var moved=new List<Move>();
        EditorApplication.LockReloadAssemblies();
        try
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var m in p.moves)
                {
                    var error=AssetDatabase.MoveAsset(m.source,m.destination);
                    if(!string.IsNullOrEmpty(error))throw new Exception(error);
                    moved.Add(m);
                }
                // Only remove the now-empty source folders that were merged into existing folders.
                foreach(var folder in p.mergedFolders.OrderByDescending(s=>s.Length))
                {
                    CheckPaths(folder,Destination(folder));
                    if(Directory.GetFileSystemEntries(folder).Length!=0)throw new Exception("Source folder is not empty: "+folder);
                    if(!AssetDatabase.DeleteAsset(folder))throw new Exception("Could not remove empty folder: "+folder);
                }
                var guide="Assets/Test/Scenes/친구_테스트_안내.txt";
                if(File.Exists(guide))
                {
                    var t=File.ReadAllText(guide).Replace("Assets/M/Scene/","Assets/Test/Scenes/").Replace("Assets/M/Arts/","Assets/Test/Art/").Replace("M 폴더에 복사","테스트 폴더에 복사");
                    File.WriteAllText(guide,t,new System.Text.UTF8Encoding(false));
                }
                var manifest="Assets/Test/Art/Models/Friend/Friend_MaterialManifest.json";
                if(File.Exists(manifest))
                {
                    var t=File.ReadAllText(manifest).Replace("Assets/M/Arts/","Assets/Test/Art/").Replace(@"Assets\\M\\Arts\\",@"Assets\\Test\\Art\\");
                    File.WriteAllText(manifest,t,new System.Text.UTF8Encoding(false));
                }
            }
            finally {AssetDatabase.StopAssetEditing();}
            // Verify every file and each asset GUID, including the ignored Blender source.
            foreach(var a in p.assets)
            {
                if(!File.Exists(a.destination)||File.Exists(a.source))throw new Exception("File did not move: "+a.source);
                if(!string.IsNullOrEmpty(a.guid)&&AssetDatabase.AssetPathToGUID(a.destination)!=a.guid)throw new Exception("GUID changed: "+a.destination);
                if(!a.destination.EndsWith("친구_테스트_안내.txt")&&!a.destination.EndsWith("Friend_MaterialManifest.json")&&Hash(a.destination)!=a.hash)throw new Exception("File bytes changed: "+a.destination);
            }
            var changed=p.products.Where(a=>!File.Exists(a.path)||Hash(a.path)!=a.hash).Select(a=>a.path).ToArray();
            if(changed.Length>0)throw new Exception("Byproducts changed: "+string.Join(",",changed));
            var result=new{pass=true,filesMoved=p.assets.Count,moveOperations=moved.Count,byproductFilesKept=p.products.Count,byproductsChanged=changed,remainingInM=Directory.GetFileSystemEntries(Source).Select(Path.GetFileName).OrderBy(s=>s).ToArray()};
            File.WriteAllText(Work+"Migration_Result.json",JsonConvert.SerializeObject(result,Formatting.Indented));
            return JsonConvert.SerializeObject(result,Formatting.Indented);
        }
        finally{EditorApplication.UnlockReloadAssemblies();}
    }
    public static object Verify()
    {
        var p=JsonConvert.DeserializeObject<PlanData>(File.ReadAllText(Work+"Plan.json"));
        var errors=new List<string>();
        foreach(var a in p.assets)
        {
            if(!File.Exists(a.destination))errors.Add("Missing file: "+a.destination);
            if(!string.IsNullOrEmpty(a.guid)&&AssetDatabase.GUIDToAssetPath(a.guid)!=a.destination)errors.Add("GUID mismatch: "+a.destination);
        }
        var scenePaths=p.assets.Where(a=>a.destination.EndsWith(".unity")).Select(a=>a.destination).ToArray();
        var sceneResults=new List<object>();
        foreach(var path in scenePaths)
        {
            var loaded=SceneManager.GetSceneByPath(path);bool opened=!loaded.IsValid()||!loaded.isLoaded;
            var s=opened?EditorSceneManager.OpenScene(path,OpenSceneMode.Additive):loaded;
            int missing=s.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            int badMaterials=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Sum(r=>r.sharedMaterials.Count(m=>m==null||m.shader==null||!m.shader.isSupported));
            var dependencies=AssetDatabase.GetDependencies(path,true);
            var stale=dependencies.Where(d=>d.StartsWith(Source+"/")).ToArray();
            if(missing>0||badMaterials>0||stale.Length>0)errors.Add("Scene references failed: "+path);
            sceneResults.Add(new{path,missingScripts=missing,badMaterials,staleMDependencies=stale});
            if(opened)EditorSceneManager.CloseScene(s,true);
        }
        foreach(var prefab in p.assets.Where(a=>a.destination.EndsWith(".prefab")))
        {
            var go=PrefabUtility.LoadPrefabContents(prefab.destination);
            try{if(go.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))>0)errors.Add("Missing prefab script: "+prefab.destination);}
            finally{PrefabUtility.UnloadPrefabContents(go);}
            if(AssetDatabase.GetDependencies(prefab.destination,true).Any(d=>d.StartsWith(Source+"/")))errors.Add("Old M dependency: "+prefab.destination);
        }
        var changed=p.products.Where(a=>!File.Exists(a.path)||Hash(a.path)!=a.hash).Select(a=>a.path).ToArray();
        if(changed.Length>0)errors.Add("Byproducts changed: "+string.Join(",",changed));
        var result=new{pass=errors.Count==0,filesVerified=p.assets.Count,byproductsVerified=p.products.Count,sceneResults,errors,activeScene=SceneManager.GetActiveScene().path};
        File.WriteAllText(Work+"Verification.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        if(errors.Count>0)throw new Exception(JsonConvert.SerializeObject(result));
        return JsonConvert.SerializeObject(result,Formatting.Indented);
    }
}
