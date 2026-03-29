using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Initializing;

    [Header("UI Panelleri")]
    [SerializeField] private GameObject panelPause;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetState(GameState.Initializing);
    }

    private void OnDestroy()
    {
        GameEvents.ClearAllEvents();
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

        // Serbest mod
        if (FreeModeManager.IsFreeModeActive)
        {
            if (panelWin != null) panelWin.SetActive(true);
            return;
        }

        // Daily Challenge
        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            int stars = 3;
            DailyChallengeManager.Instance.MarkPlayedToday(stars);
            DailyChallengeManager.SetInactive();
            GiveDailyChallengeReward(stars);

            if (panelWin != null)
            {
                panelWin.SetActive(true);
                var buttons = panelWin.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                    if (btn.gameObject.name == "Btn_Retry")
                        btn.gameObject.SetActive(false);
                WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
                winPanel?.ShowResult(stars);
            }
            return;
        }

        // Normal hikaye modu
        int starsNormal = CalculateStars();
        SaveStars(starsNormal);
        SaveRecord(starsNormal); // ← YENİ
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
        PowerUpManager.Instance.AddPowerUp(PowerUpType.MindFreeze);
        PowerUpManager.Instance.AddPowerUp(PowerUpType.TimeFreeze);
        Debug.Log("[DC] Ödül: 1 İpucu + 1 Süre Dondur kazanıldı!");
    }

    private int CalculateStars()
    {
        bool isFree = FreeModeManager.IsFreeModeActive;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        float timeLimit = 0f;
        int moveLimit = 0;

        if (isFree)
        {
            timeLimit = TimerController.Instance != null ? TimerController.Instance.TotalTime : 120f;
            moveLimit = MoveController.Instance != null ? MoveController.Instance.MoveLimit : 30;
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
        Debug.Log($"[GameStateManager] {stars} yıldız kaydedildi.");
    }

    // ─── REKOR KAYDETME ───────────────────────────────────────────
    private void SaveRecord(int stars)
    {
        if (stars == 0) return;
        if (LevelSelectManager.SelectedLevel == null) return;

        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel.levelID;
        int mode = (int)LevelSelectManager.SelectedMode;
        string recordKey = $"record_{categoryID}_{levelID}_{mode}";
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        Debug.Log($"[SaveRecord] categoryID:{categoryID}, levelID:{levelID}, mode:{mode}, key:{recordKey}");


        if (isMoveMode)
        {
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 0;
            int existing = PlayerPrefs.GetInt(recordKey + "_moves", 9999);
            if (usedMoves < existing)
            {
                PlayerPrefs.SetInt(recordKey + "_moves", usedMoves);
                PlayerPrefs.Save();
                Debug.Log($"[GameStateManager] Hamle rekoru kaydedildi: {usedMoves}");
            }
        }
        else
        {
            float usedTime = LevelSelectManager.SelectedLevel.timeLimit -
                (TimerController.Instance != null ? TimerController.Instance.RemainingTime : 0f);
            float existing = PlayerPrefs.GetFloat(recordKey, 9999f);
            if (usedTime < existing)
            {
                PlayerPrefs.SetFloat(recordKey, usedTime);
                PlayerPrefs.Save();
                Debug.Log($"[GameStateManager] Süre rekoru kaydedildi: {usedTime:F1}s");
            }
        }
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

        if (stars == 0)
        {
            int previousBestStars = LevelProgressManager.Instance.GetBestStars(categoryID, currentLevelID);
            if (previousBestStars > 0)
            {
                Debug.Log("[GameStateManager] 0 yıldız ama daha önce geçilmiş — uyarı yok.");
                return;
            }
            Debug.Log("[GameStateManager] 0 yıldız — level açılmadı.");
            WinPanelController winPanel = panelWin?.GetComponentInChildren<WinPanelController>();
            winPanel?.ShowNoStarWarning();
            return;
        }

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

        if (stars == 2)
        {
            PowerUpManager.Instance?.AddPowerUp(PowerUpType.XRay);
            Debug.Log("[GameStateManager] 2 yıldız — Açık Bak kazanıldı!");
        }

        if (stars == 3)
        {
            PowerUpType reward = Random.value > 0.5f ? PowerUpType.MindFreeze : PowerUpType.TimeFreeze;
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

        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            DailyChallengeManager.Instance?.AddAttempt();
            bool hasAttempts = DailyChallengeManager.Instance.HasAttemptsLeft();

            if (!hasAttempts) DailyChallengeManager.SetInactive();

            if (panelGameOver != null) panelGameOver.SetActive(true);

            var buttons = panelGameOver.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name == "Btn_Retry")
                {
                    btn.interactable = hasAttempts;
                    TMPro.TextMeshProUGUI txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (txt != null)
                        txt.text = hasAttempts ? "TEKRAR" : "HAKKIN DOLDU";
                }
            }
            return;
        }

        if (panelGameOver != null) panelGameOver.SetActive(true);
    }

    public bool IsPlaying => CurrentState == GameState.Playing;

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameStateManager] State: {newState}");
    }

    public void LoadMainMenu()
    {
        if (DailyChallengeManager.IsDailyChallengeActive)
            DailyChallengeManager.SetInactive();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            if (!DailyChallengeManager.Instance.HasAttemptsLeft())
            {
                DailyChallengeManager.SetInactive();
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
                Debug.Log("[DC] Deneme hakkı doldu, ana menüye dönüldü.");
                return;
            }
        }
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