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
        if (panelWin != null) panelWin.SetActive(true);
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