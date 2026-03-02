using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LevelSelectManager;

/// <summary>
/// PixelMatch — Level Popup Yöneticisi
/// Level seçilince açılan mod seçim popup'ını yönetir.
/// </summary>
public class LevelPopupManager : MonoBehaviour
{
    public static LevelPopupManager Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Popup")]
    [SerializeField] private GameObject popupPanel;

    [Header("UI Elementleri")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI classicModeText;
    [SerializeField] private TextMeshProUGUI moveModeText;

    // ─── RUNTIME VERİSİ ───────────────────────────────────────────

    private LevelConfig pendingLevel;
    private int pendingLevelIndex;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // ─── POPUP AÇMA ───────────────────────────────────────────────

    public void ShowPopup(LevelConfig level, int levelIndex)
    {
        pendingLevel = level;
        pendingLevelIndex = levelIndex;

        // Level adını güncelle
        if (levelNameText != null)
            levelNameText.text = level.levelName;

        // Klasik mod bilgisini güncelle
        if (classicModeText != null)
        {
            int minutes = Mathf.FloorToInt(level.timeLimit / 60);
            int seconds = Mathf.FloorToInt(level.timeLimit % 60);
            classicModeText.text = $"KLASİK MOD\nSüre: {minutes:00}:{seconds:00}";
        }

        // Hamle mod bilgisini güncelle
        if (moveModeText != null)
            moveModeText.text = $"HAMLE MODU\n{level.moveLimit} Hamle";

        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    // ─── BUTON HANDLER'LARI ───────────────────────────────────────

    public void OnClassicModeClicked()
    {
        LevelSelectManager.SelectedMode = GameMode.Classic;
        StartLevel();
    }

    public void OnMoveModeClicked()
    {
        LevelSelectManager.SelectedMode = GameMode.Move;
        StartLevel();
    }

    public void OnCloseClicked()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // ─── LEVEL BAŞLATMA ───────────────────────────────────────────

    private void StartLevel()
    {
        if (pendingLevel == null) return;

        LevelSelectManager.SetSelectedLevel(pendingLevel, pendingLevelIndex);

        if (popupPanel != null)
            popupPanel.SetActive(false);

        SceneManager.LoadScene("Level1");
    }
}