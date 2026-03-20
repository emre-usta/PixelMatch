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
        SetState(GameState.Initializing);
    }

    private void OnDestroy()
    {
        GameEvents.ClearAllEvents();
    }

    // ─── STATE GEÇİŞLERİ ──────────────────────────────────────────

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
        UnlockNextLevel(stars);

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
        if (LevelSelectManager.SelectedLevel == null) return 0;
        LevelConfig config = LevelSelectManager.SelectedLevel;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        if (isMoveMode)
        {
            if (config.moveLimit <= 0) return 0;
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 99;
            float usageRatio = (float)usedMoves / config.moveLimit;

            if (usageRatio <= 0.45f) return 3;   // %45'inden azını kullandı
            if (usageRatio <= 0.70f) return 2;   // %45–70 arası
            if (usageRatio <= 0.90f) return 1;   // %70–90 arası
            return 0;                             // %90'dan fazla
        }
        else
        {
            if (config.timeLimit <= 0) return 0;
            float usedTime = config.timeLimit - (TimerController.Instance != null
                ? TimerController.Instance.RemainingTime : 0f);
            float usageRatio = usedTime / config.timeLimit;

            if (usageRatio <= 0.45f) return 3;   // %45'inden azını kullandı
            if (usageRatio <= 0.70f) return 2;   // %45–70 arası
            if (usageRatio <= 0.90f) return 1;   // %70–90 arası
            return 0;                             // %90'dan fazla
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

    private void UnlockNextLevel(int stars)
    {
        if (LevelSelectManager.SelectedLevel == null) return;
        if (LevelProgressManager.Instance == null) return;

        int currentLevelID = LevelSelectManager.SelectedLevel.levelID;
        int categoryID = LevelSelectManager.SelectedCategoryID;
        int totalLevels = LevelSelectManager.SelectedCategoryLevelCount;
        int nextLevelID = currentLevelID + 1;
        bool isLastLevel = currentLevelID >= totalLevels - 1;

        // ─── 0 YILDIZ — hiçbir şey açılmaz ───────────────────────
        if (stars == 0)
        {
            int previousBestStars = LevelProgressManager.Instance.GetBestStars(categoryID, currentLevelID);

            if (previousBestStars > 0)
            {
                // Daha önce geçmiş — uyarı gösterme, sessizce bitir
                Debug.Log("[GameStateManager] 0 yıldız ama daha önce geçilmiş — uyarı yok.");
                return;
            }

            // İlk kez 0 yıldız — uyarı göster
            Debug.Log("[GameStateManager] 0 yıldız — level açılmadı.");
            WinPanelController winPanel = panelWin?.GetComponentInChildren<WinPanelController>();
            winPanel?.ShowNoStarWarning();
            return;
        }

        // ─── 1+ YILDIZ — sonraki level açılır ────────────────────
        if (!isLastLevel)
        {
            LevelProgressManager.Instance.UnlockLevel(categoryID, nextLevelID);
            Debug.Log($"[GameStateManager] Level açıldı: Cat{categoryID} Lv{nextLevelID}");
        }
        else
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

        // ─── 2 YILDIZ — Açık Bak kazanılır ──────────────────────
        if (stars == 2)
        {
            // Faz 8'de PowerUpManager.Instance.AddPowerUp(PowerUpType.XRay) olacak
            Debug.Log("[GameStateManager] 2 yıldız — Açık Bak kazanıldı! (Faz 8'de bağlanacak)");
        }

        // ─── 3 YILDIZ — İpucu veya Süre Dondur kazanılır ────────
        if (stars == 3)
        {
            // Rastgele İpucu veya Süre Dondur
            PowerUpType reward = Random.value > 0.5f ? PowerUpType.MindFreeze : PowerUpType.TimeFreeze;
            // Faz 8'de PowerUpManager.Instance.AddPowerUp(reward) olacak
            Debug.Log($"[GameStateManager] 3 yıldız — {reward} kazanıldı! (Faz 8'de bağlanacak)");
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

public enum GameState
{
    Initializing,
    Playing,
    Paused,
    Win,
    Lose
}