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
                btn.onClick.AddListener(() => OnLevelSelected(capturedIndex));
            }
        }
    }

    // ─── LEVEL SEÇİMİ ─────────────────────────────────────────────

    private void OnLevelSelected(int levelIndex)
    {
        SelectedLevel = selectedCategory.levels[levelIndex];
        Debug.Log($"[LevelSelectManager] Level seçildi: {SelectedLevel.levelName}");
        SceneManager.LoadScene("Level1");
    }

    // ─── BUTON HANDLER'LARI ───────────────────────────────────────

    public void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnCategoryClicked(int index)
    {
        LoadCategory(index);
    }
}