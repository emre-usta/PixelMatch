using UnityEngine;

/// <summary>
/// PixelMatch — Kategori Konfigürasyonu
/// Bir kategoriyi ve içindeki levelleri tanýmlar.
/// </summary>
[CreateAssetMenu(fileName = "CategoryConfig", menuName = "PixelMatch/Category Config")]
public class CategoryConfig : ScriptableObject
{
    [Header("Kategori Bilgisi")]
    public int categoryID;
    public string categoryName;
    public string categoryDescription;
    public Sprite categoryIcon;
    public Color categoryColor = Color.white;

    [Header("Levellar")]
    public LevelConfig[] levels;

    /// <summary>Kategori tamamlandý mý?</summary>
    public bool IsCompleted(int unlockedLevelCount) => unlockedLevelCount >= levels.Length;
}