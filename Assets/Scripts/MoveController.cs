using UnityEngine;
using TMPro;

public class MoveController : MonoBehaviour
{
    public static MoveController Instance { get; private set; }

    [Header("Hamle Modu Ayarları")]
    [SerializeField] private bool useMoveLimit = false;
    [SerializeField] private int moveLimit = 30;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moveText;

    private int moveCount = 0;
    private int frozenMoves = 0;

    public int MoveLimit => moveLimit;
    public int MoveCount => moveCount;
    public bool UseMoveLimit => useMoveLimit;
    public int RemainingMoves => moveLimit - moveCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        // Önizleme/tutorial sırasında göstergede seviyenin başlangıç değeri görünsün.
        UpdateUI();
    }

    private void HandleGameStarted()
    {
        moveCount = 0;
        UpdateUI();
    }

    public void ActivateFreezeForMoves(int freeMovesCount)
    {
        frozenMoves = freeMovesCount;
    }

    // Oyun bitince kalan bedava hamleler iptal edilir (second chance sonrası geri gelmez).
    public void CancelFreeze()
    {
        frozenMoves = 0;
    }

    private void HandlePairResult(CardController a, CardController b)
    {
        if (frozenMoves > 0)
        {
            frozenMoves--;
            UpdateUI();

            if (frozenMoves == 0)
                PowerUpUI.Instance?.HideFreezeFeedback();

            return;
        }

        moveCount++;
        UpdateUI();
        GameEvents.RaiseMoveUsed(useMoveLimit ? moveLimit - moveCount : moveCount);
        if (useMoveLimit && moveCount >= moveLimit)
            GameStateManager.Instance.LoseGame();
    }

    private void UpdateUI()
    {
        if (moveText == null) return;
        moveText.text = useMoveLimit
            ? $"{moveLimit - moveCount}"
            : $"{moveCount}";
    }

    public void StealMoves(int amount)
    {
        moveCount += amount;
        UpdateUI();
        GameEvents.RaiseMoveUsed(useMoveLimit ? moveLimit - moveCount : moveCount);

        if (useMoveLimit && moveCount >= moveLimit)
            GameStateManager.Instance.LoseGame();
    }

    public void AddMoves(int amount)
    {
        if (!useMoveLimit) return;
        moveLimit += amount;
        UpdateUI();
        GameEvents.RaiseMoveUsed(moveLimit - moveCount);
    }
}