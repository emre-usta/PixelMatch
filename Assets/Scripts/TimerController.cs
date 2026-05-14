using System.Collections;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public static TimerController Instance { get; private set; }

    [Header("Süre Ayarları")]
    [SerializeField] private float totalTime = 120f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    private float remainingTime;
    private bool isRunning = false;
    private bool useTimer = true;
    private Coroutine timerCoroutine;

    public float TotalTime => totalTime;
    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStarted += HandleGameStarted;
        GameEvents.OnGamePaused += HandleGamePaused;
        GameEvents.OnGameResumed += HandleGameResumed;
        GameEvents.OnGameWon += HandleGameEnded;
        GameEvents.OnGameLost += HandleGameEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStarted -= HandleGameStarted;
        GameEvents.OnGamePaused -= HandleGamePaused;
        GameEvents.OnGameResumed -= HandleGameResumed;
        GameEvents.OnGameWon -= HandleGameEnded;
        GameEvents.OnGameLost -= HandleGameEnded;
    }

    // ─── EVENT HANDLERS ───────────────────────────────────────────

    private void HandleGameStarted() { if (useTimer) StartTimer(); }
    private void HandleGamePaused() => PauseTimer();
    private void HandleGameResumed() => ResumeTimer();
    private void HandleGameEnded() => StopTimer();

    // ─── TIMER KONTROLÜ ───────────────────────────────────────────

    public void StartTimer()
    {
        remainingTime = totalTime;
        isRunning = true;
        UpdateTimerUI();

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;

    public void StopTimer()
    {
        isRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    public void SetConfig(bool useTimer, float timeLimit)
    {
        this.useTimer = useTimer;
        if (!useTimer)
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            return;
        }
        totalTime = timeLimit;
    }

    public void AddTime(float delta)
    {
        remainingTime = Mathf.Max(0, remainingTime + delta);
        UpdateTimerUI();
        GameEvents.RaiseTimeChanged(remainingTime);
    }

    // ─── COROUTINE ────────────────────────────────────────────────

    private IEnumerator TimerCoroutine()
    {
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);

            if (!isRunning) continue;

            remainingTime = Mathf.Max(0, remainingTime - 1f);
            UpdateTimerUI();
            GameEvents.RaiseTimeChanged(remainingTime);

            if (remainingTime <= 0)
            {
                GameEvents.RaiseTimeUp();
                GameStateManager.Instance.LoseGame();
                yield break;
            }
        }
    }

    // ─── UI ───────────────────────────────────────────────────────

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}