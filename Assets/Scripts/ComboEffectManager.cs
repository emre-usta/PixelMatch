using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboEffectManager : MonoBehaviour
{
    public static ComboEffectManager Instance { get; private set; }

    [Header("Flash Efekti")]
    [SerializeField] private Image flashOverlay;        // Tam ekran flash image
    [SerializeField] private TextMeshProUGUI txtCombo;  // "COMBO x3!" yazısı

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (flashOverlay != null)
            flashOverlay.gameObject.SetActive(false);
        if (txtCombo != null)
            txtCombo.gameObject.SetActive(false);
    }

    public void TriggerComboFlash(int comboCount)
    {
        StartCoroutine(ComboFlashSequence(comboCount));
    }

    // Eşleşme anında yeşil flash
    public void TriggerMatchFlash()
    {
        StartCoroutine(QuickFlash(new Color(0.32f, 0.93f, 0.51f, 0.15f))); // yeşil
    }

    // Time Thief tetiklenince kırmızı flash
    public void TriggerTimeThiefFlash()
    {
        StartCoroutine(QuickFlash(new Color(0.89f, 0.29f, 0.29f, 0.25f))); // kırmızı
    }

    // Freeze power-up mavi flash (zaten var ama buraya da bağlayabiliriz)
    public void TriggerFreezeFlash()
    {
        StartCoroutine(QuickFlash(new Color(0.27f, 0.67f, 1.00f, 0.20f))); // mavi
    }

    // ─── COMBO FLASH ──────────────────────────────────────────────
    private IEnumerator ComboFlashSequence(int count)
    {
        if (flashOverlay == null || txtCombo == null) yield break;

        // Combo yazısını göster
        txtCombo.text = $"COMBO x{count}!";
        txtCombo.gameObject.SetActive(true);
        txtCombo.transform.localScale = Vector3.zero;

        // Yazı pop animasyonu
        float elapsed = 0f;
        float popDuration = 0.2f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / popDuration);
            txtCombo.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsed / 0.1f);
            txtCombo.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Flash overlay: altın renk
        flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, 0f);
        flashOverlay.gameObject.SetActive(true);

        // Fade in
        elapsed = 0f;
        float fadeDuration = 0.1f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 0.3f, elapsed / fadeDuration);
            flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, alpha);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.3f, 0f, elapsed / 0.3f);
            flashOverlay.color = new Color(0.96f, 0.65f, 0.14f, alpha);
            yield return null;
        }

        flashOverlay.gameObject.SetActive(false);

        // Combo yazısı fade out
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
            txtCombo.color = new Color(
                txtCombo.color.r,
                txtCombo.color.g,
                txtCombo.color.b,
                alpha);
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

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, flashColor.a, elapsed / 0.08f);
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / 0.2f);
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        flashOverlay.gameObject.SetActive(false);
    }

    public void TriggerComboText(int comboCount)
    {
        StartCoroutine(ComboTextOnly(comboCount));
    }

    private IEnumerator ComboTextOnly(int count)
    {
        if (txtCombo == null) yield break;

        txtCombo.text = $"COMBO x{count}!";
        txtCombo.gameObject.SetActive(true);
        txtCombo.transform.localScale = Vector3.zero;

        // Pop animasyonu
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / 0.2f);
            txtCombo.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsed / 0.1f);
            txtCombo.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Bekle
        yield return new WaitForSecondsRealtime(0.5f);

        // Fade out
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
}