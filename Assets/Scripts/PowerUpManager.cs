using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    private const string KEY_REVEAL = "powerup_reveal";
    private const string KEY_HINT = "powerup_hint";
    private const string KEY_FREEZE = "powerup_freeze";
    private const int MAX_COUNT = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── ENVANTER ─────────────────────────────────────────────────

    public int GetCount(PowerUpType type)
        => PlayerPrefs.GetInt(GetKey(type), 0);

    public void AddPowerUp(PowerUpType type, int amount = 1)
    {
        string key = GetKey(type);
        int newCount = Mathf.Min(PlayerPrefs.GetInt(key, 0) + amount, MAX_COUNT);
        PlayerPrefs.SetInt(key, newCount);
        PlayerPrefs.Save();
        PowerUpUI.Instance?.UpdateUI();
    }

    public bool TryUsePowerUp(PowerUpType type)
    {
        string key = GetKey(type);
        int current = PlayerPrefs.GetInt(key, 0);

        if (current <= 0)
        {
            AdRewardPopup.Instance?.ShowForPowerUp(type, () =>
            {
                AddPowerUp(type, 1);
            });
            return false;
        }

        PlayerPrefs.SetInt(key, current - 1);
        PlayerPrefs.Save();
        PowerUpUI.Instance?.UpdateUI();
        return true;
    }

    private string GetKey(PowerUpType type) => type switch
    {
        PowerUpType.XRay => KEY_REVEAL,
        PowerUpType.MindFreeze => KEY_HINT,
        PowerUpType.TimeFreeze => KEY_FREEZE,
        _ => "powerup_unknown"
    };

    // ─── KULLANIM ─────────────────────────────────────────────────

    public void UseXRay()
    {
        if (!TryUsePowerUp(PowerUpType.XRay)) return;
        FirebaseManager.Instance?.LogPowerUpUsed("xray");
        StartCoroutine(XRayCoroutine());
    }

    public void UseHint()
    {
        if (GridManager.Instance != null)
        {
            int openCount = 0;
            foreach (var card in GridManager.Instance.AllCards)
                if (card.State == CardState.Open)
                    openCount++;

            if (openCount > 2)
                return;
        }

        if (!TryUsePowerUp(PowerUpType.MindFreeze)) return;
        FirebaseManager.Instance?.LogPowerUpUsed("hint");
        StartCoroutine(HintCoroutine());
    }

    public void UseTimeFreeze()
    {
        if (!TryUsePowerUp(PowerUpType.TimeFreeze)) return;
        FirebaseManager.Instance?.LogPowerUpUsed("freeze");
        StartCoroutine(TimeFreezeCoroutine());
    }

    // ─── X-RAY ────────────────────────────────────────────────────

    private IEnumerator XRayCoroutine()
    {
        if (GridManager.Instance == null) yield break;

        GridManager.Instance.SetAllCardsInteractable(false);

        CardController selectedCard = GridManager.Instance.FirstSelectedCard;

        foreach (var card in GridManager.Instance.AllCards)
            if (card.State == CardState.Closed)
                card.RevealSilent();

        yield return new WaitForSecondsRealtime(2f);

        foreach (var card in GridManager.Instance.AllCards)
        {
            if (card == selectedCard) continue;
            if (card.State == CardState.Open)
                card.Hide();
        }

        yield return new WaitForSecondsRealtime(0.3f);

        GridManager.Instance.SetAllCardsInteractable(true);
    }

    // ─── HINT ─────────────────────────────────────────────────────

    private IEnumerator HintCoroutine()
    {
        if (GridManager.Instance == null) yield break;

        var cards = GridManager.Instance.AllCards;
        CardController hintA = null;
        CardController hintB = null;

        CardController selectedCard = GridManager.Instance.FirstSelectedCard;

        if (selectedCard != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == selectedCard) continue;
                if (cards[i].State != CardState.Closed) continue;
                if (cards[i].CardID != selectedCard.CardID) continue;
                hintA = selectedCard;
                hintB = cards[i];
                break;
            }
        }

        if (hintA == null || hintB == null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].State != CardState.Closed) continue;
                for (int j = i + 1; j < cards.Count; j++)
                {
                    if (cards[j].State != CardState.Closed) continue;
                    if (cards[i].CardID != cards[j].CardID) continue;
                    hintA = cards[i];
                    hintB = cards[j];
                    break;
                }
                if (hintA != null) break;
            }
        }

        if (hintA == null || hintB == null) yield break;

        GridManager.Instance.SetAllCardsInteractable(false);
        hintA.RevealSilent();
        hintB.RevealSilent();

        yield return new WaitForSecondsRealtime(0.6f);

        GridManager.Instance.ClearSelectedCard();
        GameEvents.RaisePairMatched(hintA, hintB);

        yield return new WaitForSecondsRealtime(0.3f);
        GridManager.Instance.SetAllCardsInteractable(true);
    }

    // ─── TIME FREEZE ──────────────────────────────────────────────

    private IEnumerator TimeFreezeCoroutine()
    {
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        ComboEffectManager.Instance?.TriggerFreezeFlash();
        AudioManager.Instance?.PlayFreezeSFX();
        PowerUpUI.Instance?.ShowFreezeFeedback();

        if (isMoveMode)
        {
            MoveController.Instance?.ActivateFreezeForMoves(3);
            yield break;
        }

        TimerController.Instance?.PauseTimer();

        yield return new WaitForSecondsRealtime(5f);

        TimerController.Instance?.ResumeTimer();
        PowerUpUI.Instance?.HideFreezeFeedback();
    }
}