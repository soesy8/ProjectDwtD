using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Newtonsoft.Json;

public static class ValidateElevatorAssets
{
    public static object Run()
    {
        const string shared="Assets/Shared Assets/Models/Friend/Wait_Friend.prefab";
        var dependencies=AssetDatabase.GetDependencies(shared,true);
        var lesson=UnityEngine.Object.FindFirstObjectByType<ElevatorLesson>();
        var missing=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0).Select(t=>t.name).ToArray();
        var badMaterials=lesson.friend.GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||!m.shader.isSupported)).Select(r=>r.name).ToArray();
        var report=new{
            scene=SceneManager.GetActiveScene().path,saved=!SceneManager.GetActiveScene().isDirty,
            sharedPrefab=shared,sharedDependencies=dependencies,
            sharedIsSelfContained=dependencies.All(p=>p.StartsWith("Assets/Shared Assets/")||!p.StartsWith("Assets/")),
            sourceGuid=AssetDatabase.AssetPathToGUID("Assets/Test/Art/Models/Friend/Wait_Friend_Edited.fbx"),
            sharedGuid=AssetDatabase.AssetPathToGUID("Assets/Shared Assets/Models/Friend/Wait_Friend_Edited.fbx"),
            sourceBlendSaved=File.Exists("Assets/Shared Assets/Models/Friend/Source~/Wait_Friend_Edited.blend"),
            missingScripts=missing,badMaterials,
            requiredReferencesPresent=lesson.settings&&lesson.view&&lesson.friend&&lesson.floorLabel&&lesson.cabinLight&&lesson.motor&&lesson.signal&&lesson.motorClip&&lesson.stopClip&&lesson.playerCamera,
            observationCount=lesson.GetComponentsInChildren<ElevatorObservation>(true).Length,
            runtimeDoesNotDependOnCodexTemp=AssetDatabase.GetDependencies(SceneManager.GetActiveScene().path,true).All(p=>!p.StartsWith("Assets/M/CodexTemp/")),
            compiled=!EditorUtility.scriptCompilationFailed,
            checkedUtc=DateTime.UtcNow.ToString("o")};
        File.WriteAllText("Assets/M/CodexTemp/ElevatorQA/AssetValidation.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        if(missing.Length>0||badMaterials.Length>0||!report.requiredReferencesPresent||!report.sharedIsSelfContained||!report.runtimeDoesNotDependOnCodexTemp||!report.saved||!report.compiled)throw new Exception("Asset validation failed; see report");
        return report;
    }
}
