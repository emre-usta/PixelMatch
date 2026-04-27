using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LevelSelectManager;

public class LevelPopupManager : MonoBehaviour
{
    public static LevelPopupManager Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupPanel;

    [Header("UI Elementleri")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI classicModeText;
    [SerializeField] private TextMeshProUGUI moveModeText;
    [SerializeField] private TextMeshProUGUI classicDetailText;
    [SerializeField] private TextMeshProUGUI moveDetailText;

    private LevelConfig pendingLevel;
    private int pendingLevelIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // ─── POPUP AÇMA ───────────────────────────────────────────────
    public void ShowPopup(LevelConfig level, int levelIndex)
    {
        pendingLevel = level;
        pendingLevelIndex = levelIndex;

        // Level adı
        if (levelNameText != null)
            levelNameText.text = level.levelName;

        // Klasik mod başlığı
        if (classicModeText != null)
            classicModeText.text = LocalizationManager.Get("classic_title");

        // Hamle mod başlığı
        if (moveModeText != null)
            moveModeText.text = LocalizationManager.Get("movement_title");

        // Klasik mod detay
        if (classicDetailText != null)
        {
            int minutes = Mathf.FloorToInt(level.timeLimit / 60);
            int seconds = Mathf.FloorToInt(level.timeLimit % 60);
            string timeLabel = LocalizationManager.Get("time_label");
            classicDetailText.text = $"{timeLabel}: {minutes:00}:{seconds:00}  ·  {level.columns}×{level.rows}";
        }

        // Hamle mod detay
        if (moveDetailText != null)
        {
            string moveLabel = LocalizationManager.Get("move_label");
            moveDetailText.text = $"{level.moveLimit} {moveLabel}  ·  {level.columns}×{level.rows}";
        }

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