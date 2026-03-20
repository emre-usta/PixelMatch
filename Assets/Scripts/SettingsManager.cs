using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_BGM = "settings_bgm";
    private const string KEY_SFX = "settings_sfx";

    [Header("UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Kayıtlı değerleri yükle
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

        // AudioManager'a uygula
        AudioManager.Instance?.SetBGMVolume(bgm);
        AudioManager.Instance?.SetSFXVolume(sfx);

        // Panel başlangıçta kapalı
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ─── SLIDER HANDLERS ──────────────────────────────────────────

    private void OnBGMChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
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