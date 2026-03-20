using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class FreeModeManager : MonoBehaviour
{
    public static FreeModeManager Instance { get; private set; }

    // ─── SEÇİLEN DEĞERLER (Static) ────────────────────────────────
    public static bool IsFreeModeActive { get; private set; }
    public static int SelectedColumns { get; private set; }
    public static int SelectedRows { get; private set; }
    public static DifficultyLevel SelectedDifficulty { get; private set; }
    public static LevelSelectManager.GameMode SelectedMode { get; private set; }
    public static CategoryConfig SelectedCategory { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticState()
    {
        IsFreeModeActive = false;
        SelectedColumns = 4;
        SelectedRows = 4;
        SelectedDifficulty = DifficultyLevel.Easy;
        SelectedMode = LevelSelectManager.GameMode.Classic;
        SelectedCategory = null;
    }

    // ─── INSPECTOR ────────────────────────────────────────────────
    [Header("Konfigürasyon")]
    [SerializeField] private FreeModeLimitConfig limitConfig;
    [SerializeField] private CategoryConfig[] categories;

    [Header("Kategori Butonları")]
    [SerializeField] private Button[] categoryButtons;

    [Header("Grid Butonları")]
    [SerializeField] private Button btn4x4;
    [SerializeField] private Button btn4x5;
    [SerializeField] private Button btn5x4;
    [SerializeField] private Button btn6x6;

    [Header("Zorluk Butonları")]
    [SerializeField] private Button btnEasy;
    [SerializeField] private Button btnMedium;
    [SerializeField] private Button btnHard;

    [Header("Mod Butonları")]
    [SerializeField] private Button btnClassic;
    [SerializeField] private Button btnMove;

    [Header("Başlat")]
    [SerializeField] private Button btnStart;
    [SerializeField] private TextMeshProUGUI txtLimitInfo;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SelectedColumns = 4;
        SelectedRows = 4;
        SelectedDifficulty = DifficultyLevel.Easy;
        SelectedMode = LevelSelectManager.GameMode.Classic;
        if (categories != null && categories.Length > 0)
            SelectedCategory = categories[0];

        SetupButtons();
        UpdateLimitInfo();

        // Default vurgulamalar
        Button[] gridBtns = { btn4x4, btn4x5, btn5x4, btn6x6 };
        HighlightButton(gridBtns, 0);

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, 0);

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, 0);

        HighlightButton(categoryButtons, 0);
    }

    private void SetupButtons()
    {
        // Kategori butonları
        for (int i = 0; i < categoryButtons.Length && i < categories.Length; i++)
        {
            int index = i;
            CategoryConfig cat = categories[i];
            TextMeshProUGUI txt = categoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = cat.categoryName;
            categoryButtons[i].onClick.AddListener(() => SelectCategory(cat));
        }

        btn4x4?.onClick.AddListener(() => SelectGrid(4, 4));
        btn4x5?.onClick.AddListener(() => SelectGrid(4, 5));
        btn5x4?.onClick.AddListener(() => SelectGrid(5, 4));
        btn6x6?.onClick.AddListener(() => SelectGrid(6, 6));

        btnEasy?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Easy));
        btnMedium?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Medium));
        btnHard?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Hard));

        btnClassic?.onClick.AddListener(() => SelectMode(LevelSelectManager.GameMode.Classic));
        btnMove?.onClick.AddListener(() => SelectMode(LevelSelectManager.GameMode.Move));

        btnStart?.onClick.AddListener(StartFreeMode);
    }

    // ─── SEÇİM METOTLARI ──────────────────────────────────────────

    private void SelectCategory(CategoryConfig category)
    {
        SelectedCategory = category;
        HighlightButton(categoryButtons, System.Array.IndexOf(categories, category));
        Debug.Log($"[FreeModeManager] Kategori: {category.categoryName}");
    }

    private void SelectGrid(int cols, int rows)
    {
        SelectedColumns = cols;
        SelectedRows = rows;
        UpdateLimitInfo();

        Button[] gridBtns = { btn4x4, btn4x5, btn5x4, btn6x6 };
        int index = (cols == 4 && rows == 4) ? 0 :
                    (cols == 4 && rows == 5) ? 1 :
                    (cols == 5 && rows == 4) ? 2 : 3;
        HighlightButton(gridBtns, index);
        Debug.Log($"[FreeModeManager] Grid: {cols}x{rows}");
    }

    private void SelectDifficulty(DifficultyLevel difficulty)
    {
        SelectedDifficulty = difficulty;
        UpdateLimitInfo();

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, (int)difficulty);
        Debug.Log($"[FreeModeManager] Zorluk: {difficulty}");
    }

    private void SelectMode(LevelSelectManager.GameMode mode)
    {
        SelectedMode = mode;
        LevelSelectManager.SelectedMode = mode;

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, mode == LevelSelectManager.GameMode.Classic ? 0 : 1);
        Debug.Log($"[FreeModeManager] Mod: {mode}");
    }

    private void HighlightButton(Button[] buttons, int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            Image img = buttons[i].GetComponent<Image>();
            if (img == null) continue;

            img.color = (i == selectedIndex)
                ? new Color(0.55f, 0.41f, 0.08f)   // #8B6914 — aktif
                : new Color(0.10f, 0.08f, 0.03f);  // #1A1408 — pasif
        }
    }

    private void UpdateLimitInfo()
    {
        if (limitConfig == null || txtLimitInfo == null) return;
        var limit = limitConfig.GetLimit(SelectedColumns, SelectedRows, SelectedDifficulty);

        int minutes = Mathf.FloorToInt(limit.timeLimit / 60);
        int seconds = Mathf.FloorToInt(limit.timeLimit % 60);
        txtLimitInfo.text = $"Süre: {minutes:00}:{seconds:00}  ·  Hamle: {limit.moveLimit}";
    }

    // ─── OYUN BAŞLATMA ────────────────────────────────────────────

    private void StartFreeMode()
    {
        if (SelectedCategory == null)
        {
            Debug.LogWarning("[FreeModeManager] Kategori seçilmedi!");
            return;
        }

        IsFreeModeActive = true;
        LevelSelectManager.SelectedMode = SelectedMode;
        SceneManager.LoadScene("Level1");
        Debug.Log($"[FreeModeManager] Serbest Mod başlatıldı: {SelectedColumns}x{SelectedRows} {SelectedDifficulty} {SelectedMode}");
    }

    public void OnBackClicked()
    {
        IsFreeModeActive = false;
        SceneManager.LoadScene("MainMenu");
    }
}