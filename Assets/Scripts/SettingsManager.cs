using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_BGM = "settings_bgm";
    private const string KEY_SFX = "settings_sfx";
    private const string KEY_BGM_MUTED = "settings_bgm_muted";
    private const string KEY_SFX_MUTED = "settings_sfx_muted";

    [Header("UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private GameObject settingsPanel;

    [Header("Value Texts")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("Mute Butonları")]
    [SerializeField] private Button btnBGMMute;
    [SerializeField] private Button btnSFXMute;
    [SerializeField] private TextMeshProUGUI txtBGMMute;
    [SerializeField] private TextMeshProUGUI txtSFXMute;

    [Header("Dil Butonları")]
    [SerializeField] private Button btnTR;
    [SerializeField] private Button btnEN;

    private bool bgmMuted = false;
    private bool sfxMuted = false;

    // ─── Aktif/Pasif renkleri ─────────────────────────────────────
    private readonly Color colorLangActive = new Color(0.10f, 0.23f, 0.06f); // #1A3A10
    private readonly Color colorLangInactive = new Color(0.04f, 0.04f, 0.08f); // #0A0A14
    private readonly Color colorTextActive = new Color(0.96f, 0.65f, 0.14f); // #F5A623
    private readonly Color colorTextInactive = new Color(0.32f, 0.26f, 0.20f); // #524534

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        bgmMuted = PlayerPrefs.GetInt(KEY_BGM_MUTED, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(KEY_SFX_MUTED, 0) == 1;

        if (bgmSlider != null)
        {
            bgmSlider.value = bgm;
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfx;
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        btnBGMMute?.onClick.AddListener(ToggleBGMMute);
        btnSFXMute?.onClick.AddListener(ToggleSFXMute);

        btnTR?.onClick.AddListener(() => SetLanguage(LocalizationManager.Language.TR));
        btnEN?.onClick.AddListener(() => SetLanguage(LocalizationManager.Language.EN));

        ApplyBGM(bgm);
        ApplySFX(sfx);
        UpdateBGMText(bgm);
        UpdateSFXText(sfx);
        UpdateMuteButtons();
        UpdateLanguageButtons();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ─── SLIDER HANDLERS ──────────────────────────────────────────

    private void OnBGMChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();
        UpdateBGMText(value);

        if (!bgmMuted)
            AudioManager.Instance?.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
        UpdateSFXText(value);

        if (!sfxMuted)
            AudioManager.Instance?.SetSFXVolume(value);
    }

    // ─── MUTE TOGGLE ──────────────────────────────────────────────

    public void ToggleBGMMute()
    {
        bgmMuted = !bgmMuted;
        PlayerPrefs.SetInt(KEY_BGM_MUTED, bgmMuted ? 1 : 0);
        PlayerPrefs.Save();

        float value = bgmMuted ? 0f : PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        AudioManager.Instance?.SetBGMVolume(value);
        UpdateMuteButtons();
    }

    public void ToggleSFXMute()
    {
        sfxMuted = !sfxMuted;
        PlayerPrefs.SetInt(KEY_SFX_MUTED, sfxMuted ? 1 : 0);
        PlayerPrefs.Save();

        float value = sfxMuted ? 0f : PlayerPrefs.GetFloat(KEY_SFX, 1f);
        AudioManager.Instance?.SetSFXVolume(value);
        UpdateMuteButtons();
    }

    private void ApplyBGM(float value)
    {
        AudioManager.Instance?.SetBGMVolume(bgmMuted ? 0f : value);
    }

    private void ApplySFX(float value)
    {
        AudioManager.Instance?.SetSFXVolume(sfxMuted ? 0f : value);
    }

    private void UpdateMuteButtons()
    {
        if (txtBGMMute != null)
            txtBGMMute.text = bgmMuted ? "🔇" : "🔊";
        if (txtSFXMute != null)
            txtSFXMute.text = sfxMuted ? "🔇" : "🔊";

        if (btnBGMMute != null)
        {
            Image img = btnBGMMute.GetComponent<Image>();
            if (img != null)
                img.color = bgmMuted
                    ? new Color(0.58f, 0.00f, 0.04f)
                    : new Color(0.04f, 0.12f, 0.06f);
        }

        if (btnSFXMute != null)
        {
            Image img = btnSFXMute.GetComponent<Image>();
            if (img != null)
                img.color = sfxMuted
                    ? new Color(0.58f, 0.00f, 0.04f)
                    : new Color(0.04f, 0.12f, 0.06f);
        }
    }

    // ─── DİL BUTONLARI ────────────────────────────────────────────

    private void SetLanguage(LocalizationManager.Language lang)
    {
        LocalizationManager.Instance?.SetLanguage(lang);
        UpdateLanguageButtons();
    }

    private void UpdateLanguageButtons()
    {
        bool isTR = LocalizationManager.CurrentLanguage == LocalizationManager.Language.TR;

        if (btnTR != null)
        {
            Image img = btnTR.GetComponent<Image>();
            if (img != null) img.color = isTR ? colorLangActive : colorLangInactive;
            TextMeshProUGUI txt = btnTR.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = isTR ? colorTextActive : colorTextInactive;
        }

        if (btnEN != null)
        {
            Image img = btnEN.GetComponent<Image>();
            if (img != null) img.color = !isTR ? colorLangActive : colorLangInactive;
            TextMeshProUGUI txt = btnEN.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = !isTR ? colorTextActive : colorTextInactive;
        }
    }

    // ─── VALUE TEXTS ──────────────────────────────────────────────

    private void UpdateBGMText(float value)
    {
        if (bgmValueText != null)
            bgmValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void UpdateSFXText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // ─── PANEL AÇMA/KAPAMA ────────────────────────────────────────

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}