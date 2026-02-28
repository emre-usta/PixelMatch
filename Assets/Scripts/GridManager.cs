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
        if (LevelSelectManager.SelectedLevel != null)
            levelConfig = LevelSelectManager.SelectedLevel;

        GenerateGrid();
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
        AdjustGridLayout();

        for (int i = 0; i < totalCards; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CardController card = cardObj.GetComponent<CardController>();

            if (card == null)
            {
                Debug.LogError("[GridManager] Card prefab'ında CardController componenti yok!");
                continue;
            }

            int id = cardIDs[i];
            card.Setup(id, cardSprites[id], cardBackSprite);
            card.SetInteractable(true);
            allCards.Add(card);
        }

        // Grid hazır — GameStateManager'a bildir
        GameStateManager.Instance.OnGridReady();

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
            // ✅ Eşleşti
            GameEvents.RaisePairMatched(firstSelectedCard, secondSelectedCard);
        }
        else
        {
            // ❌ Eşleşmedi
            GameEvents.RaisePairMismatch(firstSelectedCard, secondSelectedCard);
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
    public List<CardController> AllCards => allCards;
}