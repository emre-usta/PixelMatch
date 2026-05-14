using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    private const string KEY_UNLOCKED = "unlocked_level_";
    private const string KEY_UNLOCKED_CATEGORY = "unlocked_category_";
    private const string KEY_BEST_TIME = "best_time_";
    private const string KEY_BEST_MOVES = "best_moves_";
    private const string KEY_BEST_STARS = "best_stars_";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── LEVEL KİLİT ──────────────────────────────────────────────

    public bool IsLevelUnlocked(int categoryID, int levelID)
    {
        if (levelID == 0) return true;
        return PlayerPrefs.GetInt(KEY_UNLOCKED + categoryID + "_" + levelID, 0) == 1;
    }

    public void UnlockLevel(int categoryID, int levelID)
    {
        PlayerPrefs.SetInt(KEY_UNLOCKED + categoryID + "_" + levelID, 1);
        PlayerPrefs.Save();
    }

    // ─── KATEGORİ KİLİT ───────────────────────────────────────────

    public bool IsCategoryUnlocked(int categoryID)
    {
        if (categoryID == 0) return true;
        return PlayerPrefs.GetInt(KEY_UNLOCKED_CATEGORY + categoryID, 0) == 1;
    }

    public void UnlockCategory(int categoryID)
    {
        PlayerPrefs.SetInt(KEY_UNLOCKED_CATEGORY + categoryID, 1);
        PlayerPrefs.Save();
    }

    // ─── SKOR ─────────────────────────────────────────────────────

    public void SaveBestTime(int categoryID, int levelID, float time)
    {
        string key = KEY_BEST_TIME + categoryID + "_" + levelID;
        float current = PlayerPrefs.GetFloat(key, float.MaxValue);
        if (time < current) { PlayerPrefs.SetFloat(key, time); PlayerPrefs.Save(); }
    }

    public float GetBestTime(int categoryID, int levelID)
        => PlayerPrefs.GetFloat(KEY_BEST_TIME + categoryID + "_" + levelID, 0f);

    public void SaveBestMoves(int categoryID, int levelID, int moves)
    {
        string key = KEY_BEST_MOVES + categoryID + "_" + levelID;
        int current = PlayerPrefs.GetInt(key, int.MaxValue);
        if (moves < current) { PlayerPrefs.SetInt(key, moves); PlayerPrefs.Save(); }
    }

    public int GetBestMoves(int categoryID, int levelID)
        => PlayerPrefs.GetInt(KEY_BEST_MOVES + categoryID + "_" + levelID, 0);

    public void SaveBestStars(int categoryID, int levelID, int stars)
    {
        string key = KEY_BEST_STARS + categoryID + "_" + levelID;
        int current = PlayerPrefs.GetInt(key, 0);
        if (stars > current) { PlayerPrefs.SetInt(key, stars); PlayerPrefs.Save(); }
    }

    public int GetBestStars(int categoryID, int levelID)
        => PlayerPrefs.GetInt(KEY_BEST_STARS + categoryID + "_" + levelID, 0);

    // ─── SIFIRLAMA ────────────────────────────────────────────────

    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}