using System;
using System.Globalization;
using UnityEngine;

// AUDIT D2: Aktif oyun oturumunun tek kaynağı (kaynak, mod, seviye, grid, başlangıç limitleri).
// Eski static alanlar (LevelSelectManager.Selected*, DailyChallengeManager.DC*, FreeModeManager.Selected*)
// analitik için Faz 3'e kadar paralel olarak yazılmaya devam eder.

public enum SessionSource
{
    Story,
    DailyChallenge,
    FreeMode
}

// Değişmez: tüm alanlar yalnızca fabrika metotlarıyla ayarlanır.
public sealed class SessionContext
{
    public SessionSource Source { get; }
    public LevelSelectManager.GameMode Mode { get; }
    public bool IsMoveMode => Mode == LevelSelectManager.GameMode.Move;

    public CategoryConfig Category { get; }
    public int CategoryID { get; }
    public int CategoryLevelCount { get; }      // Yalnızca Story; diğerlerinde 0

    public LevelConfig Level { get; }           // Yalnızca Story; diğerlerinde null
    public int LevelID { get; }                 // Yalnızca Story; diğerlerinde -1
    public int LevelIndex { get; }              // Yalnızca Story; diğerlerinde -1

    public string DcDate { get; }               // Yalnızca DC: "yyyy-MM-dd" (InvariantCulture, yerel saat)

    public DifficultyLevel Difficulty { get; }
    public int Columns { get; }
    public int Rows { get; }

    // Oyunun başındaki limitler (combo / second chance ile değişen çalışma zamanı değerleri controller'larda).
    public bool HasLimits { get; }              // DC/Free'de limit config yoksa false (GridManager o durumda controller'ları yapılandırmaz)
    public float TimeLimit { get; }
    public int MoveLimit { get; }
    public bool UsesTimer { get; }
    public bool UsesMoveLimit { get; }

    public bool TimeThiefEnabled { get; }
    public Sprite TimeThiefSprite { get; }

    // Faz 3 analitiği için string kimlik (henüz hiçbir event'te kullanılmıyor).
    public string LevelKey { get; }

    private SessionContext(
        SessionSource source, LevelSelectManager.GameMode mode,
        CategoryConfig category, int categoryLevelCount,
        LevelConfig level, int levelID, int levelIndex,
        string dcDate, DifficultyLevel difficulty, int columns, int rows,
        bool hasLimits, float timeLimit, int moveLimit, bool usesTimer, bool usesMoveLimit,
        bool timeThiefEnabled, Sprite timeThiefSprite)
    {
        Source = source;
        Mode = mode;
        Category = category;
        CategoryID = category != null ? category.categoryID : 0;
        CategoryLevelCount = categoryLevelCount;
        Level = level;
        LevelID = levelID;
        LevelIndex = levelIndex;
        DcDate = dcDate;
        Difficulty = difficulty;
        Columns = columns;
        Rows = rows;
        HasLimits = hasLimits;
        TimeLimit = timeLimit;
        MoveLimit = moveLimit;
        UsesTimer = usesTimer;
        UsesMoveLimit = usesMoveLimit;
        TimeThiefEnabled = timeThiefEnabled;
        TimeThiefSprite = timeThiefSprite;
        LevelKey = BuildLevelKey();
    }

    private string BuildLevelKey()
    {
        switch (Source)
        {
            case SessionSource.Story:
                return $"s_c{CategoryID}_l{LevelID}";
            case SessionSource.DailyChallenge:
                return $"dc_{DcDate}";
            default:
                return $"free_c{CategoryID}_{Columns}x{Rows}_{Difficulty.ToString().ToLowerInvariant()}";
        }
    }

    // DC anahtarlarıyla (dc_attempts_{tarih}, dc_last_played) aynı biçim: yerel saat, "yyyy-MM-dd".
    public static string TodayDcDate() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ─── FABRİKALAR ───────────────────────────────────────────────

    // GridManager'ın story yolu ile aynı: grid ve limitler LevelConfig'den; Zaman Hırsızı yalnızca Hard'da.
    public static SessionContext ForStory(CategoryConfig category, LevelConfig level, int levelIndex,
                                          LevelSelectManager.GameMode mode)
    {
        if (level == null) return null;

        bool isMove = mode == LevelSelectManager.GameMode.Move;
        return new SessionContext(
            SessionSource.Story, mode,
            category, category != null && category.levels != null ? category.levels.Length : 0,
            level, level.levelID, levelIndex,
            null, level.difficulty, level.columns, level.rows,
            true, level.timeLimit, level.moveLimit, !isMove, isMove,
            level.difficulty == DifficultyLevel.Hard, level.timeThiefSprite);
    }

