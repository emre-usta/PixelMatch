using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                isInitialized = true;
            }
            else
            {
                Debug.LogError($"Firebase dependency error: {task.Result}");
            }
        });
    }

    // ─── YARDIMCI ─────────────────────────────────────────────────

    private string GetGridSize(int columns, int rows) => $"{columns}x{rows}";

    private string GetGameModeString(LevelSelectManager.GameMode mode)
    {
        return mode switch
        {
            LevelSelectManager.GameMode.Classic => "classic",
            LevelSelectManager.GameMode.Move => "move",
            _ => "classic"
        };
    }

    // ─── LEVEL ────────────────────────────────────────────────────

    public void LogLevelStart(int categoryID, int levelID)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("level_start",
            new Parameter("category_id", categoryID),
            new Parameter("level_id", levelID));
    }

    // Eski metodlar korunuyor — mevcut çağrı noktaları bozulmasın
    public void LogLevelComplete(int categoryID, int levelID, int stars)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("level_complete",
            new Parameter("category_id", categoryID),
            new Parameter("level_id", levelID),
            new Parameter("stars", stars));
    }

    public void LogLevelFail(int categoryID, int levelID)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("level_fail",
            new Parameter("category_id", categoryID),
            new Parameter("level_id", levelID));
    }

    /// <summary>
    /// Hem win hem lose durumunda çağrılır.
    /// GameStateManager'dan tüm parametreler dolu olarak gönderilmeli.
    /// </summary>
    public void LogLevelEnd(
        int categoryID,
        string categoryName,
        int levelID,
        bool success,
        string gameMode,
        int columns,
        int rows,
        int stars,
        float timeLeft,
        int movesLeft,
        int timeThiefHits)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("level_end",
            new Parameter("category_id", categoryID),
            new Parameter("category_name", categoryName),
            new Parameter("level_id", levelID),
            new Parameter("success", success ? "true" : "false"),
            new Parameter("game_mode", gameMode),
            new Parameter("grid_size", GetGridSize(columns, rows)),
            new Parameter("stars", stars),
            new Parameter("time_left", timeLeft),
            new Parameter("moves_left", movesLeft),
            new Parameter("time_thief_hits", timeThiefHits));
    }

    // ─── DAILY CHALLENGE ──────────────────────────────────────────

    public void LogDailyChallengeStart(string difficulty, int columns, int rows, string categoryName)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("daily_challenge_start",
            new Parameter("difficulty", difficulty),
            new Parameter("grid_size", GetGridSize(columns, rows)),
            new Parameter("category", categoryName));
    }

    public void LogDailyChallengeComplete(int stars)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("daily_challenge_complete",
            new Parameter("stars", stars));
    }

    // ─── POWER-UP ─────────────────────────────────────────────────

    /// <summary>
    /// trigger_context: "standard_play", "low_time", "low_moves"
    /// </summary>
    public void LogPowerUpUsed(string powerUpType, int levelID, int categoryID, string triggerContext)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("powerup_used",
            new Parameter("powerup_type", powerUpType),
            new Parameter("level_id", levelID),
            new Parameter("category_id", categoryID),
            new Parameter("trigger_context", triggerContext));
    }

    // ─── REKLAM ───────────────────────────────────────────────────

    /// <summary>
    /// ad_type: "rewarded", "interstitial", "rewarded_interstitial"
    /// ad_placement: "second_chance", "powerup", "dc_start", "double_reward", "level_unlock"
    /// </summary>
    public void LogAdWatched(string adType, string adPlacement, int levelID)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("ad_watched",
            new Parameter("ad_type", adType),
            new Parameter("ad_placement", adPlacement),
            new Parameter("level_id", levelID));
    }

    // ─── TUTORIAL ─────────────────────────────────────────────────

    public void LogTutorialBegin()
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialBegin);
    }

    /// <summary>
    /// step_name örnekleri:
    /// "flip_first_card", "make_first_match", "use_xray_powerup",
    /// "combo_explained", "daily_challenge_introduced" vb.
    /// </summary>
    public void LogTutorialStep(int stepNumber, string stepName)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("tutorial_step",
            new Parameter("step_number", stepNumber),
            new Parameter("step_name", stepName));
    }

    public void LogTutorialComplete()
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialComplete);
    }
}