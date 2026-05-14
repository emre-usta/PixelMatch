using UnityEngine;

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
    public Sprite[] cardSprites;
    public Sprite cardBackSprite;

    [Header("Zorluk")]
    public DifficultyLevel difficulty = DifficultyLevel.Easy;

    [Header("Özel Kart Görselleri")]
    public Sprite timeThiefSprite;

    [Header("Level Önizleme")]
    public Sprite previewSprite;

    public int TotalCards => columns * rows;
    public int TotalPairs => TotalCards / 2;
    public bool IsValid => cardSprites != null && cardSprites.Length >= TotalPairs;
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}