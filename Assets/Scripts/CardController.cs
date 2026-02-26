using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PixelMatch — Kart Kontrolcüsü
/// Her kartın state'ini ve davranışını yönetir.
/// GridManager tarafından üretilir ve yapılandırılır.
/// </summary>
public class CardController : MonoBehaviour
{
    // ─── KART VERİSİ ──────────────────────────────────────────────

    public int CardID { get; private set; }           // Eşleşme için kimlik (aynı ID = çift)
    public CardState State { get; private set; } = CardState.Closed;
    public CardEffectType EffectType { get; private set; } = CardEffectType.None;

    // ─── GÖRSEL REFERANSLAR ───────────────────────────────────────

    [Header("Görsel")]
    [SerializeField] private Image cardImage;         // Kartın UI Image componenti
    [SerializeField] private Sprite cardBackSprite;   // Kapalı yüz (arka)
    private Sprite cardFrontSprite;                   // Açık yüz (ön) — GridManager atar

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();
    }

    // ─── KURULUM (GridManager çağırır) ────────────────────────────

    /// <summary>
    /// GridManager bu kartı üretince çağırır.
    /// ID, sprite ve efekt tipini dışarıdan alır.
    /// </summary>
    public void Setup(int id, Sprite frontSprite, Sprite backSprite, CardEffectType effectType = CardEffectType.None)
    {
        CardID = id;
        cardFrontSprite = frontSprite;
        cardBackSprite = backSprite;
        EffectType = effectType;

        SetState(CardState.Closed);
        ShowBack();
    }

    // ─── TIKLANMA ─────────────────────────────────────────────────

    public void OnCardClicked()
    {
        if (!GameStateManager.Instance.IsPlaying) return;
        if (State != CardState.Closed) return;
        Reveal();
    }

    // ─── KART STATE İŞLEMLERİ ─────────────────────────────────────

    /// <summary>Kartı açar ve event fırlatır.</summary>
    public void Reveal()
    {
        SetState(CardState.Open);
        ShowFront();
        GameEvents.RaiseCardRevealed(this);
    }

    /// <summary>Kartı kapatır (yanlış eşleşme sonrası).</summary>
    public void Hide()
    {
        SetState(CardState.Closed);
        ShowBack();
    }

    /// <summary>Kartı eşleşmiş olarak işaretler.</summary>
    public void SetMatched()
    {
        SetState(CardState.Matched);
        cardImage.enabled = false;
        GetComponent<Button>().enabled = false;
        // İleride: eşleşme animasyonu buraya
    }

    /// <summary>Kartı kilitler (LockBreaker efekti).</summary>
    public void SetLocked()
    {
        SetState(CardState.Locked);
        // İleride: kilit görseli buraya
    }

    /// <summary>Kilitli kartı tekrar kapalıya döndürür.</summary>
    public void Unlock()
    {
        SetState(CardState.Closed);
        ShowBack();
    }

    // ─── GÖRSEL YARDIMCILAR ───────────────────────────────────────

    private void ShowFront()
    {
        if (cardImage != null && cardFrontSprite != null)
            cardImage.sprite = cardFrontSprite;
    }

    private void ShowBack()
    {
        if (cardImage != null && cardBackSprite != null)
            cardImage.sprite = cardBackSprite;
    }

    private void SetState(CardState newState)
    {
        State = newState;
    }

    // ─── RAYCAST KONTROLÜ ─────────────────────────────────────────

    /// <summary>Input engelleme — GridManager tüm kartlar için çağırır.</summary>
    public void SetInteractable(bool interactable)
    {
        if (cardImage != null)
            cardImage.raycastTarget = interactable;
    }
}

// ─── CARD STATE ENUM ──────────────────────────────────────────────

public enum CardState
{
    Closed,     // Kapalı — tıklanabilir
    Open,       // Açık — eşleşme bekleniyor
    Matched,    // Eşleşti — devre dışı
    Locked      // Kilitli — LockBreaker tarafından kilitlendi
}