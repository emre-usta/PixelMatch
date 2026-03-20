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

    // ─── FREEZE ───────────────────────────────────────────────────
    private int frozenMoves = 0; // Kaç hamle daha ücretsiz

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
        UpdateUI(); // "Moves: 0" yerine UpdateUI() çağır — moveLimit'i gösterir
    }

    public void ActivateFreezeForMoves(int freeMovesCount)
    {
        frozenMoves = freeMovesCount;
        Debug.Log($"[MoveController] {freeMovesCount} hamle ücretsiz!");
    }

    private void HandlePairResult(CardController a, CardController b)
    {
        if (frozenMoves > 0)
        {
            frozenMoves--;
            Debug.Log($"[MoveController] Ücretsiz hamle kullanıldı. Kalan: {frozenMoves}");
            UpdateUI();
            return; // Hamle sayılmaz
        }

        moveCount++;
        UpdateUI();
        GameEvents.RaiseMoveUsed(useMoveLimit ? moveLimit - moveCount : moveCount);

        if (useMoveLimit && moveCount >= moveLimit)
            GameStateManager.Instance.LoseGame();
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

    //--------------------------------------------------------------
    public void StealMoves(int amount)
    {
        moveCount += amount;
        UpdateUI();
        GameEvents.RaiseMoveUsed(useMoveLimit ? moveLimit - moveCount : moveCount);
        Debug.Log($"[MoveController] Hamle Hırsızı: +{amount} hamle eklendi. Toplam: {moveCount}");

        if (useMoveLimit && moveCount >= moveLimit)
            GameStateManager.Instance.LoseGame();
    }

    // ─── PUBLIC ERİŞİM ────────────────────────────────────────────

    public int MoveCount => moveCount;
    public bool UseMoveLimit => useMoveLimit;
    public int RemainingMoves => moveLimit - moveCount;
}