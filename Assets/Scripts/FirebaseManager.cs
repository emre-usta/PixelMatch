using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using System.Threading.Tasks;
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
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                isInitialized = true;
                Debug.Log("Firebase initialized successfully!");
            }
            else
            {
                Debug.LogError($"Firebase dependency error: {dependencyStatus}");
            }
        });
    }

    // ─── ANALİTİK EVENTLER ────────────────────────────────────────

    public void LogLevelStart(int categoryID, int levelID)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("level_start",
            new Parameter("category_id", categoryID),
            new Parameter("level_id", levelID));
    }

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

    public void LogAdWatched(string adType)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("ad_watched",
            new Parameter("ad_type", adType));
    }

    public void LogPowerUpUsed(string powerUpType)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("powerup_used",
            new Parameter("powerup_type", powerUpType));
    }

    public void LogTutorialComplete()
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialComplete);
    }

    public void LogDailyChallengeComplete(int stars)
    {
        if (!isInitialized) return;
        FirebaseAnalytics.LogEvent("daily_challenge_complete",
            new Parameter("stars", stars));
    }
}