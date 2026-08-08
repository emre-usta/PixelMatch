using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using Google.Play.Review;
#endif

public class InAppReviewManager : MonoBehaviour
{
    public static InAppReviewManager Instance { get; private set; }
#if UNITY_ANDROID
    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;
    private bool _isReviewFlowReady = false;
#endif
    private const string KEY_LEVELS_COMPLETED = "review_levels_completed";
    private const string KEY_REVIEW_SHOWN = "review_shown";
    private const int LEVELS_BEFORE_REVIEW = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID
        StartCoroutine(InitReviewFlow());
#endif
    }

#if UNITY_ANDROID
    // ─── BAŞLATMA ─────────────────────────────────────────────────
    private IEnumerator InitReviewFlow()
    {
        _reviewManager = new ReviewManager();
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppReview] Review flow request failed: {requestFlowOperation.Error}");
            yield break;
        }
        _playReviewInfo = requestFlowOperation.GetResult();
        _isReviewFlowReady = true;
        Debug.Log("[InAppReview] Review flow ready.");
    }
#endif

    // ─── LEVEL TAMAMLANDI ─────────────────────────────────────────
    public void OnLevelCompleted()
    {
#if UNITY_ANDROID
        if (FreeModeManager.IsFreeModeActive) return;
        if (DailyChallengeManager.IsDailyChallengeActive) return;
        if (PlayerPrefs.GetInt(KEY_REVIEW_SHOWN, 0) == 1) return;

        int levelsCompleted = PlayerPrefs.GetInt(KEY_LEVELS_COMPLETED, 0) + 1;
        PlayerPrefs.SetInt(KEY_LEVELS_COMPLETED, levelsCompleted);
        PlayerPrefs.Save();
        Debug.Log($"[InAppReview] Levels completed: {levelsCompleted}");

        if (levelsCompleted >= LEVELS_BEFORE_REVIEW)
        {
            StartCoroutine(LaunchReviewFlow());
        }
#endif
        // iOS: TODO - SKStoreReviewController eşdeğeri eklenecek (not alındı)
    }

#if UNITY_ANDROID
    // ─── POPUP GÖSTER ─────────────────────────────────────────────
    private IEnumerator LaunchReviewFlow()
    {
        if (!_isReviewFlowReady)
        {
            yield return StartCoroutine(InitReviewFlow());
        }
        if (!_isReviewFlowReady || _playReviewInfo == null)
        {
            Debug.LogWarning("[InAppReview] Review flow not ready, skipping.");
            yield break;
        }
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;
        _playReviewInfo = null;
        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppReview] Launch failed: {launchFlowOperation.Error}");
            yield break;
        }
        PlayerPrefs.SetInt(KEY_REVIEW_SHOWN, 1);
        PlayerPrefs.Save();
        Debug.Log("[InAppReview] Review flow completed.");
    }
#endif

    // ─── DEBUG ────────────────────────────────────────────────────
    [ContextMenu("Reset Review State")]
    public void ResetReviewState()
    {
        PlayerPrefs.DeleteKey(KEY_LEVELS_COMPLETED);
        PlayerPrefs.DeleteKey(KEY_REVIEW_SHOWN);
        PlayerPrefs.Save();
        Debug.Log("[InAppReview] Review state reset.");
    }
}