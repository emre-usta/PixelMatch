using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboEffectManager : MonoBehaviour
{
    public static ComboEffectManager Instance { get; private set; }

    [Header("Flash Efekti")]
    [SerializeField] private Image flashOverlay;
    [SerializeField] private TextMeshProUGUI txtCombo;

    [Header("Combo Ödül")]
    [SerializeField] private TextMeshProUGUI txtComboReward;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (flashOverlay != null) flashOverlay.gameObject.SetActive(false);
        if (txtCombo != null) txtCombo.gameObject.SetActive(false);
    }

    // ─── PUBLIC ───────────────────────────────────────────────────

    public void TriggerComboFlash(int comboCount) => StartCoroutine(ComboFlashSequence(comboCount));
    public void TriggerComboText(int comboCount) => StartCoroutine(ComboTextOnly(comboCount));
    public void TriggerMatchFlash() => StartCoroutine(QuickFlash(new Color(0.32f, 0.93f, 0.51f, 0.15f)));
    public void TriggerTimeThiefFlash() => StartCoroutine(QuickFlash(new Color(0.89f, 0.29f, 0.29f, 0.25f)));
    public void TriggerFreezeFlash() => StartCoroutine(QuickFlash(new Color(0.27f, 0.67f, 1.00f, 0.20f)));
    public void ShowComboReward(string rewardText) => StartCoroutine(ShowRewardText(rewardText));

    // ─── COMBO FLASH ──────────────────────────────────────────────

    private IEnumerator ComboFlashSequence(int count)
    {
        if (flashOverlay == null || txtCombo == null) yield break;

        txtCombo.text = $"COMBO x{count}!";
        txtCombo.gameObject.SetActive(true);
        txtCombo.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            txtCombo.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, elapsed / 0.2f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            txtCombo.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, elapsed / 0.1f);
            yield return null;
        }

        flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, 0f);
        flashOverlay.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, Mathf.Lerp(0f, 0.3f, elapsed / 0.1f));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, Mathf.Lerp(0.3f, 0f, elapsed / 0.3f));
            yield return null;
        }

        flashOverlay.gameObject.SetActive(false);

        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
            txtCombo.color = new Color(txtCombo.color.r, txtCombo.color.g, txtCombo.color.b, alpha);
            yield return null;
        }

        txtCombo.gameObject.SetActive(false);
        txtCombo.color = new Color(txtCombo.color.r, txtCombo.color.g, txtCombo.color.b, 1f);
    }

    // ─── COMBO TEXT ONLY ──────────────────────────────────────────

    private IEnumerator ComboTextOnly(int count)
    {
        if (txtCombo == null) yield break;

        txtCombo.text = $"COMBO x{count}!";
        txtCombo.gameObject.SetActive(true);
        txtCombo.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            txtCombo.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, elapsed / 0.2f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            txtCombo.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, elapsed / 0.1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
            txtCombo.color = new Color(txtCombo.color.r, txtCombo.color.g, txtCombo.color.b, alpha);
            yield return null;
        }

        txtCombo.gameObject.SetActive(false);
        txtCombo.color = new Color(txtCombo.color.r, txtCombo.color.g, txtCombo.color.b, 1f);
    }

    // ─── QUICK FLASH ──────────────────────────────────────────────

    private IEnumerator QuickFlash(Color flashColor)
    {
        if (flashOverlay == null) yield break;

        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b,
                Mathf.Lerp(0f, flashColor.a, elapsed / 0.08f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b,
                Mathf.Lerp(flashColor.a, 0f, elapsed / 0.2f));
            yield return null;
        }

        flashOverlay.gameObject.SetActive(false);
    }

    // ─── COMBO REWARD ─────────────────────────────────────────────

    private IEnumerator ShowRewardText(string text)
    {
        if (txtComboReward == null) yield break;

        txtComboReward.text = text;
        txtComboReward.gameObject.SetActive(true);
        txtComboReward.color = new Color(0.37f, 0.94f, 0.82f, 1f);

        Vector3 startPos = txtComboReward.transform.localPosition;
        float elapsed = 0f;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            txtComboReward.transform.localPosition = startPos + Vector3.up * (40f * t);
            txtComboReward.color = new Color(0.37f, 0.94f, 0.82f, 1f - t);
            yield return null;
        }

        txtComboReward.gameObject.SetActive(false);
        txtComboReward.transform.localPosition = startPos;
    }
}