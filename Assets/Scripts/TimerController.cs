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
        GameEvents.OnGameWon += HandleGameEnded;
        GameEvents.OnGameLost += HandleGameLost;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStarted -= HandleGameStarted;
        GameEvents.OnGamePaused -= HandleGamePaused;
        GameEvents.OnGameWon -= HandleGameEnded;
        GameEvents.OnGameLost -= HandleGameLost;
    }

    private void HandleGameStarted() { if (useTimer) StartTimer(); }
    private void HandleGamePaused() => PauseTimer();
    private void HandleGameEnded() => StopTimer();
    private void HandleGameLost() => PauseTimer();

    public void StartTimer()
    {
        remainingTime = totalTime;
        isRunning = true;
        UpdateTimerUI();

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void PauseTimer() => isRunning = false;

    // TimeFreeze power-up'ı: Pause/Resume'dan bağımsız sayım durdurma (AUDIT 1.8).
    private bool isFrozen = false;
    public void Freeze() => isFrozen = true;
    public void Unfreeze() => isFrozen = false;

    public void ResumeTimer()
    {
        isRunning = true;
        if (timerCoroutine == null && remainingTime > 0)
            timerCoroutine = StartCoroutine(TimerCoroutine());
    }

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

        // Önizleme/tutorial sırasında göstergede seviyenin başlangıç süresi görünsün (timer henüz akmaz).
        UpdateTimerUI(totalTime);
    }

    public void AddTime(float delta)
    {
        remainingTime = Mathf.Max(0, remainingTime + delta);
        UpdateTimerUI();
        GameEvents.RaiseTimeChanged(remainingTime);

        // Süre dışarıdan (ör. Zaman Hırsızı) sıfırlandıysa, timer'ın durumundan bağımsız olarak hemen kaybettir.
        if (remainingTime <= 0 && useTimer &&
            GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying)
        {
            HandleTimeUp();
            return;
        }

        if (isRunning && timerCoroutine == null && remainingTime > 0)
            timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        while (remainingTime > 0)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (!isRunning || isFrozen) continue;
            if (Time.timeScale == 0f) continue;

            remainingTime = Mathf.Max(0, remainingTime - 1f);
            UpdateTimerUI();
            GameEvents.RaiseTimeChanged(remainingTime);

            if (remainingTime <= 0)
            {
                timerCoroutine = null;
                HandleTimeUp();
                yield break;
            }
        }

        timerCoroutine = null;
    }

    private void HandleTimeUp()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        isRunning = false;
        GameEvents.RaiseTimeUp();
        GameStateManager.Instance.LoseGame();
    }

    private void UpdateTimerUI() => UpdateTimerUI(remainingTime);

    private void UpdateTimerUI(float time)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}