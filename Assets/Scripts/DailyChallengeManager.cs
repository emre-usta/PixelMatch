using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class DailyChallengeManager : MonoBehaviour
{
    public static DailyChallengeManager Instance { get; private set; }

    public static bool IsDailyChallengeActive { get; private set; }
    public static int DCColumns { get; private set; }
    public static int DCRows { get; private set; }
    public static DifficultyLevel DCDifficulty { get; private set; }
    public static CategoryConfig DCCategory { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticState()
    {
        IsDailyChallengeActive = false;
    }

    private Coroutine pulseCoroutine;

    private const string KEY_LAST_PLAYED = "dc_last_played";
    private const string KEY_LAST_STARS = "dc_last_stars";
    private const string KEY_ATTEMPTS = "dc_attempts";

    [Header("Kategoriler")]
    [SerializeField] private CategoryConfig[] categories;

    [Header("Limit Config")]
    [SerializeField] private FreeModeLimitConfig limitConfig;

    [Header("Ana Menü UI")]
    [SerializeField] private GameObject dcCard;
    [SerializeField] private TextMeshProUGUI txtTimer;
    [SerializeField] private TextMeshProUGUI txtDifficulty;
    [SerializeField] private TextMeshProUGUI txtStatus;
    [SerializeField] private UnityEngine.UI.Button btnPlay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GenerateDailyConfig();
        UpdateUI();
    }

    private void Update()
    {
        UpdateTimerText();
    }

    // ─── GÜNLÜK CONFIG ────────────────────────────────────────────

    private void GenerateDailyConfig()
    {
        DateTime today = DateTime.Now;
        int seed = today.Year * 10000 + today.Month * 100 + today.Day;

        UnityEngine.Random.InitState(seed);

        if (categories != null && categories.Length > 0)
            DCCategory = categories[UnityEngine.Random.Range(0, categories.Length)];

        int[][] grids = { new[] { 4, 4 }, new[] { 4, 5 }, new[] { 5, 4 }, new[] { 6, 6 } };
        int[] grid = grids[UnityEngine.Random.Range(0, grids.Length)];
        DCColumns = grid[0];
        DCRows = grid[1];

        DCDifficulty = today.DayOfWeek switch
        {
            DayOfWeek.Monday => DifficultyLevel.Easy,
            DayOfWeek.Tuesday => DifficultyLevel.Easy,
            DayOfWeek.Wednesday => DifficultyLevel.Easy,
            DayOfWeek.Thursday => DifficultyLevel.Medium,
            DayOfWeek.Friday => DifficultyLevel.Medium,
            DayOfWeek.Saturday => DifficultyLevel.Medium,
            DayOfWeek.Sunday => DifficultyLevel.Hard,
            _ => DifficultyLevel.Easy
        };

        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
    }

    // ─── DURUM KONTROL ────────────────────────────────────────────

    public bool IsPlayedToday()
    {
        string lastPlayed = PlayerPrefs.GetString(KEY_LAST_PLAYED, "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return lastPlayed == today;
    }

    public int GetTodayStars()
    {
        if (!IsPlayedToday()) return -1;
        return PlayerPrefs.GetInt(KEY_LAST_STARS, 0);
    }

    public void MarkPlayedToday(int stars)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(KEY_LAST_PLAYED, today);
        PlayerPrefs.SetInt(KEY_LAST_STARS, stars);
        PlayerPrefs.Save();
    }

    public bool CanRetry() => !IsPlayedToday();

    public int GetTodayAttempts()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return PlayerPrefs.GetInt(KEY_ATTEMPTS + "_" + today, 0);
    }

    public void AddAttempt()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        int current = GetTodayAttempts();
        PlayerPrefs.SetInt(KEY_ATTEMPTS + "_" + today, current + 1);
        PlayerPrefs.Save();
    }

    public bool HasAttemptsLeft() => GetTodayAttempts() < 2;

    public static void SetInactive()
    {
        IsDailyChallengeActive = false;

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastPlayed = PlayerPrefs.GetString("dc_last_played", "");

        if (lastPlayed != today)
        {
            PlayerPrefs.SetString("dc_last_played", today);
            PlayerPrefs.SetInt("dc_last_stars", 0);
            PlayerPrefs.Save();
        }
    }

    // ─── OYUN BAŞLATMA ────────────────────────────────────────────

    public void StartDailyChallenge()
    {
        if (IsPlayedToday()) return;

        AdManager.Instance?.ShowRewardedInterstitialIfReady(
            onSuccess: () =>
            {
                PowerUpManager.Instance?.AddPowerUp(PowerUpType.TimeFreeze);
                LaunchDailyChallenge();
            },
            onSkipped: () =>
            {
                LaunchDailyChallenge();
            }
        );
    }

    private void LaunchDailyChallenge()
    {
        IsDailyChallengeActive = true;
        LevelSelectManager.SelectedMode = LevelSelectManager.GameMode.Classic;
        SceneManager.LoadScene("Level1");
    }

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    public void UpdateUI()
    {
        if (dcCard == null) return;

        bool playedToday = IsPlayedToday();

        if (playedToday && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (txtDifficulty != null)
        {
            string diffKey = DCDifficulty switch
            {
                DifficultyLevel.Easy => "difficulty_easy",
                DifficultyLevel.Medium => "difficulty_medium",
                DifficultyLevel.Hard => "difficulty_hard",
                _ => "difficulty_easy"
            };
            txtDifficulty.text = $"{LocalizationManager.Get(diffKey)} · {DCColumns}×{DCRows}";
        }

        if (txtStatus != null)
        {
            if (playedToday)
            {
                int stars = GetTodayStars();
                if (stars > 0)
                {
                    txtStatus.text = LocalizationManager.Get("dc_completed");
                    txtStatus.color = new Color(0.59f, 0.77f, 0.35f);
                }
                else
                {
                    txtStatus.text = LocalizationManager.Get("dc_failed");
                    txtStatus.color = new Color(0.89f, 0.29f, 0.29f);
                }
            }
            else
            {
                txtStatus.text = LocalizationManager.Get("dc_play_hint");
                txtStatus.color = new Color(0.96f, 0.65f, 0.14f);

                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseText(txtStatus));
            }
        }

        if (btnPlay != null)
            btnPlay.interactable = !playedToday;
    }

    private void UpdateTimerText()
    {
        if (txtTimer == null) return;

        DateTime now = DateTime.Now;
        DateTime tomorrow = now.Date.AddDays(1);
        TimeSpan remaining = tomorrow - now;

        txtTimer.text = $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private IEnumerator PulseText(TextMeshProUGUI txt)
    {
        Color baseColor = new Color(0.96f, 0.65f, 0.14f);
        Color dimColor = new Color(0.96f, 0.65f, 0.14f, 0.3f);
        float duration = 1f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                txt.color = Color.Lerp(baseColor, dimColor, elapsed / (duration / 2f));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                txt.color = Color.Lerp(dimColor, baseColor, elapsed / (duration / 2f));
                yield return null;
            }
        }
    }

    [ContextMenu("DC'yi Sıfırla")]
    public void ResetDailyChallenge()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.DeleteKey(KEY_LAST_PLAYED);
        PlayerPrefs.DeleteKey(KEY_LAST_STARS);
        PlayerPrefs.DeleteKey(KEY_ATTEMPTS + "_" + today);
        PlayerPrefs.Save();
    }
}