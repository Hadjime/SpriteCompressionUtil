using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

public class SpriteCompressionUtil : EditorWindow
{
    #region Nested types

    private class PlatformNames
    {
        public readonly string nameForSprite;
        public readonly string nameForAtlas;

        public PlatformNames(string _nameForSprite, string _nameForAtlas)
        {
            nameForSprite = _nameForSprite;
            nameForAtlas = _nameForAtlas;
        }
    }

    #endregion



    #region Fields

    private static readonly Dictionary<BuildTarget, PlatformNames> platformBuildTargetForSprites
        = new Dictionary<BuildTarget, PlatformNames>()
        {
            { BuildTarget.iOS, new PlatformNames(BuildTarget.iOS.ToString(), "iPhone") },
            { BuildTarget.Android, new PlatformNames(BuildTarget.Android.ToString(), BuildTarget.Android.ToString()) },
        };

    private TextureImporterFormat toFormat;
    private TextureImporterFormat[] fromFormats;
    private BuildTarget buildTarget;

    private bool isFoldoutOpen;

    #endregion



    #region Methods

    [MenuItem("Tools/Compression Tool")]
    private static void Setup()
    {
        InitializeWindow();
    }


    private static void InitializeWindow()
    {
        SpriteCompressionUtil window = CreateWindow<SpriteCompressionUtil>();

        window.titleContent.text = nameof(SpriteCompressionUtil);

        int width = Screen.currentResolution.width / 6;
        int height = Screen.currentResolution.height / 4;

        int x = Screen.currentResolution.width / 2 - width / 2;
        int y = Screen.currentResolution.height / 2 - height / 2;

        window.position = new Rect(x, y, width, height);

        window.Show();
    }

    private void OnEnable()
    {
        buildTarget = EditorUserBuildSettings.activeBuildTarget;

        fromFormats = Array.Empty<TextureImporterFormat>();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUILayout.Space(5);

            buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", buildTarget);

            GUILayout.Space(5);

            fromFormats = EnumArrayField("From Formats", ref isFoldoutOpen, fromFormats);
            GUILayout.Space(5);
            toFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Target Format", toFormat);

            GUILayout.Space(5);

            if(GUILayout.Button("Start"))
            {
                ChangeCompression();
            }
        }
        GUILayout.EndVertical();
    }


    private T[] EnumArrayField<T>(string label, ref bool open, T[] array) where T : Enum
    {
        open = EditorGUILayout.Foldout(open, label);

        int newSize = array.Length;

        if (open)
        {
            newSize = EditorGUILayout.IntField("Size", newSize);
            newSize = newSize < 0 ? 0 : newSize;

            if (newSize != array.Length)
            {
                Array.Resize(ref array, newSize);
            }

            for (var i = 0; i < newSize; i++)
            {
                array[i] = (T)EditorGUILayout.EnumPopup($"Value {i}", array[i]);
            }
        }

        return array;
    }


    private void ChangeCompression()
    {
        string path = Application.dataPath;

        List<Sprite> sprites = GetAssets<Sprite>(path);
        List<SpriteAtlas> spriteAtlases = GetAssets<SpriteAtlas>(path);
        List<Texture2D> textures = GetAssets<Texture2D>(path);

        Debug.Log($"Found {sprites.Count} sprites");
        Debug.Log($"Found {spriteAtlases.Count} sprite atlases");
        Debug.Log($"Found {textures.Count} textures");

        foreach (var tex in textures)
        {
            SetTextureComperession(tex);
        }

        Debug.Log("Textures compression was completed!");

        //foreach (var sprite in sprites)
        //{
        //    SetSpriteComperession(sprite);
        //}

        //Debug.Log("Sprites compression was completed!");

        //foreach (var spriteAtlas in spriteAtlases)
        //{
        //    SetSpriteAtlasComperession(spriteAtlas);
        //}

        //Debug.Log("Sprites atlases compression was completed!");
    }


    private List<T> GetAssets<T>(string path)
    {
        List<T> result = new List<T>();

        foreach(var file in Directory.GetFiles(path))
        {
            string assetPath = "Assets" + file.Replace(Application.dataPath, string.Empty);
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));

            if (asset is T value)
            {
                result.Add(value);
            }
        }

        foreach(var directory in Directory.GetDirectories(path))
        {
            result.AddRange(GetAssets<T>(directory));
        }

        return result;
    }

    private void SetTextureComperession(Texture2D tex)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));

        TextureImporterPlatformSettings settings = ti.GetPlatformTextureSettings(platformBuildTargetForSprites[buildTarget].nameForSprite);

        bool apply = fromFormats.Length == 0 || Array.Exists(fromFormats, x => x == toFormat);
        if (apply)
        {
            settings.overridden = true;
            settings.format = toFormat;
            ti.SetPlatformTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    private void SetSpriteComperession(Sprite sprite) 
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));

        TextureImporterPlatformSettings settings = ti.GetPlatformTextureSettings(platformBuildTargetForSprites[buildTarget].nameForSprite);

        bool apply = fromFormats.Length == 0 || Array.Exists(fromFormats, x => x == toFormat);
        if (apply)
        {
            settings.overridden = true;
            settings.format = toFormat;
            ti.SetPlatformTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }


    private void SetSpriteAtlasComperession(SpriteAtlas spriteAtlas)
    {
        AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteAtlas));

        TextureImporterPlatformSettings settings = spriteAtlas.GetPlatformSettings(platformBuildTargetForSprites[buildTarget].nameForAtlas);

        if (Array.Exists(fromFormats, x => x == toFormat))
        {
            settings.overridden = true;
            settings.format = toFormat;
            spriteAtlas.SetPlatformSettings(settings);
            importer.SaveAndReimport();
        }
    }
  
    #endregion 
}