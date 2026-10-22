using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ExportFriendForBlender
{
    static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    static float[] C(Color c) => new[] { c.r, c.g, c.b, c.a };
    static string MaterialId(Material m) => m == null ? "missing" : AssetDatabase.GetAssetPath(m) + "#" + m.name;
    static object MaterialData(Material m)
    {
        var floats = new Dictionary<string, float>();
        foreach (var key in new[] { "_MidTone", "_ShadowThreshold", "_LightThreshold", "_BandSoftness", "_OutlineWidth", "_EmissionStrength" })
            if (m.HasProperty(key)) floats[key] = m.GetFloat(key);
        var colors = new Dictionary<string, float[]>();
        foreach (var key in new[] { "_BaseColor", "_Color", "_ShadeColor", "_OutlineColor" })
            if (m.HasProperty(key)) colors[key] = C(m.GetColor(key));
        return new { id = MaterialId(m), name = m.name, path = AssetDatabase.GetAssetPath(m), shader = m.shader.name, floats, colors,
            texture = m.mainTexture == null ? null : AssetDatabase.GetAssetPath(m.mainTexture) };
    }
    public static object Run()
    {
        const string source = "Assets/Test/Scenes/WetCorridor_Test.unity";
        const string output = "Assets/M/CodexTemp/BlenderExport~/Friend_Milo_Source.json";
        var scene = EditorSceneManager.OpenPreviewScene(source);
        try
        {
            var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Friend_Milo");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var materials = new Dictionary<string, Material>();
            var parts = new List<object>();
            var warnings = new List<string>();
            int meshCount = 0, vertexCount = 0;
            foreach (var t in transforms)
            {
                var filter = t.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                var renderer = t.GetComponent<Renderer>();
                var line = t.GetComponent<LineRenderer>();
                object geometry = null;
                if (mesh != null)
                {
                    if (!mesh.isReadable) throw new Exception("Unreadable mesh: " + t.name);
                    var submeshes = Enumerable.Range(0, mesh.subMeshCount).Select(i => {
                        if (mesh.GetTopology(i) != UnityEngine.MeshTopology.Triangles) throw new Exception("Unsupported mesh topology: " + t.name);
                        return mesh.GetTriangles(i);
                    }).ToArray();
                    geometry = new { name = mesh.name, path = AssetDatabase.GetAssetPath(mesh),
                        vertices = mesh.vertices.Select(V).ToArray(), normals = mesh.normals.Select(V).ToArray(),
                        uv = mesh.uv.Select(uv => new[] { uv.x, uv.y }).ToArray(), submeshes };
                    meshCount++; vertexCount += mesh.vertexCount;
                }
                object lineData = null;
                if (line != null)
                {
                    var positions = new Vector3[line.positionCount]; line.GetPositions(positions);
                    if (line.useWorldSpace) positions = positions.Select(t.InverseTransformPoint).ToArray();
                    lineData = new { positions = positions.Select(V).ToArray(), width = line.startWidth,
                        endWidth = line.endWidth, loop = line.loop, worldSpace = line.useWorldSpace };
                    if (Mathf.Abs(line.startWidth - line.endWidth) > 0.00001f) warnings.Add("Tapered line approximated: " + t.name);
                }
                if (renderer != null)
                    foreach (var material in renderer.sharedMaterials.Where(m => m != null)) materials[MaterialId(material)] = material;
                parts.Add(new {
                    id = t.GetInstanceID().ToString(), name = t.name,
                    parent = t == root ? null : t.parent.GetInstanceID().ToString(),
                    localPosition = t == root ? new float[] { 0, 0, 0 } : V(t.localPosition),
                    localRotation = t == root ? new float[] { 0, 0, 0, 1 } : new[] { t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w },
                    localScale = V(t.localScale), active = t.gameObject.activeInHierarchy,
                    visible = t.gameObject.activeInHierarchy && (renderer == null || renderer.enabled),
                    materials = renderer == null ? new string[0] : renderer.sharedMaterials.Select(MaterialId).ToArray(),
                    geometry, line = lineData
                });
            }
            var bounds = new Bounds(); bool initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy))
            {
                if (!initialized) { bounds = renderer.bounds; initialized = true; } else bounds.Encapsulate(renderer.bounds);
            }
            var data = new { format = "ProjectWait.FriendBlender.v1", sourceScene = source,
                unityVersion = Application.unityVersion, rootName = root.name, rootPosition = V(root.position), rootRotation = new[] { root.rotation.x, root.rotation.y, root.rotation.z, root.rotation.w },
                boundsSize = V(bounds.size), hasArmature = root.GetComponentInChildren<SkinnedMeshRenderer>(true) != null,
                parts, materials = materials.Values.Select(MaterialData).ToArray(), warnings };
            File.WriteAllText(output, JsonConvert.SerializeObject(data, Formatting.None), new System.Text.UTF8Encoding(false));
            return new { output, parts = parts.Count, meshes = meshCount, vertices = vertexCount,
                materials = materials.Count, bounds = bounds.size.ToString(), warnings };
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
