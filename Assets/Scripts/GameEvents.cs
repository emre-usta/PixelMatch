using System;
using UnityEngine;

/// <summary>
/// PixelMatch — Merkezi Event Sistemi
/// Tüm oyun olayları buradan yayınlanır ve buraya abone olunur.
/// Yeni bir mekanik eklemek = card scriptini bozmak değil, buraya abone olmak.
/// </summary>
public static class GameEvents
{
    // ─── KART EVENTLERİ ───────────────────────────────────────────

    /// <summary>Bir kart açıldığında tetiklenir.</summary>
    public static event Action<CardController> OnCardRevealed;

    /// <summary>İki kart eşleştiğinde tetiklenir.</summary>
    public static event Action<CardController, CardController> OnPairMatched;

    /// <summary>İki kart eşleşmediğinde tetiklenir.</summary>
    public static event Action<CardController, CardController> OnPairMismatch;

    // ─── OYUN AKIŞI EVENTLERİ ─────────────────────────────────────

    /// <summary>Oyun başladığında tetiklenir.</summary>
    public static event Action OnGameStarted;

    /// <summary>Oyun kazanıldığında tetiklenir.</summary>
    public static event Action OnGameWon;

    /// <summary>Oyun kaybedildiğinde tetiklenir.</summary>
    public static event Action OnGameLost;

    /// <summary>Oyun pause yapıldığında tetiklenir.</summary>
    public static event Action OnGamePaused;

    /// <summary>Oyun devam ettirildiğinde tetiklenir.</summary>
    public static event Action OnGameResumed;

    // ─── SAYAÇ EVENTLERİ ──────────────────────────────────────────

    /// <summary>Hamle yapıldığında tetiklenir. (kalan hamle sayısı)</summary>
    public static event Action<int> OnMoveUsed;

    /// <summary>Timer değiştiğinde tetiklenir. (kalan süre saniye)</summary>
    public static event Action<float> OnTimeChanged;

    /// <summary>Timer bittiğinde tetiklenir.</summary>
    public static event Action OnTimeUp;

    // ─── EFEKT EVENTLERİ ──────────────────────────────────────────

    /// <summary>Özel kart efekti tetiklendiğinde. (efekt türü)</summary>
    public static event Action<CardEffectType> OnEffectTriggered;

    /// <summary>Grid müdahalesi gerçekleştiğinde. (müdahale türü)</summary>
    public static event Action<GridInterventionType> OnGridIntervention;

    /// <summary>Power-up kullanıldığında. (power-up türü)</summary>
    public static event Action<PowerUpType> OnPowerUpUsed;

    // ─── PUBLISHER METOTLARI ──────────────────────────────────────
    // "Raise" metotları — sadece ilgili manager'lar çağırır.

    public static void RaiseCardRevealed(CardController card)
        => OnCardRevealed?.Invoke(card);

    public static void RaisePairMatched(CardController a, CardController b)
        => OnPairMatched?.Invoke(a, b);

    public static void RaisePairMismatch(CardController a, CardController b)
        => OnPairMismatch?.Invoke(a, b);

    public static void RaiseGameStarted()
        => OnGameStarted?.Invoke();

    public static void RaiseGameWon()
        => OnGameWon?.Invoke();

    public static void RaiseGameLost()
        => OnGameLost?.Invoke();

    public static void RaiseGamePaused()
        => OnGamePaused?.Invoke();

    public static void RaiseGameResumed()
        => OnGameResumed?.Invoke();

    public static void RaiseMoveUsed(int remainingMoves)
        => OnMoveUsed?.Invoke(remainingMoves);

    public static void RaiseTimeChanged(float remainingTime)
        => OnTimeChanged?.Invoke(remainingTime);

    public static void RaiseTimeUp()
        => OnTimeUp?.Invoke();

    public static void RaiseEffectTriggered(CardEffectType effectType)
        => OnEffectTriggered?.Invoke(effectType);

    public static void RaiseGridIntervention(GridInterventionType interventionType)
        => OnGridIntervention?.Invoke(interventionType);

    public static void RaisePowerUpUsed(PowerUpType powerUpType)
        => OnPowerUpUsed?.Invoke(powerUpType);

    // ─── SAHNe DEĞİŞİMİNDE TEMİZLİK ─────────────────────────────
    // Sahne değişince eski abonelikler temizlenir — memory leak önlemi.

    public static void ClearAllEvents()
    {
        OnCardRevealed = null;
        OnPairMatched = null;
        OnPairMismatch = null;
        OnGameStarted = null;
        OnGameWon = null;
        OnGameLost = null;
        OnGamePaused = null;
        OnGameResumed = null;
        OnMoveUsed = null;
        OnTimeChanged = null;
        OnTimeUp = null;
        OnEffectTriggered = null;
        OnGridIntervention = null;
        OnPowerUpUsed = null;
    }
}

// ─── ENUM TANIMLARI ───────────────────────────────────────────────

/// <summary>Özel kart efekt türleri</summary>
public enum CardEffectType
{
    None,
    Fog,            // Sis — ekranı örter
    Swapper,        // Yer Değiştiren — 2 kartı takas eder
    TimeThief,      // Zaman Hırsızı — süre azaltır
    LockBreaker     // Kilit Kırıcı — eşleşmiş çifti geri açar
}

/// <summary>Grid müdahale türleri</summary>
public enum GridInterventionType
{
    None,
    Rotate,         // Grid döner
    Shrink,         // Grid sıkışır
    RowAdded        // Yeni satır eklenir
}

/// <summary>Power-up türleri</summary>
public enum PowerUpType
{
    XRay,           // 2 sn tüm kartlar açık
    MindFreeze,     // 5 sn kartlar kapanmaz
    TimeFreeze      // Timer 3-5 sn durur
}