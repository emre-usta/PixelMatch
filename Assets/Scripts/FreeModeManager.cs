using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Kategori Scroll")]
    [SerializeField] private Transform categoryContent;
    [SerializeField] private Button categoryButtonPrefab;

    [Header("Grid Butonları")]
    [SerializeField] private Button btn4x4;
    [SerializeField] private Button btn4x5;
    [SerializeField] private Button btn5x6;
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

    private List<Button> categoryButtons = new List<Button>();
    private int selectedCategoryIndex = 0;
    private bool isStarting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            OnBackClicked();
    }

    private void Start()
    {
        SelectedColumns = 4;
        SelectedRows = 4;
        SelectedDifficulty = DifficultyLevel.Easy;
        SelectedMode = LevelSelectManager.GameMode.Classic;

        if (categories != null && categories.Length > 0)
            SelectedCategory = categories[0];

        CreateCategoryButtons();
        SetupButtons();
        UpdateLimitInfo();
        UpdateButtonLabels();

        Button[] gridBtns = { btn4x4, btn4x5, btn5x6, btn6x6 };
        HighlightButton(gridBtns, 0);

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, 0);

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, 0);

        HighlightCategoryButton(0);
    }

    // ─── KATEGORİ BUTON OLUŞTURMA ────────────────────────────────

    private void CreateCategoryButtons()
    {
        if (categoryContent == null || categoryButtonPrefab == null) return;

        // Mevcut butonları temizle
        foreach (Transform child in categoryContent)
            Destroy(child.gameObject);

        categoryButtons.Clear();

        for (int i = 0; i < categories.Length; i++)
        {
            int index = i;
            CategoryConfig cat = categories[i];

            Button btn = Instantiate(categoryButtonPrefab, categoryContent);
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = LocalizationManager.Get(cat.categoryName);

            btn.onClick.AddListener(() => SelectCategory(cat, index));
            categoryButtons.Add(btn);
        }
    }

    // ─── BUTON KURULUM ────────────────────────────────────────────

    private void SetupButtons()
    {
        btn4x4?.onClick.AddListener(() => SelectGrid(4, 4));
        btn4x5?.onClick.AddListener(() => SelectGrid(4, 5));
        btn5x6?.onClick.AddListener(() => SelectGrid(5, 6));
        btn6x6?.onClick.AddListener(() => SelectGrid(6, 6));

        btnEasy?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Easy));
        btnMedium?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Medium));
        btnHard?.onClick.AddListener(() => SelectDifficulty(DifficultyLevel.Hard));

        btnClassic?.onClick.AddListener(() => SelectMode(LevelSelectManager.GameMode.Classic));
        btnMove?.onClick.AddListener(() => SelectMode(LevelSelectManager.GameMode.Move));

        btnStart?.onClick.AddListener(StartFreeMode);
    }

    // ─── STATİK RESET ─────────────────────────────────────────────

    public static void ResetFreeModeState()
    {
        IsFreeModeActive = false;
        SelectedColumns = 4;
        SelectedRows = 4;
        SelectedDifficulty = DifficultyLevel.Easy;
        SelectedMode = LevelSelectManager.GameMode.Classic;
        SelectedCategory = null;
    }

    // ─── DİL GÜNCELLEMESİ ────────────────────────────────────────

    public void UpdateButtonLabels()
    {
        SetButtonLabel(btnEasy, "difficulty_easy");
        SetButtonLabel(btnMedium, "difficulty_medium");
        SetButtonLabel(btnHard, "difficulty_hard");
        SetButtonLabel(btnClassic, "mode_classic");
        SetButtonLabel(btnMove, "mode_move");

        for (int i = 0; i < categoryButtons.Count && i < categories.Length; i++)
        {
            TextMeshProUGUI txt = categoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = LocalizationManager.Get(categories[i].categoryName);
        }

        UpdateLimitInfo();
    }

    private void SetButtonLabel(Button btn, string key)
    {
        if (btn == null) return;
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = LocalizationManager.Get(key);
    }

    // ─── SEÇİM METOTLARI ──────────────────────────────────────────

    private void SelectCategory(CategoryConfig category, int index)
    {
        SelectedCategory = category;
        selectedCategoryIndex = index;
        HighlightCategoryButton(index);
    }

    private void SelectGrid(int cols, int rows)
    {
        SelectedColumns = cols;
        SelectedRows = rows;
        UpdateLimitInfo();

        Button[] gridBtns = { btn4x4, btn4x5, btn5x6, btn6x6 };
        int index = (cols == 4 && rows == 4) ? 0 :
                    (cols == 4 && rows == 5) ? 1 :
                    (cols == 5 && rows == 6) ? 2 : 3;
        HighlightButton(gridBtns, index);
    }

    private void SelectDifficulty(DifficultyLevel difficulty)
    {
        SelectedDifficulty = difficulty;
        UpdateLimitInfo();

        Button[] diffBtns = { btnEasy, btnMedium, btnHard };
        HighlightButton(diffBtns, (int)difficulty);
    }

    private void SelectMode(LevelSelectManager.GameMode mode)
    {
        SelectedMode = mode;
        LevelSelectManager.SelectedMode = mode;

        Button[] modeBtns = { btnClassic, btnMove };
        HighlightButton(modeBtns, mode == LevelSelectManager.GameMode.Classic ? 0 : 1);
    }

    // ─── BUTON VURGULAMA ──────────────────────────────────────────

    private void HighlightCategoryButton(int selectedIndex)
    {
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            if (categoryButtons[i] == null) continue;

            bool isActive = (i == selectedIndex);

            Image bg = categoryButtons[i].GetComponent<Image>();
            if (bg != null)
                bg.color = isActive ? colorActiveBg : colorInactiveBg;

            TextMeshProUGUI txt = categoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = isActive ? colorActiveText : colorInactiveText;

            Transform borderBottom = categoryButtons[i].transform.Find("Border_Bottom");
            if (borderBottom != null)
            {
                Image borderImg = borderBottom.GetComponent<Image>();
                if (borderImg != null)
                    borderImg.color = isActive ? colorActiveBorder : colorInactiveBorder;
            }
        }
    }

    private void HighlightButton(Button[] buttons, int selectedIndex)
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
        if (SelectedCategory == null) return;
        if (isStarting) return;
        isStarting = true;

        IsFreeModeActive = true;
        LevelSelectManager.SelectedMode = SelectedMode;
        GameSession.Begin(SessionContext.ForFreeMode(
            SelectedCategory, SelectedColumns, SelectedRows, SelectedDifficulty, SelectedMode, limitConfig));
        SceneManager.LoadScene("Level1");
    }

    public void OnBackClicked()
    {
        // StartFreeMode ile aynı tek seferlik sahne geçiş bayrağı (AUDIT 1.23 tipi koruma).
        if (isStarting) return;
        isStarting = true;

        IsFreeModeActive = false;
        SceneManager.LoadScene("MainMenu");
    }
}