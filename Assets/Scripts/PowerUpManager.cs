using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    // ─── PLAYERPREFS ANAHTARLARI ──────────────────────────────────
    private const string KEY_REVEAL = "powerup_reveal";
    private const string KEY_HINT = "powerup_hint";
    private const string KEY_FREEZE = "powerup_freeze";
    private const int MAX_COUNT = 10;

    // ─── UNITY LIFECYCLE ──────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── ENVANTER YÖNETİMİ ────────────────────────────────────────

    public int GetCount(PowerUpType type)
    {
        return PlayerPrefs.GetInt(GetKey(type), 0);
    }

    public void AddPowerUp(PowerUpType type, int amount = 1)
    {
        string key = GetKey(type);
        int current = PlayerPrefs.GetInt(key, 0);
        int newCount = Mathf.Min(current + amount, MAX_COUNT);
        PlayerPrefs.SetInt(key, newCount);
        PlayerPrefs.Save();
        Debug.Log($"[PowerUpManager] {type} eklendi: {newCount}/{MAX_COUNT}");

        // UI güncelle
        PowerUpUI.Instance?.UpdateUI();
    }

    public bool TryUsePowerUp(PowerUpType type)
    {
        string key = GetKey(type);
        int current = PlayerPrefs.GetInt(key, 0);

        if (current <= 0)
        {
            Debug.Log($"[PowerUpManager] {type} yok!");
            return false;
        }

        PlayerPrefs.SetInt(key, current - 1);
        PlayerPrefs.Save();
        Debug.Log($"[PowerUpManager] {type} kullanıldı. Kalan: {current - 1}");

        PowerUpUI.Instance?.UpdateUI();
        return true;
    }

    private string GetKey(PowerUpType type)
    {
        return type switch
        {
            PowerUpType.XRay => KEY_REVEAL,
            PowerUpType.MindFreeze => KEY_HINT,
            PowerUpType.TimeFreeze => KEY_FREEZE,
            _ => "powerup_unknown"
        };
    }

    // ─── POWER-UP KULLANIMI ───────────────────────────────────────

    public void UseXRay()
    {
        if (!TryUsePowerUp(PowerUpType.XRay)) return;
        StartCoroutine(XRayCoroutine());
    }

    public void UseHint()
    {
        if (!TryUsePowerUp(PowerUpType.MindFreeze)) return;
        StartCoroutine(HintCoroutine());
    }

    public void UseTimeFreeze()
    {
        if (!TryUsePowerUp(PowerUpType.TimeFreeze)) return;
        StartCoroutine(TimeFreezeCoroutine());
    }

    // ─── AÇIK BAK — Tüm kartlar 2sn açılır ──────────────────────
    private IEnumerator XRayCoroutine()
    {
        if (GridManager.Instance == null) yield break;

        // Input kilitle
        GridManager.Instance.SetAllCardsInteractable(false);

        // Tüm eşleşmemiş kartları sessizce aç
        foreach (var card in GridManager.Instance.AllCards)
            if (card.State == CardState.Closed)
                card.RevealSilent();

        yield return new WaitForSecondsRealtime(2f);

        // Kapat
        foreach (var card in GridManager.Instance.AllCards)
            if (card.State == CardState.Open)
                card.Hide();

        yield return new WaitForSecondsRealtime(0.3f);

        // Input aç
        GridManager.Instance.SetAllCardsInteractable(true);
        Debug.Log("[PowerUpManager] X-Ray tamamlandı.");
    }

    // ─── İPUCU — Bir çift otomatik eşleşir ──────────────────────
    private IEnumerator HintCoroutine()
    {
        if (GridManager.Instance == null) yield break;

        // Eşleşmemiş bir çift bul
        var cards = GridManager.Instance.AllCards;
        CardController hintA = null;
        CardController hintB = null;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].State != CardState.Closed) continue;
            for (int j = i + 1; j < cards.Count; j++)
            {
                if (cards[j].State != CardState.Closed) continue;
                if (cards[i].CardID == cards[j].CardID)
                {
                    hintA = cards[i];
                    hintB = cards[j];
                    break;
                }
            }
            if (hintA != null) break;
        }

        if (hintA == null || hintB == null)
        {
            Debug.Log("[PowerUpManager] Eşleşecek kart kalmadı.");
            yield break;
        }

        // Kısa flash animasyonu
        GridManager.Instance.SetAllCardsInteractable(false);
        hintA.RevealSilent();
        hintB.RevealSilent();

        yield return new WaitForSecondsRealtime(0.6f);

        // Otomatik eşleştir
        GameEvents.RaisePairMatched(hintA, hintB);
        Debug.Log("[PowerUpManager] İpucu: çift otomatik eşleştirildi.");

        yield return new WaitForSecondsRealtime(0.3f);
        GridManager.Instance.SetAllCardsInteractable(true);
    }

    // ─── SÜRE DONDUR — Timer 5sn durur ───────────────────────────
    private IEnumerator TimeFreezeCoroutine()
    {
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        if (isMoveMode)
        {
            // Hamle modunda: sonraki 3 hamle ücretsiz
            MoveController.Instance?.ActivateFreezeForMoves(3);
            PowerUpUI.Instance?.ShowFreezeFeedback();
            ComboEffectManager.Instance?.TriggerFreezeFlash(); // ← ekle
            AudioManager.Instance?.PlayFreezeSFX();
            Debug.Log("[PowerUpManager] Süre Dondur: sonraki 3 hamle ücretsiz!");
            yield break;
        }

        // Klasik modda: timer 5sn durur
        TimerController.Instance?.PauseTimer();
        PowerUpUI.Instance?.ShowFreezeFeedback();
        ComboEffectManager.Instance?.TriggerFreezeFlash(); // ← ekle
        AudioManager.Instance?.PlayFreezeSFX();
        Debug.Log("[PowerUpManager] Süre Dondur: 5sn durakladı.");

        yield return new WaitForSecondsRealtime(5f);

        TimerController.Instance?.ResumeTimer();
        PowerUpUI.Instance?.HideFreezeFeedback();
        Debug.Log("[PowerUpManager] Süre Dondur: timer devam etti.");
    }
}