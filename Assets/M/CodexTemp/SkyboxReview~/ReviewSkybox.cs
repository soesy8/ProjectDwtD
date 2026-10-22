using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ReviewStorybookSkybox
{
    public static object Run()
    {
        const string texturePath = "Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png";
        const string reviewRoot = "Assets/M/CodexTemp/SkyboxCityReview";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Skybox verification requires Edit Mode.");

        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 1024;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = true;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.wrapModeW = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        var platform = importer.GetDefaultPlatformTextureSettings();
        platform.maxTextureSize = 1024;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platform);
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null || texture.width != 1024 || texture.height != 512)
            throw new InvalidOperationException("The imported texture is not 1024 x 512.");
        var shader = Shader.Find("Skybox/Panoramic");
        if (shader == null || !shader.isSupported)
            throw new InvalidOperationException("Skybox/Panoramic is not available.");
        var shaderErrors = ShaderUtil.GetShaderMessages(shader)
            .Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
        if (shaderErrors.Length != 0) throw new InvalidOperationException(string.Join("; ", shaderErrors));
        if (!AssetDatabase.IsValidFolder(reviewRoot))
            AssetDatabase.CreateFolder("Assets/M/CodexTemp", "SkyboxCityReview");
        var material = AssetDatabase.LoadAssetAtPath<Material>(reviewRoot + "/PreviewSkybox.mat");
        bool isNewMaterial = material == null;
        if (isNewMaterial) material = new Material(shader) { name = "PreviewSkybox" };
        material.SetTexture("_MainTex", texture);
        material.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1));
        material.SetFloat("_Exposure", 1);
        material.SetFloat("_Rotation", 0);
        material.SetFloat("_Mapping", 1);
        material.SetFloat("_ImageType", 0);
        material.SetFloat("_Layout", 0);
        if (isNewMaterial) AssetDatabase.CreateAsset(material, reviewRoot + "/PreviewSkybox.mat");
        else { EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); }

        var originalScene = SceneManager.GetActiveScene();
        var originalSelection = Selection.objects;
        var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var activeTarget = RenderTexture.active;
        RenderTexture target = null;
        Texture2D atlas = null;
        try
        {
            SceneManager.SetActiveScene(preview);
            RenderSettings.skybox = material;
            var cameraObject = new GameObject("Skybox Review Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.cullingMask = 0;
            camera.fieldOfView = 90;
            camera.aspect = 512f / 384;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var skybox = cameraObject.AddComponent<Skybox>();
            skybox.material = material;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            EditorSceneManager.SaveScene(preview, reviewRoot + "/Preview.unity");

            target = new RenderTexture(512, 384, 24, RenderTextureFormat.ARGB32);
            target.Create();
            atlas = new Texture2D(1536, 768, TextureFormat.RGB24, false);
            var views = new[] {
                Quaternion.Euler(0, 0, 0), Quaternion.Euler(0, 90, 0),
                Quaternion.Euler(0, 180, 0), Quaternion.Euler(0, 270, 0),
                Quaternion.Euler(-90, 0, 0), Quaternion.Euler(90, 0, 0)
            };
            camera.targetTexture = target;
            for (int i = 0; i < views.Length; i++)
            {
                camera.transform.rotation = views[i];
                camera.Render();
                RenderTexture.active = target;
                atlas.ReadPixels(new Rect(0, 0, 512, 384), (i % 3) * 512, (1 - i / 3) * 384, false);
            }
            atlas.Apply();
            File.WriteAllBytes(reviewRoot + "/Skybox_Views.png", atlas.EncodeToPNG());
            var report = new {
                success = true,
                texture = texturePath,
                width = texture.width, height = texture.height,
                projection = "2:1 equirectangular 360 x 180 degrees",
                shader = shader.name,
                shaderErrors,
                textureGuid = AssetDatabase.AssetPathToGUID(texturePath),
                sRGB = importer.sRGBTexture,
                compression = importer.textureCompression.ToString(),
                wrapU = importer.wrapModeU.ToString(), wrapV = importer.wrapModeV.ToString(),
                previewScene = reviewRoot + "/Preview.unity",
                previewImage = reviewRoot + "/Skybox_Views.png",
                imageViewOrder = new[] {"forward", "right", "back", "left", "zenith", "nadir"},
                graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
                limitations = new[] { "Physical XR headset and player build not tested." }
            };
            File.WriteAllText(reviewRoot + "/Validation.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            RenderTexture.active = activeTarget;
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            if (atlas != null) Object.DestroyImmediate(atlas);
            if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
            if (preview.IsValid() && preview.isLoaded) EditorSceneManager.CloseScene(preview, true);
            Selection.objects = originalSelection;
        }
    }
}
