using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DailyChallengeManager : MonoBehaviour
{
    public static DailyChallengeManager Instance { get; private set; }

    // ─── STATIC STATE ─────────────────────────────────────────────
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

    // ─── PLAYERPREFS ANAHTARLARI ──────────────────────────────────
    private const string KEY_LAST_PLAYED = "dc_last_played";
    private const string KEY_LAST_STARS = "dc_last_stars";

    // ─── INSPECTOR ────────────────────────────────────────────────
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

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

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
        // Timer'ı her saniye güncelle
        UpdateTimerText();
    }

    // ─── GÜNLÜK CONFIG ÜRETİMİ ────────────────────────────────────

    private void GenerateDailyConfig()
    {
        DateTime today = DateTime.Now;
        int seed = today.Year * 10000 + today.Month * 100 + today.Day;

        // Seed ile rastgele üretim
        UnityEngine.Random.InitState(seed);

        // Kategori — seed ile rastgele
        if (categories != null && categories.Length > 0)
            DCCategory = categories[UnityEngine.Random.Range(0, categories.Length)];

        // Grid — seed ile rastgele
        int[][] grids = { new[] { 4, 4 }, new[] { 4, 5 }, new[] { 5, 4 }, new[] { 6, 6 } };
        int[] grid = grids[UnityEngine.Random.Range(0, grids.Length)];
        DCColumns = grid[0];
        DCRows = grid[1];

        // Zorluk — haftanın gününe göre
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

        // Random state'i sıfırla — diğer sistemleri etkilemesin
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        Debug.Log($"[DC] Seed:{seed} Grid:{DCColumns}x{DCRows} Zorluk:{DCDifficulty} Kategori:{DCCategory?.categoryName}");
    }

    // ─── GÜNLÜK OYNANDI MI? ───────────────────────────────────────

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

    // ─── OYUN BAŞLATMA ────────────────────────────────────────────

    public void StartDailyChallenge()
    {
        if (IsPlayedToday())
        {
            Debug.Log("[DC] Bugün zaten oynadın!");
            return;
        }

        IsDailyChallengeActive = true;
        LevelSelectManager.SelectedMode = LevelSelectManager.GameMode.Classic;
        SceneManager.LoadScene("Level1");
        Debug.Log($"[DC] Daily Challenge başlatıldı!");
    }

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    private void UpdateUI()
    {
        if (dcCard == null) return;

        bool playedToday = IsPlayedToday();

        // Zorluk metni
        if (txtDifficulty != null)
        {
            string diffText = DCDifficulty switch
            {
                DifficultyLevel.Easy => "KOLAY",
                DifficultyLevel.Medium => "ORTA",
                DifficultyLevel.Hard => "ZOR",
                _ => ""
            };
            txtDifficulty.text = $"GÜNLÜK GÖREV  ·  {diffText}  ·  {DCColumns}×{DCRows}";
        }

        // Durum metni
        if (txtStatus != null)
        {
            if (playedToday)
            {
                int stars = GetTodayStars();
                txtStatus.text = $"Tamamlandı  {stars} yıldız";
                txtStatus.color = new Color(0.59f, 0.77f, 0.35f);
            }
            else
            {
                txtStatus.text = "Oynamak için tıkla!";
                txtStatus.color = new Color(0.98f, 0.78f, 0.46f);
            }
        }

        // Buton
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

    // Test için — build'e almadan önce sil
    [ContextMenu("DC'yi Sıfırla")]
    public void ResetDailyChallenge()
    {
        PlayerPrefs.DeleteKey("dc_last_played");
        PlayerPrefs.DeleteKey("dc_last_stars");
        PlayerPrefs.Save();
        Debug.Log("[DC] Sıfırlandı!");
    }
}