    // GridManager'ın DC yolu ile aynı: her zaman süreli (Classic), limitler FreeModeLimitConfig'den.
    public static SessionContext ForDailyChallenge(string dcDate, CategoryConfig category, int columns, int rows,
                                                   DifficultyLevel difficulty, FreeModeLimitConfig limits)
    {
        bool hasLimits = limits != null;
        var limit = hasLimits ? limits.GetLimit(columns, rows, difficulty) : null;

        return new SessionContext(
            SessionSource.DailyChallenge, LevelSelectManager.GameMode.Classic,
            category, 0,
            null, -1, -1,
            dcDate, difficulty, columns, rows,
            hasLimits, hasLimits ? limit.timeLimit : 0f, hasLimits ? limit.moveLimit : 0, true, false,
            false, null);
    }

    // GridManager'ın Free yolu ile aynı: mod seçime göre, limitler FreeModeLimitConfig'den.
    public static SessionContext ForFreeMode(CategoryConfig category, int columns, int rows,
                                             DifficultyLevel difficulty, LevelSelectManager.GameMode mode,
                                             FreeModeLimitConfig limits)
    {
        bool isMove = mode == LevelSelectManager.GameMode.Move;
        bool hasLimits = limits != null;
        var limit = hasLimits ? limits.GetLimit(columns, rows, difficulty) : null;

        return new SessionContext(
            SessionSource.FreeMode, mode,
            category, 0,
            null, -1, -1,
            null, difficulty, columns, rows,
            hasLimits, hasLimits ? limit.timeLimit : 0f, hasLimits ? limit.moveLimit : 0, !isMove, isMove,
            false, null);
    }

    // Bugünkü static'lerden, GridManager.Start ile aynı öncelikle (DC → Free → Story) context üretir.
    // Static'ler de boşsa null döner (çağıran inspector varsayılanlarına düşer).
    public static SessionContext FromLegacyStatics(FreeModeLimitConfig limits = null)
    {
        if (DailyChallengeManager.IsDailyChallengeActive)
            return ForDailyChallenge(TodayDcDate(), DailyChallengeManager.DCCategory,
                DailyChallengeManager.DCColumns, DailyChallengeManager.DCRows,
                DailyChallengeManager.DCDifficulty, limits);

        if (FreeModeManager.IsFreeModeActive)
            return ForFreeMode(FreeModeManager.SelectedCategory,
                FreeModeManager.SelectedColumns, FreeModeManager.SelectedRows,
                FreeModeManager.SelectedDifficulty, FreeModeManager.SelectedMode, limits);

        if (LevelSelectManager.SelectedLevel != null)
        {
            LevelConfig level = LevelSelectManager.SelectedLevel;
            var ctx = ForStory(LevelSelectManager.SelectedCategory, level, level.levelID, LevelSelectManager.SelectedMode);
            return ctx;
        }

        return null;
    }
}

// Aktif oturumun tutulduğu yer. Veri olduğu için MonoBehaviour değil; sahne geçişlerinde yaşar,
// uygulama açılışında sıfırlanır, kalıcı olarak saklanmaz.
public static class GameSession
{
    public static SessionContext Current { get; private set; }

    private static bool legacyWarningLogged = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticState()
    {
        Current = null;
        legacyWarningLogged = false;
    }

    public static void Begin(SessionContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("[GameSession] Begin(null) yok sayıldı");
            return;
        }

        Current = context;
        legacyWarningLogged = false;
    }

    public static void End()
    {
        Current = null;
        legacyWarningLogged = false;
    }

    // Okuyucular için tek giriş noktası. Current yoksa eski static'lerden üretir; o da yoksa null.
    public static SessionContext CurrentOrLegacy(FreeModeLimitConfig limits = null)
    {
        if (Current != null) return Current;

        if (!legacyWarningLogged)
        {
            Debug.LogWarning("[GameSession] Current null, legacy statics fallback");
            legacyWarningLogged = true;
        }

        return SessionContext.FromLegacyStatics(limits);
    }
}
