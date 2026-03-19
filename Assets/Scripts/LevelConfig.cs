using UnityEngine;

/// <summary>
/// PixelMatch — Level Konfigürasyonu
/// Her level için ayarları tutan ScriptableObject.
/// Assets/Data/Levels/ klasöründe saklanır.
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "PixelMatch/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Bilgisi")]
    public int levelID;
    public string levelName;
    public Sprite levelIcon;

    [Header("Grid Ayarları")]
    public int columns = 4;
    public int rows = 4;

    [Header("Mod Ayarları")]
    public bool useTimer = true;
    public float timeLimit = 120f;
    public bool useMoveLimit = false;
    public int moveLimit = 30;

    [Header("Kart Ayarları")]
    public Sprite[] cardSprites;    // Bu level için kullanılacak sprite'lar
    public Sprite cardBackSprite;   // Kart arka yüzü

    [Header("Zorluk")]
    public DifficultyLevel difficulty = DifficultyLevel.Easy;

    [Header("Yıldız Kriterleri — Klasik Mod")]
    public float threeStarTime = 90f;   // Bu süreden fazla kalırsa 3 yıldız
    public float twoStarTime = 60f;     // 2 yıldız eşiği

    [Header("Yıldız Kriterleri — Hamle Modu")]
    public int threeStarMoves = 12;     // Bu kadar veya az hamlede 3 yıldız
    public int twoStarMoves = 18;       // 2 yıldız eşiği

    [Header("Özel Kart Görselleri")]
    public Sprite timeThiefSprite;

    // LevelConfig.cs'e ekle
    //[HideInInspector] public bool runtimeUseTimer;
    //[HideInInspector] public bool runtimeUseMoveLimit;

    // ─── HESAPLANAN DEĞERLER ──────────────────────────────────────

    /// <summary>Toplam kart sayısı</summary>
    public int TotalCards => columns * rows;

    /// <summary>Toplam çift sayısı</summary>
    public int TotalPairs => TotalCards / 2;

    /// <summary>Gerekli minimum sprite sayısı</summary>
    public bool IsValid => cardSprites != null && cardSprites.Length >= TotalPairs;
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}