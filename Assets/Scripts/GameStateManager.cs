using System.Collections;
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
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Debug.unityLogger.logEnabled = false;
#endif
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
        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.gameObject.activeInHierarchy &&
            PlayerPrefs.GetInt("tutorial_done", 0) == 0)
            return;

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

        float delay = ComboEffectManager.Instance != null ? 1.2f : 0f;
        StartCoroutine(ShowWinPanelDelayed(delay));
    }

    private IEnumerator ShowWinPanelDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (FreeModeManager.IsFreeModeActive)
        {
            if (panelWin != null)
            {
                panelWin.SetActive(true);
                int stars = CalculateStars();
                WinPanelController winPanel = panelWin.GetComponentInChildren<WinPanelController>();
                winPanel?.ShowFreeModeResult(stars);
            }
            yield break;
        }

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
            yield break;
        }

        int starsNormal = CalculateStars();
        SaveStars(starsNormal);
        SaveRecord(starsNormal);
        UnlockNextLevel(starsNormal);

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
        if (PowerUpManager.Instance == null) return;
        PowerUpManager.Instance.AddPowerUp(PowerUpType.MindFreeze);
        PowerUpManager.Instance.AddPowerUp(PowerUpType.TimeFreeze);
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

        if (stars == 2)
        {
            PowerUpManager.Instance?.AddPowerUp(PowerUpType.XRay);
            NotifyWinPanel(PowerUpType.XRay);
        }

        if (stars == 3)
        {
            PowerUpType reward = Random.value > 0.5f ? PowerUpType.MindFreeze : PowerUpType.TimeFreeze;
            PowerUpManager.Instance?.AddPowerUp(reward);
            NotifyWinPanel(reward);
        }
    }

    // ─── LOSE ─────────────────────────────────────────────────────

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

            if (panelGameOver != null)
            {
                panelGameOver.SetActive(true);
                UpdateGameOverReason();
                UpdateTilesRemaining();
                UpdateTotalPoints();

                var buttons = panelGameOver.GetComponentsInChildren<UnityEngine.UI.Button>();
                foreach (var btn in buttons)
                {
                    if (btn.gameObject.name == "Btn_Retry")
                    {
                        btn.interactable = hasAttempts;
                        TMPro.TextMeshProUGUI txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        if (txt != null)
                            txt.text = hasAttempts
                                ? LocalizationManager.Get("btn_retry")
                                : LocalizationManager.Get("dc_no_attempts");
                    }
                }
            }
            return;
        }

        // İkinci şans popup
        AdRewardPopup.Instance?.ShowForSecondChance(
            onSuccess: () => ApplySecondChance(),
            onDecline: () => ShowGameOverPanel()
        );
    }

    private void ApplySecondChance()
    {
        SetState(GameState.Playing);
        Time.timeScale = 1f;

        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
        if (isMoveMode)
            MoveController.Instance?.AddMoves(10);
        else
            TimerController.Instance?.AddTime(30f);
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

    private void SetState(GameState newState) => CurrentState = newState;

    public void LoadMainMenu()
    {
        if (DailyChallengeManager.IsDailyChallengeActive)
            DailyChallengeManager.SetInactive();

        FreeModeManager.ResetFreeModeState();
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
                return;
            }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextOrRetryWin()
    {
        Time.timeScale = 1f;
        AdManager.Instance?.ShowInterstitialIfReady(() =>
        {
            SceneManager.LoadScene("LevelSelect");
        });
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