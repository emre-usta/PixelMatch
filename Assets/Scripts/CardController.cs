using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PixelMatch — Kart Kontrolcüsü
/// Her kartın state'ini, davranışını ve animasyonlarını yönetir.
/// </summary>
public class CardController : MonoBehaviour
{
    // ─── KART VERİSİ ──────────────────────────────────────────────

    public int CardID { get; private set; }
    public CardState State { get; private set; } = CardState.Closed;
    public CardEffectType EffectType { get; private set; } = CardEffectType.None;

    // ─── GÖRSEL REFERANSLAR ───────────────────────────────────────

    [Header("Görsel")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Sprite cardBackSprite;
    private Sprite cardFrontSprite;

    // ─── ANİMASYON AYARLARI ───────────────────────────────────────

    [Header("Animasyon")]
    [SerializeField] private float flipDuration = 0.15f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 8f;

    private Coroutine activeCoroutine;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────

    private void Awake()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();
    }

    // ─── KURULUM ──────────────────────────────────────────────────

    public void Setup(int id, Sprite frontSprite, Sprite backSprite, CardEffectType effectType = CardEffectType.None)
    {
        CardID = id;
        cardFrontSprite = frontSprite;
        cardBackSprite = backSprite;
        EffectType = effectType;

        SetState(CardState.Closed);
        ShowBack();
        transform.localScale = Vector3.one;
    }

    // ─── TIKLANMA ─────────────────────────────────────────────────

    public void OnCardClicked()
    {
        if (!GameStateManager.Instance.IsPlaying) return;
        if (State != CardState.Closed) return;
        Reveal();
    }

    // ─── KART STATE İŞLEMLERİ ─────────────────────────────────────

    public void Reveal()
    {
        SetState(CardState.Open);
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(FlipAnimation(cardBackSprite, cardFrontSprite));
        GameEvents.RaiseCardRevealed(this);
    }

    public void Hide()
    {
        SetState(CardState.Closed);
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(FlipAnimation(cardFrontSprite, cardBackSprite));
    }

    public void SetMatched()
    {
        SetState(CardState.Matched);
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(MatchAnimation());
    }

    public void SetLocked()
    {
        SetState(CardState.Locked);
    }

    public void Unlock()
    {
        SetState(CardState.Closed);
        ShowBack();
    }

    // ─── ANİMASYONLAR ─────────────────────────────────────────────

    private IEnumerator FlipAnimation(Sprite fromSprite, Sprite toSprite)
    {
        float elapsed = 0f;
        float half = flipDuration / 2f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = new Vector3(1f - t, 1f, 1f);
            yield return null;
        }

        if (cardImage != null && toSprite != null)
            cardImage.sprite = toSprite;

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = new Vector3(t, 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private IEnumerator MatchAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.2f, t);
            transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        elapsed = 0f;
        Color originalColor = cardImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cardImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - t);
            yield return null;
        }

        cardImage.enabled = false;
        GetComponent<Button>().enabled = false;
        transform.localScale = Vector3.one;
        cardImage.color = originalColor;
    }

    public IEnumerator ShakeAnimation()
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = originalPos.x + Mathf.Sin(elapsed * 40f) * shakeStrength;
            transform.localPosition = new Vector3(x, originalPos.y, originalPos.z);
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    // ─── GÖRSEL YARDIMCILAR ───────────────────────────────────────

    private void ShowBack()
    {
        if (cardImage != null && cardBackSprite != null)
            cardImage.sprite = cardBackSprite;
    }

    private void SetState(CardState newState)
    {
        State = newState;
    }

    public void SetInteractable(bool interactable)
    {
        if (cardImage != null)
            cardImage.raycastTarget = interactable;
    }
}

public enum CardState
{
    Closed,
    Open,
    Matched,
    Locked
}