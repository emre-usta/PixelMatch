#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CornerSpriteCreator
{
    [MenuItem("Tools/Create Corner Sprite")]
    static void CreateCornerSprite()
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Tüm pikselleri þeffaf yap
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.clear);

        // Alt-sol üçgen doldur (köþe için)
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (x + y < size)
                    tex.SetPixel(x, y, Color.white);

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = "Assets/Sprites/spr_corner.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Point;
        AssetDatabase.ImportAsset(path);

        Debug.Log("Corner sprite olusturuldu!");
    }
}
#endif