using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Ayarları")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 6;

    [Header("Referanslar")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Sprite cardBackSprite;

    [Header("Kart Sprite'ları")]
    [SerializeField] private Sprite[] cardSprites;

    [Header("Level Konfigürasyonu")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Serbest Mod")]
    [SerializeField] private FreeModeLimitConfig freeModeLimitConfig;

    private List<CardController> allCards = new List<CardController>();
    private CardController firstSelectedCard = null;
    private CardController secondSelectedCard = null;
    private bool isChecking = false;

    private int matchedPairCount = 0;
    private int totalPairCount = 0;
    private int timeThiefHitCount = 0;

    public CardController FirstSelectedCard => firstSelectedCard;
    public int TimeThiefHitCount => timeThiefHitCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnCardRevealed += HandleCardRevealed;
        GameEvents.OnPairMatched += HandlePairMatched;
        GameEvents.OnPairMismatch += HandlePairMismatch;
    }

    private void OnDisable()
    {
        GameEvents.OnCardRevealed -= HandleCardRevealed;
        GameEvents.OnPairMatched -= HandlePairMatched;
        GameEvents.OnPairMismatch -= HandlePairMismatch;
    }

    private void Start()
    {
        if (DailyChallengeManager.IsDailyChallengeActive)
            SetupDailyChallengeConfig();
        else if (FreeModeManager.IsFreeModeActive)
            SetupFreeModeConfig();
        else if (LevelSelectManager.SelectedLevel != null)
            levelConfig = LevelSelectManager.SelectedLevel;

        GenerateGrid();
    }

    private void SetupDailyChallengeConfig()
    {
        columns = DailyChallengeManager.DCColumns;
        rows = DailyChallengeManager.DCRows;

        CategoryConfig cat = DailyChallengeManager.DCCategory;
        if (cat != null)
        {
            cardSprites = cat.cardSprites;
            cardBackSprite = cat.cardBackSprite;
        }

        if (freeModeLimitConfig != null)
        {
            var limit = freeModeLimitConfig.GetLimit(columns, rows, DailyChallengeManager.DCDifficulty);
            TimerController.Instance?.SetConfig(true, limit.timeLimit);
            MoveController.Instance?.SetConfig(false, limit.moveLimit);
        }

        levelConfig = null;
    }

    private void SetupFreeModeConfig()
    {
        columns = FreeModeManager.SelectedColumns;
        rows = FreeModeManager.SelectedRows;

        CategoryConfig cat = FreeModeManager.SelectedCategory;
        if (cat != null)
        {
            cardSprites = cat.cardSprites;
            cardBackSprite = cat.cardBackSprite;
        }

        bool isMoveMode = FreeModeManager.SelectedMode == LevelSelectManager.GameMode.Move;
        if (freeModeLimitConfig != null)
        {
            var limit = freeModeLimitConfig.GetLimit(columns, rows, FreeModeManager.SelectedDifficulty);
            TimerController.Instance?.SetConfig(!isMoveMode, limit.timeLimit);
            MoveController.Instance?.SetConfig(isMoveMode, limit.moveLimit);
        }

        levelConfig = null;
    }

    // ─── GRİD ÜRETİMİ ─────────────────────────────────────────────

    private void GenerateGrid()
    {
        if (levelConfig != null)
        {
            columns = levelConfig.columns;
            rows = levelConfig.rows;

            CategoryConfig cat = LevelSelectManager.SelectedCategory;
            if (cat != null)
            {
                cardSprites = cat.cardSprites;
                cardBackSprite = cat.cardBackSprite;
            }

            bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
            TimerController.Instance?.SetConfig(!isMoveMode, levelConfig.timeLimit);
            MoveController.Instance?.SetConfig(isMoveMode, levelConfig.moveLimit);
        }

        int totalCards = columns * rows;
        totalPairCount = totalCards / 2;

        if (cardSprites == null || cardSprites.Length < totalPairCount)
        {
            Debug.LogError($"[GridManager] En az {totalPairCount} sprite gerekli! Mevcut: {cardSprites?.Length ?? 0}");
            return;
        }

        List<int> availableIDs = new List<int>();
        for (int i = 0; i < cardSprites.Length; i++)
            availableIDs.Add(i);

        Shuffle(availableIDs);

        List<int> selectedIDs = availableIDs.GetRange(0, totalPairCount);

        List<int> cardIDs = new List<int>();
        foreach (int id in selectedIDs)
        {
            cardIDs.Add(id);
            cardIDs.Add(id);
        }

        Shuffle(cardIDs);

        int timeThiefIndex1 = -1;
        int timeThiefIndex2 = -1;

        if (levelConfig != null && levelConfig.difficulty == DifficultyLevel.Hard)
        {
            int timeThiefPairID = Random.Range(2, totalPairCount);
            List<int> pairIndices = new List<int>();

            for (int i = 0; i < cardIDs.Count; i++)
                if (cardIDs[i] == timeThiefPairID)
                    pairIndices.Add(i);

            if (pairIndices.Count == 2)
            {
                timeThiefIndex1 = pairIndices[0];
                timeThiefIndex2 = pairIndices[1];
            }
        }

        StartCoroutine(GenerateGridDelayed(cardIDs, timeThiefIndex1, timeThiefIndex2, totalCards));
    }

    private IEnumerator GenerateGridDelayed(List<int> cardIDs, int timeThiefIndex1, int timeThiefIndex2, int totalCards)
    {
        yield return null;

        AdjustGridLayout();

        for (int i = 0; i < totalCards; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CardController card = cardObj.GetComponent<CardController>();

            if (card == null) continue;

            int timeThiefID = totalPairCount + 99;
            int id = cardIDs[i];
            CardEffectType effectType = CardEffectType.None;
            Sprite frontSprite = cardSprites[id];

            if (i == timeThiefIndex1 || i == timeThiefIndex2)
            {
                effectType = CardEffectType.TimeThief;
                id = timeThiefID;
                if (levelConfig.timeThiefSprite != null)
                    frontSprite = levelConfig.timeThiefSprite;
            }

            card.Setup(id, frontSprite, cardBackSprite, effectType);
            card.SetInteractable(true);
            allCards.Add(card);
        }

        StartCoroutine(PeekAndStart());
    }

    private void AdjustGridLayout()
    {
        GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) return;

        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        float spacingX = 5f;
        float spacingY = 5f;

        float cellWidth = (containerWidth - (spacingX * (columns + 1))) / columns;
        float cellHeight = (containerHeight - (spacingY * (rows + 1))) / rows;

        layout.cellSize = new Vector2(cellWidth, cellHeight);
        layout.spacing = new Vector2(spacingX, spacingY);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.childAlignment = TextAnchor.MiddleCenter;
    }

    // ─── KART SEÇİMİ & EŞLEŞME KONTROLÜ ─────────────────────────

    private void HandleCardRevealed(CardController card)
    {
        if (isChecking) return;

        if (firstSelectedCard == null)
        {
            firstSelectedCard = card;
        }
        else if (secondSelectedCard == null && card != firstSelectedCard)
        {
            secondSelectedCard = card;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        isChecking = true;
        SetAllCardsInteractable(false);

        yield return new WaitForSeconds(0.4f);

        if (firstSelectedCard.CardID == secondSelectedCard.CardID)
        {
            GameEvents.RaisePairMatched(firstSelectedCard, secondSelectedCard);
        }
        else
        {
            bool hasTimeThief = firstSelectedCard.EffectType == CardEffectType.TimeThief ||
                                secondSelectedCard.EffectType == CardEffectType.TimeThief;

            if (hasTimeThief)
            {
                timeThiefHitCount++;
                GameEvents.RaiseEffectTriggered(CardEffectType.TimeThief);
                StartCoroutine(MismatchSequence(firstSelectedCard, secondSelectedCard));
            }
            else
            {
                GameEvents.RaisePairMismatch(firstSelectedCard, secondSelectedCard);
            }
        }

        firstSelectedCard = null;
        secondSelectedCard = null;
        isChecking = false;

        SetAllCardsInteractable(true);
    }

    private void HandlePairMatched(CardController a, CardController b)
    {
        a.SetMatched();
        b.SetMatched();
        matchedPairCount++;

        if (matchedPairCount >= totalPairCount)
            GameStateManager.Instance.WinGame();
    }

    private void HandlePairMismatch(CardController a, CardController b)
    {
        StartCoroutine(MismatchSequence(a, b));
    }

    private IEnumerator MismatchSequence(CardController a, CardController b)
    {
        StartCoroutine(a.ShakeAnimation());
        StartCoroutine(b.ShakeAnimation());

        yield return new WaitForSeconds(0.3f);

        a.Hide();
        b.Hide();
    }

    // ─── YARDIMCI METOTLAR ────────────────────────────────────────

    public void SetAllCardsInteractable(bool interactable)
    {
        foreach (var card in allCards)
            if (card.State != CardState.Matched)
                card.SetInteractable(interactable);
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ─── PUBLIC ERİŞİM ────────────────────────────────────────────

    public int MatchedPairCount => matchedPairCount;
    public int TotalPairCount => totalPairCount;
    public int RemainingPairs => totalPairCount - matchedPairCount;
    public List<CardController> AllCards => allCards;

    private IEnumerator PeekAndStart()
    {
        foreach (var card in allCards)
            card.RevealSilent();

        yield return new WaitForSeconds(1.5f);

        foreach (var card in allCards)
            card.Hide();

        yield return new WaitForSeconds(0.3f);

        GameStateManager.Instance.OnGridReady();
    }

    public void ClearSelectedCard()
    {
        firstSelectedCard = null;
    }
}