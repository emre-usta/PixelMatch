#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CRTSpriteCreator
{
    [MenuItem("Tools/Create CRT Sprite")]
    static void CreateCRTSprite()
    {
        Texture2D tex = new Texture2D(1, 3, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        tex.SetPixel(0, 1, new Color(0, 0, 0, 0f));
        tex.SetPixel(0, 2, new Color(0, 0, 0, 0f));
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = "Assets/Sprites/spr_crt_line.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 1;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        AssetDatabase.ImportAsset(path);

        Debug.Log("CRT sprite oluþturuldu: " + path);
    }
}
#endif