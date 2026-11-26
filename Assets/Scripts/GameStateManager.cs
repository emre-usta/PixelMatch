using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Initializing;

    public static int OriginalMoveLimit { get; private set; }
    public static float OriginalTimeLimit { get; private set; }

    [Header("UI Panelleri")]
    [SerializeField] private GameObject panelPause;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelGameOver;

    // Son eşleşme ile kazanma sonucunun gösterilmesi arasındaki bekleme (eski GridManager.WinDelayed süresi).
    private const float WIN_PRESENTATION_DELAY = 0.8f;

    private bool gridReady = false;
    private bool losePending = false;
    private bool winResultShown = false;
    private bool isLeavingScene = false;

    private void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Debug.unityLogger.logEnabled = false;
#endif
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetState(GameState.Initializing);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingsManager.Instance != null &&
                SettingsManager.Instance.IsSettingsOpen)
            {
                SettingsManager.Instance.CloseSettings();
                return;
            }

            if (CurrentState == GameState.Playing)
            {
                PauseGame();
                return;
            }

            if (CurrentState == GameState.Paused)
            {
                ResumeGame();
                return;
            }
        }
    }

    private void OnDestroy()
    {
        GameEvents.ClearAllEvents();
    }

    public void OnGridReady()
    {
        gridReady = true;
        TryStartGame();
    }

    public void OnTutorialCompleted()
    {
        TryStartGame();
    }

    // Oyunu oyun başına yalnızca bir kez başlatır: grid hazır olmalı, state Initializing olmalı.
    private void TryStartGame()
    {
        if (CurrentState != GameState.Initializing || !gridReady) return;

        bool isAdUnlockedLevel = LevelSelectManager.SelectedLevel != null &&
            PlayerPrefs.GetInt(
                $"ad_unlocked_{LevelSelectManager.SelectedCategoryID}_{LevelSelectManager.SelectedLevel.levelID}", 0) == 1;

        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.gameObject.activeInHierarchy &&
            PlayerPrefs.GetInt("tutorial_done", 0) == 0 &&
            !DailyChallengeManager.IsDailyChallengeActive &&
            !FreeModeManager.IsFreeModeActive &&
            !isAdUnlockedLevel)
            return;

        OriginalMoveLimit = LevelSelectManager.SelectedLevel?.moveLimit ??
                            (MoveController.Instance?.MoveLimit ?? 0);
        OriginalTimeLimit = LevelSelectManager.SelectedLevel?.timeLimit ??
                            (TimerController.Instance?.TotalTime ?? 0f);

        SetState(GameState.Playing);
        GameEvents.RaiseGameStarted();

        if (!DailyChallengeManager.IsDailyChallengeActive && !FreeModeManager.IsFreeModeActive)
            FirebaseManager.Instance?.LogLevelStart(
                LevelSelectManager.SelectedCategoryID,
                LevelSelectManager.SelectedLevel?.levelID ?? 0);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing || losePending) return;
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
        TimerController.Instance?.ResumeTimer();
        if (panelPause != null) panelPause.SetActive(false);
    }

    // ─── OYUN SONU (TEK OTORİTE) ──────────────────────────────────
    // Kazanma hemen kesinleşir. Kaybetme isteği bir kare bekletilir; böylece aynı karede
    // gerçekleşen son eşleşme (abonelik sırasından bağımsız olarak) her zaman önceliklidir.

    public void WinGame() => EndGame(GameResult.Win);

    public void LoseGame()
    {
        if (CurrentState != GameState.Playing || losePending) return;
        losePending = true;
        StartCoroutine(ResolveLoseNextFrame());
    }

    private IEnumerator ResolveLoseNextFrame()
    {
        yield return null;
        losePending = false;

        bool boardCleared = GridManager.Instance != null && GridManager.Instance.RemainingPairs == 0;
        if (boardCleared)
        {
            EndGame(GameResult.Win);
            yield break;
        }

        // Aynı karede gelen bir ödül (ör. combo) kaybetme koşulunu ortadan kaldırdıysa kaybetmeyi iptal et.
        if (CurrentState == GameState.Playing && !IsLoseConditionMet())
        {
            bool isMoveLimited = MoveController.Instance != null && MoveController.Instance.UseMoveLimit;
            if (!isMoveLimited)
                TimerController.Instance?.ResumeTimer();
            yield break;
        }

        EndGame(GameResult.Lose);
    }

    private bool IsLoseConditionMet()
    {
        if (MoveController.Instance != null && MoveController.Instance.UseMoveLimit)
            return MoveController.Instance.RemainingMoves <= 0;

        return TimerController.Instance == null || TimerController.Instance.RemainingTime <= 0;
    }

    private void EndGame(GameResult result)
    {
        if (CurrentState != GameState.Playing) return;

        if (result == GameResult.Win)
            BeginWin();
        else
            BeginLose();
    }

    private void BeginWin()
    {
        SetState(GameState.Win);
        TimerController.Instance?.StopTimer();
        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        yield return new WaitForSecondsRealtime(WIN_PRESENTATION_DELAY);

        Time.timeScale = 0f;
        GameEvents.RaiseGameWon();

        float delay = ComboEffectManager.Instance != null ? 1.2f : 0f;
        yield return StartCoroutine(ShowWinPanelDelayed(delay));
    }

    private IEnumerator ShowWinPanelDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        winResultShown = true;

        if (FreeModeManager.IsFreeModeActive)
        {
            if (panelWin != null)
            {
                panelWin.SetActive(true);
                int stars = CalculateStars();
                WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
                winPanel?.ShowFreeModeResult(stars);
            }
            FreeModeManager.ResetFreeModeState();
            yield break;
        }

        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            int stars = CalculateStars();
            DailyChallengeManager.Instance.MarkPlayedToday(stars);
            DailyChallengeManager.SetInactive();

            yield return new WaitUntil(() => PowerUpManager.Instance != null);

            GiveDailyChallengeReward(stars);
            LogLevelEndEvent(success: true, stars: stars);

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
            yield break;
        }

        int starsNormal = CalculateStars();
        SaveStars(starsNormal);
        SaveRecord(starsNormal);
        UnlockNextLevel(starsNormal);
        LogLevelEndEvent(success: true, stars: starsNormal);

        if (panelWin != null)
        {
            panelWin.SetActive(true);
            WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
            winPanel?.ShowResult(starsNormal);
        }
    }

    private void NotifyWinPanel(PowerUpType type)
    {
        if (panelWin == null) return;
        WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
        winPanel?.AddPendingReward(type, 1);
    }

    private void GiveDailyChallengeReward(int stars)
    {
        FirebaseManager.Instance?.LogDailyChallengeComplete(stars);

        if (PowerUpManager.Instance == null) return;

        if (stars == 1)
        {
            PowerUpManager.Instance.AddPowerUp(PowerUpType.TimeFreeze);
            NotifyWinPanel(PowerUpType.TimeFreeze);
        }
        else if (stars == 2)
        {
            PowerUpManager.Instance.AddPowerUp(PowerUpType.TimeFreeze);
            NotifyWinPanel(PowerUpType.TimeFreeze);
            PowerUpManager.Instance.AddPowerUp(PowerUpType.XRay);
            NotifyWinPanel(PowerUpType.XRay);
        }
        else if (stars == 3)
        {
            PowerUpManager.Instance.AddPowerUp(PowerUpType.XRay);
            NotifyWinPanel(PowerUpType.XRay);
            PowerUpManager.Instance.AddPowerUp(PowerUpType.MindFreeze);
            NotifyWinPanel(PowerUpType.MindFreeze);
        }
    }

    private int CalculateStars()
    {
        bool isFree = FreeModeManager.IsFreeModeActive;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        float timeLimit = 0f;
        int moveLimit = 0;

        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            timeLimit = TimerController.Instance != null ? TimerController.Instance.TotalTime : 120f;
            moveLimit = MoveController.Instance != null ? MoveController.Instance.MoveLimit : 30;
        }
        else if (isFree)
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
            float remainingRatio = (float)(moveLimit - usedMoves) / moveLimit;
            if (remainingRatio >= 0.50f) return 3;
            if (remainingRatio >= 0.20f) return 2;
            return 1;
        }
        else
        {
            if (timeLimit <= 0) return 0;
            float remainingTime = TimerController.Instance != null ? TimerController.Instance.RemainingTime : 0f;
            float remainingRatio = remainingTime / timeLimit;
            if (remainingRatio >= 0.60f) return 3;
            if (remainingRatio >= 0.30f) return 2;
            return 1;
        }
    }

    private void SaveStars(int stars)
    {
        if (LevelProgressManager.Instance == null) return;

        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel.levelID;
        int previousStars = LevelProgressManager.Instance.GetBestStars(categoryID, levelID);

        if (stars > previousStars)
        {
            int diff = stars - previousStars;
            int totalStars = PlayerPrefs.GetInt("total_stars", 0);
            PlayerPrefs.SetInt("total_stars", totalStars + diff);
            PlayerPrefs.Save();
        }

        LevelProgressManager.Instance.SaveBestStars(categoryID, levelID, stars);
    }

    private void SaveRecord(int stars)
    {
        if (LevelSelectManager.SelectedLevel == null) return;

        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel.levelID;
        int mode = (int)LevelSelectManager.SelectedMode;
        string recordKey = $"record_{categoryID}_{levelID}_{mode}";
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        if (isMoveMode)
        {
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 0;
            int existing = PlayerPrefs.GetInt(recordKey + "_moves", 9999);
            if (usedMoves < existing) { PlayerPrefs.SetInt(recordKey + "_moves", usedMoves); PlayerPrefs.Save(); }
        }
        else
        {
            float usedTime = LevelSelectManager.SelectedLevel.timeLimit -
                (TimerController.Instance != null ? TimerController.Instance.RemainingTime : 0f);
            float existing = PlayerPrefs.GetFloat(recordKey, 9999f);
            if (usedTime < existing) { PlayerPrefs.SetFloat(recordKey, usedTime); PlayerPrefs.Save(); }
        }
    }

    private void UnlockNextLevel(int stars)
    {
        if (LevelSelectManager.SelectedLevel == null) return;
        if (LevelProgressManager.Instance == null) return;

        int currentLevelID = LevelSelectManager.SelectedLevel.levelID;
        int categoryID = LevelSelectManager.SelectedCategoryID;
        int totalLevels = LevelSelectManager.SelectedCategoryLevelCount;
        bool isLastLevel = currentLevelID >= totalLevels - 1;

        FirebaseManager.Instance?.LogLevelComplete(categoryID, currentLevelID, stars);
        InAppReviewManager.Instance?.OnLevelCompleted();

        LevelProgressManager.Instance.UnlockLevel(categoryID, currentLevelID);

        if (!isLastLevel)
        {
            LevelProgressManager.Instance.UnlockLevel(categoryID, currentLevelID + 1);
        }
        else
        {
            int nextCategoryID = categoryID + 1;
            if (LevelSelectManager.Instance != null &&
                nextCategoryID < LevelSelectManager.TotalCategoryCount)
                LevelProgressManager.Instance.UnlockCategory(nextCategoryID);
        }

        if (stars == 1)
        {
            if (Random.value <= 0.20f)
            {
                PowerUpManager.Instance?.AddPowerUp(PowerUpType.TimeFreeze);
                NotifyWinPanel(PowerUpType.TimeFreeze);
            }
        }

        if (stars == 2)
        {
            float roll = Random.value;
            if (roll <= 0.50f)
            {
                PowerUpManager.Instance?.AddPowerUp(PowerUpType.TimeFreeze);
                NotifyWinPanel(PowerUpType.TimeFreeze);
            }
            else if (roll <= 0.70f)
            {
                PowerUpManager.Instance?.AddPowerUp(PowerUpType.XRay);
                NotifyWinPanel(PowerUpType.XRay);
            }
        }

        if (stars == 3)
        {
            float roll = Random.value;
            PowerUpType reward =
                roll <= 0.20f ? PowerUpType.TimeFreeze :
                roll <= 0.55f ? PowerUpType.XRay :
                roll <= 0.75f ? PowerUpType.MindFreeze :
                PowerUpType.TimeFreeze;

            if (roll <= 0.75f)
            {
                PowerUpManager.Instance?.AddPowerUp(reward);
                NotifyWinPanel(reward);
            }
        }
    }

    // ─── LOSE ─────────────────────────────────────────────────────

    private void BeginLose()
    {
        SetState(GameState.Lose);
        Time.timeScale = 0f;
        GameEvents.RaiseGameLost();

        if (!DailyChallengeManager.IsDailyChallengeActive && !FreeModeManager.IsFreeModeActive)
            FirebaseManager.Instance?.LogLevelFail(
                LevelSelectManager.SelectedCategoryID,
                LevelSelectManager.SelectedLevel?.levelID ?? 0);

        LogLevelEndEvent(success: false, stars: 0);

        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            DailyChallengeManager.Instance?.AddAttempt();
            bool hasAttempts = DailyChallengeManager.Instance.HasAttemptsLeft();

            AdRewardPopup.Instance?.ShowForSecondChance(
                onSuccess: () => ApplySecondChance(),
                onDecline: () =>
                {
                    if (!hasAttempts) DailyChallengeManager.SetInactive();
                    ShowDCGameOverPanel();
                }
            );
            return;
        }

        AdRewardPopup.Instance?.ShowForSecondChance(
            onSuccess: () => ApplySecondChance(),
            onDecline: () => ShowGameOverPanel()
        );
    }

    private void ApplySecondChance()
    {
        StartCoroutine(ApplySecondChanceCoroutine());
    }

    private IEnumerator ApplySecondChanceCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        SetState(GameState.Playing);
        Time.timeScale = 1f;

        // Tahtada eşleşecek çift kalmadıysa süre/hamle eklemeden doğrudan kazan.
        if (GridManager.Instance != null && GridManager.Instance.RemainingPairs == 0)
        {
            EndGame(GameResult.Win);
            yield break;
        }

        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
        if (isMoveMode)
            MoveController.Instance?.AddMoves(10);
        else
        {
            TimerController.Instance?.ResumeTimer();
            TimerController.Instance?.AddTime(30f);
        }
    }

    private void ShowGameOverPanel()
    {
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            UpdateGameOverReason();
            UpdateTilesRemaining();
            UpdateTotalPoints();
        }
    }

    private void ShowDCGameOverPanel()
    {
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            UpdateGameOverReason();
            UpdateTilesRemaining();
            UpdateTotalPoints();

            var buttons = panelGameOver.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
                if (btn.gameObject.name == "Btn_Retry")
                    btn.gameObject.SetActive(false);
        }
    }

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    private void UpdateTilesRemaining()
    {
        if (panelGameOver == null) return;

        Transform tilesT = panelGameOver.transform
            .Find("Panel_LoseContent/Panel_LoseStatCard/Panel_TilesRemaining/Text_TilesValue");

        if (tilesT != null)
        {
            TMPro.TextMeshProUGUI tilesTxt = tilesT.GetComponent<TMPro.TextMeshProUGUI>();
            if (tilesTxt != null)
            {
                int remaining = GridManager.Instance != null
                    ? GridManager.Instance.RemainingPairs * 2 : 0;
                tilesTxt.text = remaining.ToString("D2");
            }
        }
    }

    private void UpdateTotalPoints()
    {
        if (panelGameOver == null) return;

        Transform labelT = panelGameOver.transform
            .Find("Panel_LoseContent/Panel_LoseStatCard/Panel_TotalPoints/TextPointsLabel");
        Transform valueT = panelGameOver.transform
            .Find("Panel_LoseContent/Panel_LoseStatCard/Panel_TotalPoints/Text_PointsValue");

        if (labelT != null)
        {
            TMPro.TextMeshProUGUI label = labelT.GetComponent<TMPro.TextMeshProUGUI>();
            if (label != null)
                label.text = LocalizationManager.Get("completion_label");
        }

        if (valueT != null)
        {
            TMPro.TextMeshProUGUI value = valueT.GetComponent<TMPro.TextMeshProUGUI>();
            if (value != null)
            {
                int matched = GridManager.Instance != null ? GridManager.Instance.MatchedPairCount : 0;
                int total = GridManager.Instance != null ? GridManager.Instance.TotalPairCount : 1;
                int percent = Mathf.RoundToInt((float)matched / total * 100f);
                value.text = $"%{percent}";
            }
        }
    }

    private void UpdateGameOverReason()
    {
        if (panelGameOver == null) return;

        Transform reasonT = panelGameOver.transform.Find("Panel_LoseContent/Text_Reason");
        if (reasonT == null) return;

        TMPro.TextMeshProUGUI reason = reasonT.GetComponent<TMPro.TextMeshProUGUI>();
        if (reason == null) return;

        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
        reason.text = isMoveMode
            ? LocalizationManager.Get("out_of_moves")
            : LocalizationManager.Get("out_of_time");
    }

    public bool IsPlaying => CurrentState == GameState.Playing;

    private void LogLevelEndEvent(bool success, int stars)
    {
        if (FreeModeManager.IsFreeModeActive) return;

        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel?.levelID ?? 0;
        string category = LevelSelectManager.SelectedCategory?.categoryName ?? "unknown";

        string gameMode = DailyChallengeManager.IsDailyChallengeActive
                            ? "daily_challenge"
                            : LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move
                                ? "move"
                                : "classic";

        int columns = DailyChallengeManager.IsDailyChallengeActive
                            ? DailyChallengeManager.DCColumns
                            : LevelSelectManager.SelectedLevel?.columns ?? 0;

        int rows = DailyChallengeManager.IsDailyChallengeActive
                            ? DailyChallengeManager.DCRows
                            : LevelSelectManager.SelectedLevel?.rows ?? 0;

        float timeLeft = TimerController.Instance != null
                            ? TimerController.Instance.RemainingTime : 0f;

        int movesLeft = MoveController.Instance != null
                            ? MoveController.Instance.MoveLimit - MoveController.Instance.MoveCount : 0;

        int timeThiefHits = GridManager.Instance != null
                            ? GridManager.Instance.TimeThiefHitCount : 0;

        FirebaseManager.Instance?.LogLevelEnd(
            categoryID, category, levelID, success, gameMode,
            columns, rows, stars, timeLeft, movesLeft, timeThiefHits);
    }
    private void SetState(GameState newState) => CurrentState = newState;

    public void LoadMainMenu()
    {
        if (isLeavingScene) return;
        isLeavingScene = true;

        if (DailyChallengeManager.IsDailyChallengeActive)
            DailyChallengeManager.SetInactive();

        FreeModeManager.ResetFreeModeState();
        GameSession.End();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        // Kazanma kesinleşti ama sonuç paneli henüz açılmadı: yeniden başlatma yok sayılır.
        if (CurrentState == GameState.Win && !winResultShown) return;

        if (isLeavingScene) return;
        isLeavingScene = true;

        if (DailyChallengeManager.IsDailyChallengeActive)
        {
            if (!DailyChallengeManager.Instance.HasAttemptsLeft())
            {
                DailyChallengeManager.SetInactive();
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
                return;
            }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextOrRetryWin()
    {
        if (isLeavingScene) return;
        isLeavingScene = true;

        Time.timeScale = 1f;

        AdManager.Instance?.ShowRewardedInterstitialIfReady(
            onSuccess: () =>
            {
                PowerUpManager.Instance?.AddPowerUp(PowerUpType.TimeFreeze);
                AdManager.Instance?.ShowInterstitialIfReady(() =>
                    SceneManager.LoadScene("LevelSelect"));
            },
            onSkipped: () =>
            {
                AdManager.Instance?.ShowInterstitialIfReady(() =>
                    SceneManager.LoadScene("LevelSelect"));
            }
        );
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

public enum GameResult
{
    Win,
    Lose
}