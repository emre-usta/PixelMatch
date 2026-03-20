using System.Collections;
using UnityEngine;
using TMPro;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI effectFeedbackText;  // "-10 SANİYE!" yukarı kayar
    [SerializeField] private TextMeshProUGUI bannerText;          // "ZAMAN HIRSIZI!" ortada büyük

    [Header("Tutorial")]
    [SerializeField] private GameObject timeThiefTutorialPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnEffectTriggered += HandleEffectTriggered;
    }

    private void OnDisable()
    {
        GameEvents.OnEffectTriggered -= HandleEffectTriggered;
    }

    private void HandleEffectTriggered(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.TimeThief:
                bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

                if (isMoveMode)
                {
                    // Hamle modunda -3 hamle
                    MoveController.Instance?.StealMoves(3);
                    StartCoroutine(ShowFeedback("-3 HAMLE!", new Color(0.89f, 0.29f, 0.29f)));
                    StartCoroutine(ShowBanner("HAMLE HIRSIZI!", new Color(0.89f, 0.29f, 0.29f)));
                }
                else
                {
                    // Klasik modda -5 saniye
                    TimerController.Instance?.AddTime(-5f);
                    StartCoroutine(ShowFeedback("-5 SANİYE!", new Color(0.89f, 0.29f, 0.29f)));
                    StartCoroutine(ShowBanner("ZAMAN HIRSIZI!", new Color(0.89f, 0.29f, 0.29f)));
                }

                if (PlayerPrefs.GetInt("seen_timethief", 0) == 0)
                {
                    PlayerPrefs.SetInt("seen_timethief", 1);
                    PlayerPrefs.Save();
                    StartCoroutine(ShowTutorialPopup());
                }
                break;
        }
    }

    // ─── FEEDBACK (yukarı kayarak kaybolur) ───────────────────────

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

    // ─── BANNER (ortada büyük, fade out) ──────────────────────────

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
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            bannerText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
    }

    // ─── TUTORIAL POPUP ───────────────────────────────────────────

    private IEnumerator ShowTutorialPopup()
    {
        yield return new WaitForSeconds(0.5f);

        if (timeThiefTutorialPopup != null)
        {
            // Moda göre metni güncelle
            bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

            TextMeshProUGUI descText = timeThiefTutorialPopup
                .GetComponentInChildren<TextMeshProUGUI>();

            // Text_Desc'i bul
            Transform descTransform = timeThiefTutorialPopup.transform.Find("Panel/Text_Desc");
            if (descTransform != null)
            {
                TextMeshProUGUI desc = descTransform.GetComponent<TextMeshProUGUI>();
                if (desc != null)
                {
                    desc.text = isMoveMode
                        ? "Bu kart yanlış eşleştirildiğinde\n3 hamle hakkını çalar.\nDikkatli ol!"
                        : "Bu kart yanlış eşleştirildiğinde\n5 saniyeni çalar.\nDikkatli ol!";
                }
            }

            Time.timeScale = 0f;
            timeThiefTutorialPopup.SetActive(true);
        }
    }


    public void OnTutorialPopupClosed()
    {
        if (timeThiefTutorialPopup != null)
            timeThiefTutorialPopup.SetActive(false);
        Time.timeScale = 1f;
    }
}