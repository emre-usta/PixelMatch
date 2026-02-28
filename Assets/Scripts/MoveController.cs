using UnityEngine;
using TMPro;

/// <summary>
/// PixelMatch — Hamle Kontrolcüsü
/// Yapılan hamleleri sayar ve UI'a yansıtır.
/// İleride Hamle Modu için limit desteği de burada.
/// </summary>
public class MoveController : MonoBehaviour
{
    public static MoveController Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Hamle Modu Ayarları")]
    [SerializeField] private bool useMoveLimit = false;   // Klasik modda false
    [SerializeField] private int moveLimit = 30;          // Hamle modu için

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moveText;    // Opsiyonel — HUD'a eklenirse

    // ─── RUNTIME VERİSİ ───────────────────────────────────────────

    private int moveCount = 0;

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
        GameEvents.OnPairMatched += HandlePairResult;
        GameEvents.OnPairMismatch += HandlePairResult;
        GameEvents.OnGameStarted += HandleGameStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnPairMatched -= HandlePairResult;
        GameEvents.OnPairMismatch -= HandlePairResult;
        GameEvents.OnGameStarted -= HandleGameStarted;
    }

    public void SetConfig(bool useMoveLimit, int moveLimit)
    {
        this.useMoveLimit = useMoveLimit;
        this.moveLimit = moveLimit;
    }


    // ─── EVENT HANDLERS ───────────────────────────────────────────

    private void HandleGameStarted()
    {
        moveCount = 0;
        if (moveText != null)
            moveText.text = "Moves: 0";
    }

    private void HandlePairResult(CardController a, CardController b)
    {
        moveCount++;
        UpdateUI();
        GameEvents.RaiseMoveUsed(useMoveLimit ? moveLimit - moveCount : moveCount);

        // Hamle limiti doldu mu?
        if (useMoveLimit && moveCount >= moveLimit)
        {
            GameStateManager.Instance.LoseGame();
        }
    }

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    private void UpdateUI()
    {
        if (moveText == null) return;

        if (useMoveLimit)
            moveText.text = $"Moves: {moveLimit - moveCount}";
        else
            moveText.text = $"Moves: {moveCount}";
    }

    // ─── PUBLIC ERİŞİM ────────────────────────────────────────────

    public int MoveCount => moveCount;
    public bool UseMoveLimit => useMoveLimit;
    public int RemainingMoves => moveLimit - moveCount;
}