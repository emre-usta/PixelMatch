using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinPanelController : MonoBehaviour
{
    [Header("Yıldızlar")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;
    [SerializeField] private Color starActiveColor = new Color(1.00f, 0.79f, 0.24f);
    [SerializeField] private Color starInactiveColor = new Color(0.20f, 0.16f, 0.25f);

    [Header("Stat Card")]
    [SerializeField] private TextMeshProUGUI txtScoreValue;
    [SerializeField] private TextMeshProUGUI txtTimeBonusValue;
    [SerializeField] private GameObject panelNewRecord;

    [Header("Stat Card Labels")]
    [SerializeField] private TextMeshProUGUI txtTimeBonusLabel;
    [SerializeField] private TextMeshProUGUI txtScoreLabel;

    [Header("Power-up Ödül")]
    [SerializeField] private GameObject panelPowerUpReward;
    [SerializeField] private GameObject[] rewardIcons;
    [SerializeField] private Image[] rewardIconImages;
    [SerializeField] private TextMeshProUGUI[] rewardCountTexts;

    [Header("Power-up Sprite'ları")]
    [SerializeField] private Sprite sprXRay;
    [SerializeField] private Sprite sprMindFreeze;
    [SerializeField] private Sprite sprTimeFreeze;

    [Header("2x Ödül")]
    [SerializeField] private GameObject btnDoubleReward;
    [SerializeField] private Color btnDoubleActiveColor = new Color(1.00f, 1.00f, 1.00f);
    [SerializeField] private Color btnDoubleUsedColor = new Color(0.45f, 0.45f, 0.50f);

    [Header("Animasyon")]
    [SerializeField] private RectTransform txtYouWin;
    [SerializeField] private RectTransform panelNewRecordRect;

    [Header("Toplam Yıldız")]
    [SerializeField] private TextMeshProUGUI txtTotalStars;

    private List<(PowerUpType type, int amount)> pendingRewards = new();
    private List<(PowerUpType type, int amount)> displayedRewards = new();
    private bool doubleRewardUsed = false;

    // ─── PUBLIC ───────────────────────────────────────────────────

    public void AddPendingReward(PowerUpType type, int amount = 1)
        => pendingRewards.Add((type, amount));

    public void ClearPendingRewards() => pendingRewards.Clear();

    public void ShowResult(int stars)
    {
        doubleRewardUsed = false;
        displayedRewards.Clear();

        if (btnDoubleReward != null)
            btnDoubleReward.SetActive(false);

        ResetStars();
        UpdateScore();
        CheckNewRecord();
        StartCoroutine(AnimateStars(stars));
        StartCoroutine(ShowPowerUpRewards());

        if (txtYouWin != null)
            StartCoroutine(PulseAnimation(txtYouWin));

        if (panelNewRecord != null && panelNewRecord.activeSelf && panelNewRecordRect != null)
            StartCoroutine(BounceAnimation(panelNewRecordRect));
    }

    public void ShowFreeModeResult(int stars)
    {
        doubleRewardUsed = false;
        displayedRewards.Clear();

        if (btnDoubleReward != null)
            btnDoubleReward.SetActive(false);

        ResetStars();

        if (txtTimeBonusValue != null) txtTimeBonusValue.text = "+0";
        if (txtScoreValue != null) txtScoreValue.text = "0";
        if (panelNewRecord != null) panelNewRecord.SetActive(false);

        StartCoroutine(AnimateStars(stars));

        if (txtYouWin != null)
            StartCoroutine(PulseAnimation(txtYouWin));
    }

    private void ResetStars()
    {
        if (star1) star1.color = starInactiveColor;
        if (star2) star2.color = starInactiveColor;
        if (star3) star3.color = starInactiveColor;
    }

    // ─── 2X ÖDÜL ──────────────────────────────────────────────────

    public void OnDoubleRewardClicked()
    {
        if (doubleRewardUsed) return;

        AdManager.Instance?.ShowRewarded(
            onSuccess: () =>
            {
                doubleRewardUsed = true;

                foreach (var (type, amount) in displayedRewards)
                    PowerUpManager.Instance?.AddPowerUp(type, amount);

                for (int i = 0; i < displayedRewards.Count && i < rewardCountTexts.Length; i++)
                {
                    if (rewardCountTexts[i] != null)
                        rewardCountTexts[i].text = $"x{displayedRewards[i].amount * 2}";
                }

                Time.timeScale = 0f;
                StartCoroutine(DeactivateDoubleRewardButton());
            }
        );
    }

    private IEnumerator ShowDoubleRewardButton()
    {
        if (btnDoubleReward == null) yield break;

        Image btnImage = btnDoubleReward.GetComponent<Image>();
        if (btnImage != null) btnImage.color = btnDoubleActiveColor;

        Button btn = btnDoubleReward.GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        CanvasGroup cg = btnDoubleReward.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnDoubleReward.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        btnDoubleReward.transform.localScale = Vector3.one * 0.8f;
        btnDoubleReward.SetActive(true);

        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            btnDoubleReward.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t);
            yield return null;
        }

        cg.alpha = 1f;
        btnDoubleReward.transform.localScale = Vector3.one;

        if (btn != null) btn.interactable = true;

        StartCoroutine(PulseDoubleRewardButton());
    }

    private IEnumerator PulseDoubleRewardButton()
    {
        if (btnDoubleReward == null) yield break;

        while (!doubleRewardUsed)
        {
            float elapsed = 0f;
            float half = 0.5f;

            while (elapsed < half && !doubleRewardUsed)
            {
                elapsed += Time.unscaledDeltaTime;
                btnDoubleReward.transform.localScale =
                    Vector3.one * Mathf.Lerp(1f, 1.06f, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half && !doubleRewardUsed)
            {
                elapsed += Time.unscaledDeltaTime;
                btnDoubleReward.transform.localScale =
                    Vector3.one * Mathf.Lerp(1.06f, 1f, elapsed / half);
                yield return null;
            }
        }

        btnDoubleReward.transform.localScale = Vector3.one;
    }

    private IEnumerator DeactivateDoubleRewardButton()
    {
        if (btnDoubleReward == null) yield break;

        Button btn = btnDoubleReward.GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        Image btnImage = btnDoubleReward.GetComponent<Image>();
        if (btnImage != null) btnImage.color = btnDoubleUsedColor;

        TextMeshProUGUI txt = btnDoubleReward.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = LocalizationManager.Get("btn_double_used");

        CanvasGroup cg = btnDoubleReward.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnDoubleReward.AddComponent<CanvasGroup>();

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(1f, 0.5f, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0.5f;
    }

    // ─── POWER-UP ÖDÜL ────────────────────────────────────────────

    private IEnumerator ShowPowerUpRewards()
    {
        if (panelPowerUpReward == null || pendingRewards.Count == 0)
        {
            if (btnDoubleReward != null)
                btnDoubleReward.SetActive(false);
            yield break;
        }

        yield return new WaitForSecondsRealtime(1.6f);

        foreach (var icon in rewardIcons)
            if (icon != null) icon.SetActive(false);

        for (int i = 0; i < pendingRewards.Count && i < rewardIcons.Length; i++)
        {
            var (type, amount) = pendingRewards[i];

            if (rewardIconImages != null && i < rewardIconImages.Length && rewardIconImages[i] != null)
                rewardIconImages[i].sprite = GetPowerUpSprite(type);

            if (rewardCountTexts != null && i < rewardCountTexts.Length && rewardCountTexts[i] != null)
                rewardCountTexts[i].text = $"x{amount}";
        }

        panelPowerUpReward.SetActive(true);

        for (int i = 0; i < pendingRewards.Count && i < rewardIcons.Length; i++)
        {
            if (rewardIcons[i] != null)
            {
                rewardIcons[i].SetActive(true);
                StartCoroutine(ScalePop(rewardIcons[i].transform));
                yield return new WaitForSecondsRealtime(0.3f);
            }
        }

        displayedRewards = new List<(PowerUpType, int)>(pendingRewards);
        pendingRewards.Clear();

        if (!doubleRewardUsed)
            StartCoroutine(ShowDoubleRewardButton());
    }

    private Sprite GetPowerUpSprite(PowerUpType type) => type switch
    {
        PowerUpType.XRay => sprXRay,
        PowerUpType.MindFreeze => sprMindFreeze,
        PowerUpType.TimeFreeze => sprTimeFreeze,
        _ => null
    };

    // ─── SKOR ─────────────────────────────────────────────────────

    private void UpdateScore()
    {
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
        bool isFree = FreeModeManager.IsFreeModeActive;

        if (txtTimeBonusLabel != null)
            txtTimeBonusLabel.text = LocalizationManager.Get(
                isMoveMode ? "stat_moves_used" : "stat_time_remaining");

        if (txtScoreLabel != null)
            txtScoreLabel.text = LocalizationManager.Get("stat_performance");

        if (isMoveMode)
        {
            int usedMoves = MoveController.Instance?.MoveCount ?? 0;
            int moveLimit = MoveController.Instance?.MoveLimit ??
                (isFree ? 30 : (LevelSelectManager.SelectedLevel?.moveLimit ?? 0));
            int originalLimit = GameStateManager.OriginalMoveLimit > 0
                ? GameStateManager.OriginalMoveLimit : moveLimit;
            float ratio = originalLimit > 0
                ? (float)Mathf.Max(0, originalLimit - usedMoves) / originalLimit : 0f;

            if (txtTimeBonusValue != null) txtTimeBonusValue.text = $"{usedMoves} / {moveLimit}";
            if (txtScoreValue != null) txtScoreValue.text = GetPerformanceLabel(ratio);
        }
        else
        {
            float remainingTime = TimerController.Instance?.RemainingTime ?? 0f;
            float timeLimit = isFree
                ? (TimerController.Instance?.TotalTime ?? 120f)
                : (LevelSelectManager.SelectedLevel?.timeLimit ?? 0f);
            float ratio = timeLimit > 0 ? remainingTime / timeLimit : 0f;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            if (txtTimeBonusValue != null) txtTimeBonusValue.text = $"{minutes:00}:{seconds:00}";
            if (txtScoreValue != null) txtScoreValue.text = GetPerformanceLabel(ratio);
        }

        if (txtTotalStars != null)
        {
            int total = PlayerPrefs.GetInt("total_stars", 0);
            txtTotalStars.text = $"{LocalizationManager.Get("total_stars")}: {total}";
        }
    }

    private string GetPerformanceLabel(float ratio)
    {
        if (ratio >= 0.60f) return LocalizationManager.Get("perf_perfect");
        if (ratio >= 0.30f) return LocalizationManager.Get("perf_good");
        return LocalizationManager.Get("perf_completed");
    }

    // ─── NEW RECORD ───────────────────────────────────────────────

    private void CheckNewRecord()
    {
        if (panelNewRecord == null) return;
        if (LevelSelectManager.SelectedLevel == null) { panelNewRecord.SetActive(false); return; }

        int categoryID = LevelSelectManager.SelectedCategoryID;
        int levelID = LevelSelectManager.SelectedLevel.levelID;
        int mode = (int)LevelSelectManager.SelectedMode;
        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;
        bool isNewRecord;

        if (isMoveMode)
        {
            int current = MoveController.Instance?.MoveCount ?? 9999;
            int existing = PlayerPrefs.GetInt($"record_{categoryID}_{levelID}_{mode}_moves", 9999);
            isNewRecord = current <= existing;
        }
        else
        {
            float current = (LevelSelectManager.SelectedLevel?.timeLimit ?? 0f) -
                             (TimerController.Instance?.RemainingTime ?? 0f);
            float existing = PlayerPrefs.GetFloat($"record_{categoryID}_{levelID}_{mode}", 9999f);
            isNewRecord = current <= existing;
        }

        panelNewRecord.SetActive(isNewRecord);
    }

    // ─── YILDIZ ANİMASYONU ────────────────────────────────────────

    private IEnumerator AnimateStars(int stars)
    {
        yield return new WaitForSecondsRealtime(0.4f);

        if (stars >= 1 && star1) { star1.color = starActiveColor; StartCoroutine(ScalePop(star1.transform)); yield return new WaitForSecondsRealtime(0.4f); }
        if (stars >= 2 && star2) { star2.color = starActiveColor; StartCoroutine(ScalePop(star2.transform)); yield return new WaitForSecondsRealtime(0.4f); }
        if (stars >= 3 && star3) { star3.color = starActiveColor; StartCoroutine(ScalePop(star3.transform)); }
    }

    private IEnumerator ScalePop(Transform t)
    {
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, elapsed / duration);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // ─── PULSE ────────────────────────────────────────────────────

    private IEnumerator PulseAnimation(RectTransform rt)
    {
        float duration = 1.2f;
        float half = duration / 2f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.05f, elapsed / half);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.95f, elapsed / half);
                yield return null;
            }
        }
    }

    // ─── BOUNCE ───────────────────────────────────────────────────

    private IEnumerator BounceAnimation(RectTransform rt)
    {
        Vector3 originalPos = rt.anchoredPosition;
        float duration = 0.5f;
        float half = duration / 2f;
        float bounceHeight = 12f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.anchoredPosition = originalPos + new Vector3(0, Mathf.Lerp(0f, bounceHeight, elapsed / half), 0);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                rt.anchoredPosition = originalPos + new Vector3(0, Mathf.Lerp(bounceHeight, 0f, elapsed / half), 0);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}