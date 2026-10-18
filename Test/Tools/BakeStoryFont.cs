using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class BakeStoryFont
{
    public static object Run()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before baking the font.");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Test/Fonts/PrototypeKorean.asset");
        string corpus=string.Concat(Directory.GetFiles("Assets/Test/Scripts","*.cs").Select(File.ReadAllText));
        string characters=new string(corpus.Where(c=>!char.IsControl(c)).Distinct().ToArray());
        int before=font.characterTable.Count;
        font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        var serialized=new SerializedObject(font);
        serialized.FindProperty("m_SourceFontFile").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Font>("Assets/Test/Fonts/NotoSansKR.ttf");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        bool added=font.TryAddCharacters(characters,out string missing);
        font.atlasPopulationMode=AtlasPopulationMode.Static;
        font.material.SetFloat("_FaceDilate",.06f);
        EditorUtility.SetDirty(font); EditorUtility.SetDirty(font.material);
        foreach(var atlas in font.atlasTextures) if(atlas!=null) EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        if(!added) throw new System.InvalidOperationException("Missing glyphs: "+missing);
        return new { before,after=font.characterTable.Count,atlases=font.atlasTextures.Length,missing,mode=font.atlasPopulationMode.ToString() };
    }
}
