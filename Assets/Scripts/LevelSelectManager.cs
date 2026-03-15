using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PixelMatch — Level Seçim Yöneticisi
/// Kategorileri ve levelleri listeler, seçilen level'ı yükler.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    public enum GameMode
    {
        Classic,
        Move
    }

    public static GameMode SelectedMode { get; set; }
    public static LevelSelectManager Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Kategoriler")]
    [SerializeField] private CategoryConfig[] categories;
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("Levellar")]
    [SerializeField] private Transform levelContainer;
    [SerializeField] private GameObject levelCardPrefab;

    [Header("Seçili Kategori")]
    private CategoryConfig selectedCategory;
    private int selectedCategoryIndex = 0;

    // ─── SEÇİLEN LEVEL ────────────────────────────────────────────

    public static LevelConfig SelectedLevel { get; private set; }
    public static int SelectedCategoryID { get; private set; }
    public static int SelectedCategoryLevelCount { get; private set; }

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (categories == null || categories.Length == 0)
        {
            Debug.LogError("[LevelSelectManager] Kategori atanmamış!");
            return;
        }

        LoadCategory(0);
    }

    // ─── KATEGORİ YÜKLEMESİ ──────────────────────────────────────

    public void LoadCategory(int index)
    {
        if (index < 0 || index >= categories.Length) return;

        selectedCategoryIndex = index;
        selectedCategory = categories[index];

        // Mevcut level kartlarını temizle
        foreach (Transform child in levelContainer)
            Destroy(child.gameObject);

        // Level kartlarını oluştur
        for (int i = 0; i < selectedCategory.levels.Length; i++)
        {
            LevelConfig level = selectedCategory.levels[i];
            bool isUnlocked = LevelProgressManager.Instance != null
                ? LevelProgressManager.Instance.IsLevelUnlocked(selectedCategory.categoryID, i)
                : i == 0;

            CreateLevelCard(level, i, isUnlocked);
        }
    }

    public void OnCategoryClicked(int index)
    {
        if (LevelProgressManager.Instance != null &&
            !LevelProgressManager.Instance.IsCategoryUnlocked(index))
        {
            Debug.Log($"[LevelSelectManager] Kategori kilitli: {index}");
            return;
        }
        LoadCategory(index);
    }

    // ─── LEVEL KARTI OLUŞTURMA ────────────────────────────────────

    private void CreateLevelCard(LevelConfig level, int levelIndex, bool isUnlocked)
    {
        GameObject card = Instantiate(levelCardPrefab, levelContainer);

        // Level adı
        TextMeshProUGUI nameText = card.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = isUnlocked ? level.levelName : "🔒";

        // Buton
        Button btn = card.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = isUnlocked;

            if (isUnlocked)
            {
                int capturedIndex = levelIndex;
                btn.onClick.AddListener(() =>
                    LevelPopupManager.Instance.ShowPopup(level, capturedIndex));
            }
        }
    }

    // ─── LEVEL SEÇİMİ ─────────────────────────────────────────────
    public static void SetSelectedLevel(LevelConfig level, int levelIndex)
    {
        SelectedLevel = level;
        SelectedCategoryID = Instance.selectedCategory.categoryID;
        SelectedCategoryLevelCount = Instance.selectedCategory.levels.Length;
    }

    // ─── KATEGORİ SEÇİMİ ─────────────────────────────────────────────

    public static bool IsLastLevelInCategory(int levelID)
    {
        if (Instance == null || Instance.categories == null) return false;

        CategoryConfig category = null;
        foreach (var cat in Instance.categories)
            if (cat.categoryID == SelectedCategoryID)
                category = cat;

        if (category == null) return false;

        Debug.Log($"[LevelSelectManager] levelID: {levelID}, category.levels.Length: {category.levels.Length}");
        return levelID >= category.levels.Length - 1;
    }

    // ─── BUTON HANDLER'LARI ───────────────────────────────────────

    public void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}