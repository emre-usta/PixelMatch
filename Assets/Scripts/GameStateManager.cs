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

        // Serbest mod
        if (FreeModeManager.IsFreeModeActive)
        {
            if (panelWin != null) panelWin.SetActive(true);
            return;
        }

        // Daily Challenge
        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            int stars = CalculateStars();
            DailyChallengeManager.Instance.MarkPlayedToday(stars);
            GiveDailyChallengeReward(stars);

            if (panelWin != null)
            {
                panelWin.SetActive(true);
                WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
                if (winPanel != null)
                    winPanel.ShowResult(stars);
                else
                    Debug.LogWarning("[DC] WinPanelController bulunamadı!");
            }
            return;
        }

        // Normal hikaye modu
        int starsNormal = CalculateStars();
        SaveStars(starsNormal);
        UnlockNextLevel(starsNormal);

        if (panelWin != null)
        {
            panelWin.SetActive(true);
            WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
            winPanel?.ShowResult(starsNormal);
        }
    }

    private void GiveDailyChallengeReward(int stars)
    {
        if (PowerUpManager.Instance == null) return;

        // Her tamamlamada 1 İpucu
        PowerUpManager.Instance.AddPowerUp(PowerUpType.MindFreeze);
        Debug.Log("[DC] Ödül: 1 İpucu kazanıldı!");

        // 3 yıldızda ekstra Süre Dondur
        if (stars == 3)
        {
            PowerUpManager.Instance.AddPowerUp(PowerUpType.TimeFreeze);
            Debug.Log("[DC] Bonus: 1 Süre Dondur kazanıldı!");
        }
    }

    private int CalculateStars()
    {
        // DC ve Serbest Mod için özel hesaplama
        bool isDC = DailyChallengeManager.IsDailyChallengeActive;
        bool isFree = FreeModeManager.IsFreeModeActive;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        float timeLimit = 0f;
        int moveLimit = 0;

        if (isDC || isFree)
        {
            // FreeModeLimitConfig'den limit al
            int cols = isDC ? DailyChallengeManager.DCColumns : FreeModeManager.SelectedColumns;
            int rows = isDC ? DailyChallengeManager.DCRows : FreeModeManager.SelectedRows;
            DifficultyLevel diff = isDC ? DailyChallengeManager.DCDifficulty : FreeModeManager.SelectedDifficulty;

            // TimerController ve MoveController'dan mevcut limitleri oku
            timeLimit = TimerController.Instance != null
                ? TimerController.Instance.TotalTime : 120f;
            moveLimit = MoveController.Instance != null
                ? MoveController.Instance.MoveLimit : 30;
        }
        else
        {
            if (LevelSelectManager.SelectedLevel == null) return 0;
            LevelConfig config = LevelSelectManager.SelectedLevel;
            timeLimit = config.timeLimit;
            moveLimit = config.moveLimit;
        }

        if (isMoveMode)
        {
            if (moveLimit <= 0) return 0;
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 99;
            float usageRatio = (float)usedMoves / moveLimit;
            if (usageRatio <= 0.45f) return 3;
            if (usageRatio <= 0.70f) return 2;
            if (usageRatio <= 0.90f) return 1;
            return 0;
        }
        else
        {
            if (timeLimit <= 0) return 0;
            float usedTime = timeLimit - (TimerController.Instance != null
                ? TimerController.Instance.RemainingTime : 0f);
            float usageRatio = usedTime / timeLimit;
            if (usageRatio <= 0.45f) return 3;
            if (usageRatio <= 0.70f) return 2;
            if (usageRatio <= 0.90f) return 1;
            return 0;
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
            PowerUpManager.Instance?.AddPowerUp(PowerUpType.XRay);
            Debug.Log("[GameStateManager] 2 yıldız — Açık Bak kazanıldı!");
        }

        if (stars == 3)
        {
            PowerUpType reward = Random.value > 0.5f
                ? PowerUpType.MindFreeze
                : PowerUpType.TimeFreeze;
            PowerUpManager.Instance?.AddPowerUp(reward);
            Debug.Log($"[GameStateManager] 3 yıldız — {reward} kazanıldı!");
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