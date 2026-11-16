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

    private const string PRIVACY_POLICY_URL = "https://emre-usta.github.io/privacy-policy";

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

    [Header("Mute İkonları")]
    [SerializeField] private Image imgBGMMuteIcon;
    [SerializeField] private Image imgSFXMuteIcon;
    [SerializeField] private Sprite sprSpeakerOn;
    [SerializeField] private Sprite sprSpeakerOff;

    [Header("Dil Butonları")]
    [SerializeField] private Button btnTR;
    [SerializeField] private Button btnEN;

    [Header("Gizlilik")]
    [SerializeField] private Button btnPrivacy;
    [SerializeField] private Button btnPrivacyPolicy;
    [SerializeField] private GameObject panelPrivacy;

    private bool bgmMuted = false;
    private bool sfxMuted = false;

    private readonly Color colorMuteActive = new Color(1.00f, 0.42f, 0.52f);   // yakut - sessiz
    private readonly Color colorMuteInactive = new Color(0.31f, 0.90f, 0.78f); // zümrüt - açık

    // --- TR RENKLERİ ---
    private readonly Color colorTR_Main = HexToColor("FFFFFF");
    private readonly Color colorTR_Image = HexToColor("E1D5AC");
    private readonly Color colorTR_Image1 = HexToColor("CCA514");

    // --- EN RENKLERİ ---
    private readonly Color colorEN_Main = HexToColor("FFFFFF");
    private readonly Color colorEN_Image = HexToColor("83C3E9");
    private readonly Color colorEN_Image1 = HexToColor("0D527A");

    // --- PASİF (GRİ) RENKLER ---
    private readonly Color colorInactive_Main = HexToColor("8C859E");
    private readonly Color colorInactive_Image = HexToColor("888888");
    private readonly Color colorInactive_Image1 = HexToColor("333333");

    // --- YAZI RENKLERİ ---
    private readonly Color colorTextActive = HexToColor("FFD25A");
    private readonly Color colorTextInactive = HexToColor("C9B892");

    public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

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
        btnPrivacy?.onClick.AddListener(OnPrivacySettingsClicked);
        btnPrivacyPolicy?.onClick.AddListener(OnPrivacyPolicyClicked);

        ApplyBGM(bgm);
        ApplySFX(sfx);
        UpdateBGMText(bgm);
        UpdateSFXText(sfx);
        UpdateMuteButtons();
        UpdateLanguageButtons();
        UpdatePrivacyButton();

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ─── SLIDER ───────────────────────────────────────────────────

    private void OnBGMChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();
        UpdateBGMText(value);
        if (!bgmMuted) AudioManager.Instance?.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
        UpdateSFXText(value);
        if (!sfxMuted) AudioManager.Instance?.SetSFXVolume(value);
    }

    // ─── MUTE ─────────────────────────────────────────────────────

    public void ToggleBGMMute()
    {
        bgmMuted = !bgmMuted;
        PlayerPrefs.SetInt(KEY_BGM_MUTED, bgmMuted ? 1 : 0);
        PlayerPrefs.Save();
        AudioManager.Instance?.SetBGMVolume(bgmMuted ? 0f : PlayerPrefs.GetFloat(KEY_BGM, 0.5f));
        UpdateMuteButtons();
    }

    public void ToggleSFXMute()
    {
        sfxMuted = !sfxMuted;
        PlayerPrefs.SetInt(KEY_SFX_MUTED, sfxMuted ? 1 : 0);
        PlayerPrefs.Save();
        AudioManager.Instance?.SetSFXVolume(sfxMuted ? 0f : PlayerPrefs.GetFloat(KEY_SFX, 1f));
        UpdateMuteButtons();
    }

    private void ApplyBGM(float value) => AudioManager.Instance?.SetBGMVolume(bgmMuted ? 0f : value);
    private void ApplySFX(float value) => AudioManager.Instance?.SetSFXVolume(sfxMuted ? 0f : value);

    private void UpdateMuteButtons()
    {
        SetButtonColor(btnBGMMute, bgmMuted);
        SetButtonColor(btnSFXMute, sfxMuted);

        if (imgBGMMuteIcon != null) imgBGMMuteIcon.sprite = bgmMuted ? sprSpeakerOff : sprSpeakerOn;
        if (imgSFXMuteIcon != null) imgSFXMuteIcon.sprite = sfxMuted ? sprSpeakerOff : sprSpeakerOn;
    }

    private void SetButtonColor(Button btn, bool muted)
    {
        if (btn == null) return;
        Image bg = btn.GetComponent<Image>();
        if (bg != null) bg.color = muted ? colorMuteActive : colorMuteInactive;
    }

    // ─── DİL ──────────────────────────────────────────────────────

    private void SetLanguage(LocalizationManager.Language lang)
    {
        LocalizationManager.Instance?.SetLanguage(lang);
        UpdateLanguageButtons();
    }

    private void UpdateLanguageButtons()
    {
        bool isTR = LocalizationManager.CurrentLanguage == LocalizationManager.Language.TR;

        SetLangButton(btnTR, isTR, isTRButton: true);
        SetLangButton(btnEN, !isTR, isTRButton: false);
    }

    private void SetLangButton(Button btn, bool isActive, bool isTRButton)
    {
        if (btn == null) return;

        // 1. Ana Renk Seçimi
        Color mainColor, imgColor, img1Color;

        if (isActive)
        {
            // Seçili olan butonun kendi özel aktif renkleri
            mainColor = isTRButton ? colorTR_Main : colorEN_Main;
            imgColor = isTRButton ? colorTR_Image : colorEN_Image;
            img1Color = isTRButton ? colorTR_Image1 : colorEN_Image1;
        }
        else
        {
            // Seçili olmayan buton için gri tonları
            mainColor = colorInactive_Main;
            imgColor = colorInactive_Image;
            img1Color = colorInactive_Image1;
        }

        // 2. Ana Buton Image Rengi
        Image mainImg = btn.GetComponent<Image>();
        if (mainImg != null) mainImg.color = mainColor;

        // 3. Çocuk Image Görsellerinin Renkleri (Image ve Image (1))
        Transform imgTransform = btn.transform.Find("Image");
        if (imgTransform != null && imgTransform.TryGetComponent(out Image img))
        {
            img.color = imgColor;
        }

        Transform img1Transform = btn.transform.Find("Image (1)");
        if (img1Transform != null && img1Transform.TryGetComponent(out Image img1))
        {
            img1.color = img1Color;
        }

        // 4. Metin Rengi
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.color = isActive ? colorTextActive : colorTextInactive;
        }
    }

    // Hex Kodunu Color Objesine Çeviren Yardımcı Metot
    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;
        return Color.white;
    }

    // ─── VALUE TEXTS ──────────────────────────────────────────────

    private void UpdateBGMText(float value)
    {
        if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void UpdateSFXText(float value)
    {
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // ─── GİZLİLİK ────────────────────────────────────────────────

    public void OnPrivacySettingsClicked()
    {
        AdManager.Instance?.ShowPrivacyOptionsForm();
    }

    public void OnPrivacyPolicyClicked()
    {
        Application.OpenURL(PRIVACY_POLICY_URL);
    }

    private void UpdatePrivacyButton()
    {
        if (btnPrivacy == null) return;

        bool isRequired = GoogleMobileAds.Ump.Api.ConsentInformation
            .PrivacyOptionsRequirementStatus ==
            GoogleMobileAds.Ump.Api.PrivacyOptionsRequirementStatus.Required;

        btnPrivacy.gameObject.SetActive(isRequired);

        if (panelPrivacy != null)
            panelPrivacy.SetActive(true);
    }

    // ─── PANEL ────────────────────────────────────────────────────

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}