using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class FreeModeManager : MonoBehaviour
{
    public static FreeModeManager Instance { get; private set; }

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

    [Header("Limit Bilgisi")]
    [SerializeField] private TextMeshProUGUI txtTimeValue;
    [SerializeField] private TextMeshProUGUI txtMoveValue;

    private readonly Color colorActiveBg = new Color(0.10f, 0.07f, 0.00f);
    private readonly Color colorInactiveBg = new Color(0.04f, 0.04f, 0.08f);
    private readonly Color colorActiveText = new Color(0.96f, 0.65f, 0.14f);
    private readonly Color colorInactiveText = new Color(0.32f, 0.26f, 0.20f);
    private readonly Color colorActiveBorder = new Color(0.96f, 0.65f, 0.14f);
    private readonly Color colorInactiveBorder = new Color(0.16f, 0.16f, 0.31f);

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
        UpdateButtonLabels();

        Button[] gridBtns = { btn4x4, btn4x5, btn5x4, btn6x6 };
        HighlightButton(gridBtns, 0, false);

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, 0, false);

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, 0, false);

        HighlightButton(categoryButtons, 0, false);
    }

    private void SetupButtons()
    {
        // Kategori butonları — categoryName kullan (zaten lokalize)
        for (int i = 0; i < categoryButtons.Length && i < categories.Length; i++)
        {
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

    // ─── DİL GÜNCELLEMESİ ────────────────────────────────────────
    public void UpdateButtonLabels()
    {
        // Zorluk butonları
        SetButtonLabel(btnEasy, "difficulty_easy");
        SetButtonLabel(btnMedium, "difficulty_medium");
        SetButtonLabel(btnHard, "difficulty_hard");

        // Mod butonları
        SetButtonLabel(btnClassic, "mode_classic");
        SetButtonLabel(btnMove, "mode_move");

        // Limit bilgisi label'ları varsa güncelle
        UpdateLimitInfo();
    }

    private void SetButtonLabel(Button btn, string key)
    {
        if (btn == null) return;
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = LocalizationManager.Get(key);
    }

    // ─── SEÇİM METOTLARI ──────────────────────────────────────────

    private void SelectCategory(CategoryConfig category)
    {
        SelectedCategory = category;
        HighlightButton(categoryButtons, System.Array.IndexOf(categories, category), false);
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
        HighlightButton(gridBtns, index, false);
        Debug.Log($"[FreeModeManager] Grid: {cols}x{rows}");
    }

    private void SelectDifficulty(DifficultyLevel difficulty)
    {
        SelectedDifficulty = difficulty;
        UpdateLimitInfo();

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, (int)difficulty, false);
        Debug.Log($"[FreeModeManager] Zorluk: {difficulty}");
    }

    private void SelectMode(LevelSelectManager.GameMode mode)
    {
        SelectedMode = mode;
        LevelSelectManager.SelectedMode = mode;

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, mode == LevelSelectManager.GameMode.Classic ? 0 : 1, false);
        Debug.Log($"[FreeModeManager] Mod: {mode}");
    }

    // ─── BUTON VURGULAMA ──────────────────────────────────────────
    private void HighlightButton(Button[] buttons, int selectedIndex, bool isDifficulty)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            bool isActive = (i == selectedIndex);

            Image bg = buttons[i].GetComponent<Image>();
            if (bg != null)
                bg.color = isActive ? colorActiveBg : colorInactiveBg;

            TextMeshProUGUI txt = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = isActive ? colorActiveText : colorInactiveText;

            Transform borderBottom = buttons[i].transform.Find("Border_Bottom");
            if (borderBottom != null)
            {
                Image borderImg = borderBottom.GetComponent<Image>();
                if (borderImg != null)
                    borderImg.color = isActive ? colorActiveBorder : colorInactiveBorder;
            }
        }
    }

    // ─── LİMİT BİLGİSİ ───────────────────────────────────────────
    private void UpdateLimitInfo()
    {
        if (limitConfig == null) return;
        var limit = limitConfig.GetLimit(SelectedColumns, SelectedRows, SelectedDifficulty);

        if (txtTimeValue != null)
        {
            int minutes = Mathf.FloorToInt(limit.timeLimit / 60);
            int seconds = Mathf.FloorToInt(limit.timeLimit % 60);
            txtTimeValue.text = $"{minutes:00}:{seconds:00}";
        }

        if (txtMoveValue != null)
            txtMoveValue.text = $"{limit.moveLimit}";
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