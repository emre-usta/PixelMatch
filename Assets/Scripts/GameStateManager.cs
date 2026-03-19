using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PixelMatch — Oyun State Machine
/// Oyunun her anında hangi state'te olduğunu bilen tek otorite.
/// Sahneye tek bir GameManager objesi olarak eklenir.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Initializing;


    [Header("UI Panelleri")]
    [SerializeField] private GameObject panelPause;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelGameOver;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SetState(GameState.Initializing);  // Awake'e taşıdık
    }

    private void Start()
    {
        //SetState(GameState.Initializing);
    }

    private void OnDestroy()
    {
        // Sahne değişince tüm event aboneliklerini temizle
        GameEvents.ClearAllEvents();
    }

    // ─── STATE GEÇİŞLERİ ──────────────────────────────────────────

    public void StartGame()
    {
        //SetState(GameState.Initializing);

        // Grid hazır olunca Playing state'e geç
        // GridManager hazır olduğunda bu çağrılacak
    }

    public void OnGridReady()
    {
        SetState(GameState.Playing);
        GameEvents.RaiseGameStarted();
    }
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
        GameEvents.RaiseGamePaused();
        if (panelPause != null) panelPause.SetActive(true);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        GameEvents.RaiseGameResumed();
        if (panelPause != null) panelPause.SetActive(false);
    }

    public void WinGame()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Win);
        Time.timeScale = 0f;
        GameEvents.RaiseGameWon();

        int stars = CalculateStars();
        SaveStars(stars);
        UnlockNextLevel();

        if (panelWin != null)
        {
            panelWin.SetActive(true);
            WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
            if (winPanel != null)
                winPanel.ShowResult(stars);
        }
    }

    private int CalculateStars()
    {
        if (LevelSelectManager.SelectedLevel == null) return 1;
        LevelConfig config = LevelSelectManager.SelectedLevel;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        if (isMoveMode)
        {
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 99;
            if (usedMoves <= config.threeStarMoves) return 3;
            if (usedMoves <= config.twoStarMoves) return 2;
            return 1;
        }
        else
        {
            float remaining = TimerController.Instance != null ? TimerController.Instance.RemainingTime : 0f;
            if (remaining >= config.threeStarTime) return 3;
            if (remaining >= config.twoStarTime) return 2;
            return 1;
        }
    }

    private void SaveStars(int stars)
    {
        if (LevelProgressManager.Instance == null) return;
        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel.levelID;
        LevelProgressManager.Instance.SaveBestStars(categoryID, levelID, stars);
        Debug.Log($"[GameStateManager] {stars} yıldız kazanıldı ve kaydedildi.");
    }

    private void UnlockNextLevel()
    {
        if (LevelSelectManager.SelectedLevel == null) return;
        if (LevelProgressManager.Instance == null) return;

        int currentLevelID = LevelSelectManager.SelectedLevel.levelID;
        int categoryID = LevelSelectManager.SelectedCategoryID;
        int totalLevels = LevelSelectManager.SelectedCategoryLevelCount;
        int nextLevelID = currentLevelID + 1;

        LevelProgressManager.Instance.UnlockLevel(categoryID, nextLevelID);
        Debug.Log($"[GameStateManager] Level açıldı: Category {categoryID}, Level {nextLevelID}, Total: {totalLevels}");

        if (currentLevelID >= totalLevels - 1)
        {
            int nextCategoryID = categoryID + 1;
            if (LevelSelectManager.Instance != null &&
                nextCategoryID < LevelSelectManager.TotalCategoryCount)
            {
                LevelProgressManager.Instance.UnlockCategory(nextCategoryID);
                Debug.Log($"[GameStateManager] Kategori açıldı: {nextCategoryID}");
            }
            else
            {
                Debug.Log("[GameStateManager] Son kategori tamamlandı.");
            }
        }
    }

    public void LoseGame()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Lose);
        Time.timeScale = 0f;
        GameEvents.RaiseGameLost();
        if (panelGameOver != null) panelGameOver.SetActive(true);
    }

    // ─── YARDIMCI METOTLAR ────────────────────────────────────────

    /// <summary>Şu an oynanabilir durumda mı? Input kabul edilsin mi?</summary>
    public bool IsPlaying => CurrentState == GameState.Playing;

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameStateManager] State: {newState}");
    }

    // ─── SAHNE YÖNETİMİ ───────────────────────────────────────────

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextOrRetryWin()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }
}

// ─── GAME STATE ENUM ──────────────────────────────────────────────

public enum GameState
{
    Initializing,   // Grid kuruluyor
    Playing,        // Oyuncu oynuyor
    Paused,         // Duraklатıldı
    Win,            // Kazandı
    Lose            // Kaybetti
}