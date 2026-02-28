using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// PixelMatch — Timer Kontrolcüsü
/// Süreyi yönetir, event sistemi üzerinden herkese bildirir.
/// Update() yerine Coroutine kullanır — performans için.
/// </summary>
public class TimerController : MonoBehaviour
{
    public static TimerController Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Süre Ayarları")]
    [SerializeField] private float totalTime = 120f;   // Saniye cinsinden

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText; // Text_Timer objesi

    // ─── RUNTIME VERİSİ ───────────────────────────────────────────

    private float remainingTime;
    private bool isRunning = false;
    private Coroutine timerCoroutine;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

    private void HandleGameStarted()
    {
        StartTimer();
    }

    private void HandleGamePaused()
    {
        PauseTimer();
    }

    private void HandleGameResumed()
    {
        ResumeTimer();
    }

    private void HandleGameEnded()
    {
        StopTimer();
    }

    // ─── TIMER KONTROLÜ ───────────────────────────────────────────

    public void StartTimer()
    {
        remainingTime = totalTime;
        isRunning = true;
        UpdateTimerUI();

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
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
        if (!useTimer)
        {
            StopTimer();
            return;
        }
        totalTime = timeLimit;
    }

    /// <summary>
    /// Süreye delta ekler — pozitif = süre artar, negatif = azalır.
    /// Zaman Hırsızı kartı için: AddTime(-5)
    /// Zaman Donması power-up için: AddTime(+5)
    /// </summary>
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

            remainingTime -= 1f;
            remainingTime = Mathf.Max(0, remainingTime);

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

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ─── PUBLIC ERİŞİM ────────────────────────────────────────────

    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;
}