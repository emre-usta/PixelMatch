using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PixelMatch — Grid Yöneticisi
/// Kartları üretir, yerleştirir ve eşleşme kontrolünü yapar.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    // ─── INSPECTOR AYARLARI ───────────────────────────────────────

    [Header("Grid Ayarları")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 6;

    [Header("Referanslar")]
    [SerializeField] private GameObject cardPrefab;       // Kart prefab'ı
    [SerializeField] private Transform gridContainer;     // Kartların parent'ı
    [SerializeField] private Sprite cardBackSprite;       // Tüm kartların arka yüzü

    [Header("Kart Sprite'ları")]
    [SerializeField] private Sprite[] cardSprites;        // Ön yüz sprite'ları (min. 18 adet)

    [Header("Level Konfigürasyonu")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Serbest Mod")]
    [SerializeField] private FreeModeLimitConfig freeModeLimitConfig;

    // ─── RUNTIME VERİSİ ───────────────────────────────────────────

    private List<CardController> allCards = new List<CardController>();
    private CardController firstSelectedCard = null;
    private CardController secondSelectedCard = null;
    private bool isChecking = false;      // Eşleşme kontrolü yapılıyor mu?

    private int matchedPairCount = 0;
    private int totalPairCount = 0;

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
        if (cat != null && cat.levels.Length > 0)
        {
            LevelConfig biggestLevel = cat.levels[cat.levels.Length - 1];
            cardSprites = biggestLevel.cardSprites;
            cardBackSprite = biggestLevel.cardBackSprite;
        }

        if (freeModeLimitConfig != null)
        {
            var limit = freeModeLimitConfig.GetLimit(
                columns, rows, DailyChallengeManager.DCDifficulty);

            TimerController.Instance?.SetConfig(true, limit.timeLimit);
            MoveController.Instance?.SetConfig(false, limit.moveLimit);
        }

        levelConfig = null;
    }

    private void SetupFreeModeConfig()
    {
        columns = FreeModeManager.SelectedColumns;
        rows = FreeModeManager.SelectedRows;

        // Kategori sprite'larını al
        CategoryConfig cat = FreeModeManager.SelectedCategory;
        if (cat != null && cat.levels.Length > 0)
        {
            // En büyük levelin sprite'larını kullan (en fazla sprite içeriyor)
            LevelConfig biggestLevel = cat.levels[cat.levels.Length - 1];
            cardSprites = biggestLevel.cardSprites;
            cardBackSprite = biggestLevel.cardBackSprite;
        }

        // Limit config'den süre/hamle al
        bool isMoveMode = FreeModeManager.SelectedMode == LevelSelectManager.GameMode.Move;
        if (freeModeLimitConfig != null)
        {
            var limit = freeModeLimitConfig.GetLimit(
                columns, rows, FreeModeManager.SelectedDifficulty);

            if (TimerController.Instance != null)
                TimerController.Instance.SetConfig(!isMoveMode, limit.timeLimit);
            if (MoveController.Instance != null)
                MoveController.Instance.SetConfig(isMoveMode, limit.moveLimit);
        }

        // LevelConfig null bırak — GenerateGrid() içinde levelConfig kontrolü var
        levelConfig = null;
    }

    // ─── GRİD ÜRETİMİ ─────────────────────────────────────────────

    private void GenerateGrid()
    {
        if (levelConfig != null)
        {
            columns = levelConfig.columns;
            rows = levelConfig.rows;
            cardBackSprite = levelConfig.cardBackSprite;
            if (levelConfig.cardSprites != null && levelConfig.cardSprites.Length > 0)
                cardSprites = levelConfig.cardSprites;

            // Mod'u levelConfig'ten değil, SelectedMode'dan oku
            // levelConfig asset'ine hiç dokunma
            bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

            if (TimerController.Instance != null)
                TimerController.Instance.SetConfig(!isMoveMode, levelConfig.timeLimit);
            if (MoveController.Instance != null)
                MoveController.Instance.SetConfig(isMoveMode, levelConfig.moveLimit);
        }

        int totalCards = columns * rows;           // 36
        totalPairCount = totalCards / 2;           // 18 çift

        // Sprite listesi yeterli mi kontrol et
        if (cardSprites == null || cardSprites.Length < totalPairCount)
        {
            Debug.LogError($"[GridManager] En az {totalPairCount} sprite gerekli! Mevcut: {cardSprites?.Length ?? 0}");
            return;
        }

        // Her sprite'tan 2 tane — 18 çift = 36 kart
        List<int> cardIDs = new List<int>();
        for (int i = 0; i < totalPairCount; i++)
        {
            cardIDs.Add(i);
            cardIDs.Add(i);
        }

        // Fisher-Yates shuffle — gerçek rastgele karıştırma
        Shuffle(cardIDs);

        // Kartları üret ve yerleştir
        // Kartları üret ve yerleştir
        AdjustGridLayout();

        int timeThiefIndex1 = -1;
        int timeThiefIndex2 = -1;
        if (levelConfig != null && levelConfig.difficulty == DifficultyLevel.Hard)
        {
            // Aynı çiftten 2 kart seç
            int timeThiefPairID = Random.Range(2, totalPairCount); // ilk 2 çifti atla

            List<int> pairIndices = new List<int>();
            for (int i = 0; i < cardIDs.Count; i++)
            {
                if (cardIDs[i] == timeThiefPairID)
                    pairIndices.Add(i);
            }

            if (pairIndices.Count == 2)
            {
                timeThiefIndex1 = pairIndices[0];
                timeThiefIndex2 = pairIndices[1];
                Debug.Log($"[GridManager] Zaman Hırsızı: PairID={timeThiefPairID}, index={timeThiefIndex1},{timeThiefIndex2}");
            }
        }

        for (int i = 0; i < totalCards; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CardController card = cardObj.GetComponent<CardController>();

            if (card == null)
            {
                Debug.LogError("[GridManager] Card prefab'ında CardController componenti yok!");
                continue;
            }

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

        // Grid hazır — önce peek, sonra oyunu başlat
        StartCoroutine(PeekAndStart());

        Debug.Log($"[GridManager] Grid hazır: {columns}x{rows}, {totalPairCount} çift");
    }

    private void AdjustGridLayout()
    {
        GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
        if (layout == null) return;

        // Grid container'ın boyutunu al
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        // Spacing sabit
        float spacingX = 5f;
        float spacingY = 5f;

        // Her kart için uygun boyutu hesapla
        float cellWidth = (containerWidth - (spacingX * (columns + 1))) / columns;
        float cellHeight = (containerHeight - (spacingY * (rows + 1))) / rows;

        // Kare kart — küçük olanı al
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        layout.cellSize = new Vector2(cellSize, cellSize);
        layout.spacing = new Vector2(spacingX, spacingY);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
    }

    // ─── KART SEÇİMİ & EŞLEŞME KONTROLÜ ─────────────────────────

    private void HandleCardRevealed(CardController card)
    {
        // Kontrol yapılıyorsa yeni kart kabul etme
        if (isChecking) return;

        if (firstSelectedCard == null)
        {
            // İlk kart seçildi
            firstSelectedCard = card;
        }
        else if (secondSelectedCard == null && card != firstSelectedCard)
        {
            // İkinci kart seçildi
            secondSelectedCard = card;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        isChecking = true;
        SetAllCardsInteractable(false);

        yield return new WaitForSeconds(0.8f);

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
                // Zaman Hırsızı varsa normal -1 hamle yerine sadece -3 ceza
                GameEvents.RaiseEffectTriggered(CardEffectType.TimeThief);
                // Mismatch event'i tetikleme — animasyon için ayrıca çağır
                StartCoroutine(MismatchSequence(firstSelectedCard, secondSelectedCard));
            }
            else
            {
                // Normal yanlış eşleşme — -1 hamle
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

        Debug.Log($"[GridManager] Eşleşme! {matchedPairCount}/{totalPairCount}");

        // Tüm çiftler eşleşti mi?
        if (matchedPairCount >= totalPairCount)
        {
            GameStateManager.Instance.WinGame();
        }
    }

    private void HandlePairMismatch(CardController a, CardController b)
    {
        StartCoroutine(MismatchSequence(a, b));
    }

    private IEnumerator MismatchSequence(CardController a, CardController b)
    {
        // Önce shake
        StartCoroutine(a.ShakeAnimation());
        StartCoroutine(b.ShakeAnimation());

        yield return new WaitForSeconds(0.3f);

        // Sonra kapat
        a.Hide();
        b.Hide();
    }

    // ─── YARDIMCI METOTLAR ────────────────────────────────────────

    /// <summary>Tüm kartların input'unu aç/kapat.</summary>
    public void SetAllCardsInteractable(bool interactable)
    {
        foreach (var card in allCards)
        {
            if (card.State != CardState.Matched)
                card.SetInteractable(interactable);
        }
    }

    /// <summary>Fisher-Yates shuffle algoritması.</summary>
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
        // Event tetiklemeden sessizce aç
        foreach (var card in allCards)
            card.RevealSilent();

        yield return new WaitForSeconds(1.5f);

        foreach (var card in allCards)
            card.Hide();

        yield return new WaitForSeconds(0.3f);

        GameStateManager.Instance.OnGridReady();
    }
}