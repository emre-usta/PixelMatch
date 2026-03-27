using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_BGM = "settings_bgm";
    private const string KEY_SFX = "settings_sfx";

    [Header("UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private GameObject settingsPanel;

    [Header("Value Texts")]
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, 1f);

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

        AudioManager.Instance?.SetBGMVolume(bgm);
        AudioManager.Instance?.SetSFXVolume(sfx);

        // Başlangıç değerlerini göster
        UpdateBGMText(bgm);
        UpdateSFXText(sfx);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ─── SLIDER HANDLERS ──────────────────────────────────────────

    private void OnBGMChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();
        UpdateBGMText(value);
    }

    private void OnSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
        UpdateSFXText(value);
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