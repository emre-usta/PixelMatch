using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    private const string KEY_REVEAL = "powerup_reveal";
    private const string KEY_HINT = "powerup_hint";
    private const string KEY_FREEZE = "powerup_freeze";
    private const int MAX_COUNT = 10;
    private bool isPowerUpActive = false;

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

    // Power-up'lar yalnızca oyun Playing iken kullanılabilir (AUDIT 1.7).
    private static bool IsGamePlaying =>
        GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying;

    private static bool IsMatchBeingChecked =>
        GridManager.Instance != null && GridManager.Instance.IsResolvingPair;

    private const float TIME_FREEZE_DURATION = 5f;
    private Coroutine timeFreezeRoutine;

    public void UseXRay()
    {
        if (isPowerUpActive) return;
        if (!IsGamePlaying) return;
        if (IsMatchBeingChecked) return;
        if (!TryUsePowerUp(PowerUpType.XRay)) return;
        FirebaseManager.Instance?.LogPowerUpUsed(
            "xray",
            LevelSelectManager.SelectedLevel?.levelID ?? 0,
            LevelSelectManager.SelectedCategoryID,
            "standard_play");
        StartCoroutine(XRayCoroutine());
    }

    public void UseHint()
    {
        if (isPowerUpActive) return;
        if (!IsGamePlaying) return;
        if (IsMatchBeingChecked) return;
        if (GridManager.Instance == null) return;

        int openCount = 0;
        foreach (var card in GridManager.Instance.AllCards)
            if (card.State == CardState.Open)
                openCount++;

        if (openCount > 2)
            return;

        // Çift envanter düşmeden önce aranır; bulunamazsa hak harcanmaz.
        if (!FindHintPair(out CardController hintA, out CardController hintB)) return;

        if (!TryUsePowerUp(PowerUpType.MindFreeze)) return;
        FirebaseManager.Instance?.LogPowerUpUsed(
            "hint",
            LevelSelectManager.SelectedLevel?.levelID ?? 0,
            LevelSelectManager.SelectedCategoryID,
            "standard_play");
        StartCoroutine(HintCoroutine(hintA, hintB));
    }

    public void UseTimeFreeze()
    {
        if (isPowerUpActive) return;
        if (!IsGamePlaying) return;
        if (!TryUsePowerUp(PowerUpType.TimeFreeze)) return;
        FirebaseManager.Instance?.LogPowerUpUsed(
            "freeze",
            LevelSelectManager.SelectedLevel?.levelID ?? 0,
            LevelSelectManager.SelectedCategoryID,
            "standard_play");
        timeFreezeRoutine = StartCoroutine(TimeFreezeCoroutine());
        // Move modunda korutin aynı karede biter (bedava hamleler MoveController'da); referans tutulmaz.
        if (!isPowerUpActive) timeFreezeRoutine = null;
    }

    // ─── X-RAY ────────────────────────────────────────────────────

    private IEnumerator XRayCoroutine()
    {
        isPowerUpActive = true;

        if (GridManager.Instance == null) { isPowerUpActive = false; yield break; }

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
        isPowerUpActive = false;
    }

    // ─── HINT ─────────────────────────────────────────────────────

    // Önce açık kartın eşi, yoksa kapalı ilk çift. Kart durumunu değiştirmez.
    private bool FindHintPair(out CardController hintA, out CardController hintB)
    {
        hintA = null;
        hintB = null;

        var cards = GridManager.Instance.AllCards;
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

        return hintA != null && hintB != null;
    }

    private IEnumerator HintCoroutine(CardController hintA, CardController hintB)
    {
        isPowerUpActive = true;

        if (GridManager.Instance == null) { isPowerUpActive = false; yield break; }

        GridManager.Instance.SetAllCardsInteractable(false);
        hintA.RevealSilent();
        hintB.RevealSilent();

        yield return new WaitForSecondsRealtime(0.6f);

        // Pause sırasında beklenir; oyun bu arada bittiyse (Win/Lose) eşleştirme yapılmaz.
        while (GameStateManager.Instance != null &&
               GameStateManager.Instance.CurrentState == GameState.Paused)
            yield return null;

        if (!IsGamePlaying)
        {
            GridManager.Instance.ClearSelectedCard();
            hintA.Hide();
            hintB.Hide();
            GridManager.Instance.SetAllCardsInteractable(true);
            isPowerUpActive = false;
            yield break;
        }

        GridManager.Instance.ClearSelectedCard();
        GameEvents.RaisePairMatched(hintA, hintB);

        yield return new WaitForSecondsRealtime(0.3f);

        GridManager.Instance.SetAllCardsInteractable(true);
        isPowerUpActive = false;
    }

    // ─── TIME FREEZE ──────────────────────────────────────────────

    private IEnumerator TimeFreezeCoroutine()
    {
        isPowerUpActive = true;

        bool isMoveMode = GameSession.IsMoveMode;

        ComboEffectManager.Instance?.TriggerFreezeFlash();
        AudioManager.Instance?.PlayFreezeSFX();
        PowerUpUI.Instance?.ShowFreezeFeedback();

        if (isMoveMode)
        {
            MoveController.Instance?.ActivateFreezeForMoves(3);
            isPowerUpActive = false;
            yield break;
        }

        // Dondurma, Pause/Resume'dan bağımsız (AUDIT 1.8): ResumeTimer artık dondurmayı bozmaz.
        TimerController.Instance?.Freeze();

        // Süre yalnızca oyun Playing iken azalır; ölçekli zaman kullanıldığı için Pause ve
        // timeScale=0 olan popup'lar sırasında da durur, Resume'da kaldığı yerden devam eder.
        float remaining = TIME_FREEZE_DURATION;
        while (remaining > 0f)
        {
            yield return null;
            if (IsGamePlaying)
                remaining -= Time.deltaTime;
        }

        TimerController.Instance?.Unfreeze();
        PowerUpUI.Instance?.HideFreezeFeedback();
        isPowerUpActive = false;
        timeFreezeRoutine = null;
    }

    // Oyun bittiği anda (kazanma ya da kayıp) aktif dondurma sonlanır; second chance sonrası geri gelmez.
    public void CancelActiveTimeFreeze()
    {
        if (timeFreezeRoutine != null)
        {
            StopCoroutine(timeFreezeRoutine);
            timeFreezeRoutine = null;
            isPowerUpActive = false;
        }

        TimerController.Instance?.Unfreeze();
        MoveController.Instance?.CancelFreeze();

        if (PowerUpUI.Instance != null)
        {
            // ShowFreezeFeedback'in bekleyen gizleme çağrısı, sonraki bir dondurmanın göstergesini erken kapatmasın.
            PowerUpUI.Instance.CancelInvoke(nameof(PowerUpUI.HideFreezeFeedback));
            PowerUpUI.Instance.HideFreezeFeedback();
        }
    }
}