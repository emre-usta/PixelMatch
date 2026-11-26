using System.Collections;
using UnityEngine;
using TMPro;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI effectFeedbackText;
    [SerializeField] private TextMeshProUGUI bannerText;

    [Header("Tutorial")]
    [SerializeField] private GameObject timeThiefTutorialPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() => GameEvents.OnEffectTriggered += HandleEffectTriggered;
    private void OnDisable() => GameEvents.OnEffectTriggered -= HandleEffectTriggered;

    private void HandleEffectTriggered(CardEffectType effectType)
    {
        if (effectType != CardEffectType.TimeThief) return;

        bool isMoveMode = GameSession.IsMoveMode;
        Color red = new Color(0.89f, 0.29f, 0.29f);

        ComboEffectManager.Instance?.TriggerTimeThiefFlash();

        if (isMoveMode)
        {
            MoveController.Instance?.StealMoves(3);
            string text = LocalizationManager.Get("banner_move_thief");
            StartCoroutine(ShowFeedback(text, red));
            StartCoroutine(ShowBanner(text, red));
        }
        else
        {
            TimerController.Instance?.AddTime(-5f);
            string text = LocalizationManager.Get("banner_time_thief");
            StartCoroutine(ShowFeedback(text, red));
            StartCoroutine(ShowBanner(text, red));
        }

        if (PlayerPrefs.GetInt("seen_timethief", 0) == 0)
        {
            PlayerPrefs.SetInt("seen_timethief", 1);
            PlayerPrefs.Save();
            StartCoroutine(ShowTutorialPopup());
        }
    }

    // ─── FEEDBACK ─────────────────────────────────────────────────

    private IEnumerator ShowFeedback(string text, Color color)
    {
        if (effectFeedbackText == null) yield break;

        effectFeedbackText.text = text;
        effectFeedbackText.color = color;
        effectFeedbackText.gameObject.SetActive(true);

        Vector3 startPos = effectFeedbackText.transform.localPosition;
        float elapsed = 0f;
        float duration = 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            effectFeedbackText.transform.localPosition = startPos + Vector3.up * (80f * t);
            effectFeedbackText.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        effectFeedbackText.gameObject.SetActive(false);
        effectFeedbackText.transform.localPosition = startPos;
    }

    // ─── BANNER ───────────────────────────────────────────────────

    private IEnumerator ShowBanner(string text, Color color)
    {
        if (bannerText == null) yield break;

        bannerText.text = text;
        bannerText.color = new Color(color.r, color.g, color.b, 1f);
        bannerText.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.8f);

        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bannerText.color = new Color(color.r, color.g, color.b,
                Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
    }

    // ─── TUTORIAL POPUP ───────────────────────────────────────────

    private IEnumerator ShowTutorialPopup()
    {
        yield return new WaitForSeconds(0.5f);

        if (timeThiefTutorialPopup == null) yield break;

        bool isMoveMode = GameSession.IsMoveMode;

        Transform titleT = timeThiefTutorialPopup.transform
            .Find("Panel_Content/Panel_Title/Text_PauseTitle");
        if (titleT != null)
        {
            TextMeshProUGUI title = titleT.GetComponent<TextMeshProUGUI>();
            if (title != null)
                title.text = isMoveMode
                    ? LocalizationManager.Get("move_thief_title")
                    : LocalizationManager.Get("time_thief_title");
        }

        Transform descT = timeThiefTutorialPopup.transform
            .Find("Panel_Content/Text_Desc");
        if (descT != null)
        {
            TextMeshProUGUI desc = descT.GetComponent<TextMeshProUGUI>();
            if (desc != null)
                desc.text = isMoveMode
                    ? LocalizationManager.Get("move_thief_desc")
                    : LocalizationManager.Get("time_thief_desc");
        }

        TimeScaleController.Request(TimeScaleReason.TimeThiefInfo);
        timeThiefTutorialPopup.SetActive(true);
    }

    public void OnTutorialPopupClosed()
    {
        if (timeThiefTutorialPopup != null)
            timeThiefTutorialPopup.SetActive(false);
        TimeScaleController.Release(TimeScaleReason.TimeThiefInfo);
    }
}