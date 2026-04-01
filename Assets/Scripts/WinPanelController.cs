using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinPanelController : MonoBehaviour
{
    [Header("Yıldızlar")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;
    [SerializeField] private Color starActiveColor = new Color(0.96f, 0.65f, 0.14f);
    [SerializeField] private Color starInactiveColor = new Color(0.16f, 0.13f, 0.06f);

    [Header("Stat Card")]
    [SerializeField] private TextMeshProUGUI txtScoreValue;
    [SerializeField] private TextMeshProUGUI txtTimeBonusValue;
    [SerializeField] private GameObject panelNewRecord;

    [Header("Animasyon")]
    [SerializeField] private RectTransform txtYouWin;
    [SerializeField] private RectTransform panelNewRecordRect;

    [Header("0 Yıldız Uyarısı")]
    [SerializeField] private GameObject noStarWarningPanel;

    // ─── PUBLIC ───────────────────────────────────────────────────

    public void ShowResult(int stars)
    {
        if (star1) star1.color = starInactiveColor;
        if (star2) star2.color = starInactiveColor;
        if (star3) star3.color = starInactiveColor;

        UpdateScore();
        CheckNewRecord();
        StartCoroutine(AnimateStars(stars));

        // YOU WIN pulse animasyonu
        if (txtYouWin != null)
            StartCoroutine(PulseAnimation(txtYouWin));

        // NEW RECORD bounce animasyonu
        if (panelNewRecord != null && panelNewRecord.activeSelf && panelNewRecordRect != null)
            StartCoroutine(BounceAnimation(panelNewRecordRect));
    }

    public void ShowNoStarWarning()
    {
        if (star1) star1.color = starInactiveColor;
        if (star2) star2.color = starInactiveColor;
        if (star3) star3.color = starInactiveColor;

        if (noStarWarningPanel != null)
            noStarWarningPanel.SetActive(true);
    }

    // ─── SKOR ─────────────────────────────────────────────────────

    private void UpdateScore()
    {
        if (LevelSelectManager.SelectedLevel == null) return;

        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        if (isMoveMode)
        {
            int usedMoves = MoveController.Instance != null ? MoveController.Instance.MoveCount : 0;
            int moveLimit = LevelSelectManager.SelectedLevel.moveLimit;
            int remaining = Mathf.Max(0, moveLimit - usedMoves);

            if (txtTimeBonusValue != null)
                txtTimeBonusValue.text = $"+{remaining * 100}";
            if (txtScoreValue != null)
                txtScoreValue.text = $"{remaining * 100:N0}".Replace(",", ".");
        }
        else
        {
            float remainingTime = TimerController.Instance != null
                ? TimerController.Instance.RemainingTime : 0f;
            int timeBonus = Mathf.RoundToInt(remainingTime) * 10;

            if (txtTimeBonusValue != null)
                txtTimeBonusValue.text = $"+{timeBonus:N0}".Replace(",", ".");
            if (txtScoreValue != null)
                txtScoreValue.text = $"{timeBonus:N0}".Replace(",", ".");
        }
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
        bool isNewRecord = false;

        if (isMoveMode)
        {
            string key = $"record_{categoryID}_{levelID}_{mode}_moves";
            int current = MoveController.Instance != null ? MoveController.Instance.MoveCount : 9999;
            int existing = PlayerPrefs.GetInt(key, 9999);
            isNewRecord = current <= existing;
        }
        else
        {
            string key = $"record_{categoryID}_{levelID}_{mode}";
            float current = LevelSelectManager.SelectedLevel.timeLimit -
                (TimerController.Instance != null ? TimerController.Instance.RemainingTime : 0f);
            float existing = PlayerPrefs.GetFloat(key, 9999f);
            isNewRecord = current <= existing;
        }

        panelNewRecord.SetActive(isNewRecord);
    }

    // ─── YILDIZ ANİMASYONU ────────────────────────────────────────

    private IEnumerator AnimateStars(int stars)
    {
        yield return new WaitForSecondsRealtime(0.4f);

        if (stars >= 1 && star1)
        {
            star1.color = starActiveColor;
            StartCoroutine(ScalePop(star1.transform));
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (stars >= 2 && star2)
        {
            star2.color = starActiveColor;
            StartCoroutine(ScalePop(star2.transform));
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (stars >= 3 && star3)
        {
            star3.color = starActiveColor;
            StartCoroutine(ScalePop(star3.transform));
        }
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

    // ─── YOU WIN PULSE ────────────────────────────────────────────

    private IEnumerator PulseAnimation(RectTransform rt)
    {
        float duration = 1.2f;
        float minScale = 0.95f;
        float maxScale = 1.05f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(minScale, maxScale, elapsed / (duration / 2f));
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(maxScale, minScale, elapsed / (duration / 2f));
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }

    // ─── NEW RECORD BOUNCE ────────────────────────────────────────

    private IEnumerator BounceAnimation(RectTransform rt)
    {
        Vector3 originalPos = rt.anchoredPosition;
        float duration = 0.5f;
        float bounceHeight = 12f;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float y = Mathf.Lerp(0f, bounceHeight, elapsed / (duration / 2f));
                rt.anchoredPosition = originalPos + new Vector3(0, y, 0);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float y = Mathf.Lerp(bounceHeight, 0f, elapsed / (duration / 2f));
                rt.anchoredPosition = originalPos + new Vector3(0, y, 0);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}