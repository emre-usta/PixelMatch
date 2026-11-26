using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelModeSelect;
    [SerializeField] private UIPopupController modeSelectPopup;

    private const string KEY_STARTER = "starter_powerups_given";
    private bool isLeavingScene = false;
    public static MainMenuControl Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Ana Menüye her girişte aktif oyun oturumu kapanır.
        GameSession.End();

        if (panelModeSelect != null) panelModeSelect.SetActive(false);
        GiveStarterPowerUps();
        UpdateModeSelectTexts();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingsManager.Instance != null &&
                SettingsManager.Instance.IsSettingsOpen)
            {
                SettingsManager.Instance.CloseSettings();
                return;
            }
            if (panelModeSelect != null && panelModeSelect.activeSelf)
            {
                CloseModeSelect();
                return;
            }
        }
    }

    private void GiveStarterPowerUps()
    {
        if (PlayerPrefs.GetInt(KEY_STARTER, 0) == 1) return;
        PlayerPrefs.SetInt("powerup_reveal", PlayerPrefs.GetInt("powerup_reveal", 0) + 2);
        PlayerPrefs.SetInt("powerup_hint", PlayerPrefs.GetInt("powerup_hint", 0) + 2);
        PlayerPrefs.SetInt("powerup_freeze", PlayerPrefs.GetInt("powerup_freeze", 0) + 2);
        PlayerPrefs.SetInt(KEY_STARTER, 1);
        PlayerPrefs.Save();
    }

    public void UpdateModeSelectTexts()
    {
        if (panelModeSelect == null) return;

        UpdateText("Panel_Content/Upper_Panel/Text_ModeTitle",
                   LocalizationManager.Get("select_mode"));
        UpdateText("Panel_Content/Panel_Body/Panel_StoryMode/Btn_StoryMode/Text_StoryMode_Title",
                   LocalizationManager.Get("mode_story_title"));
        UpdateText("Panel_Content/Panel_Body/Panel_StoryMode/Btn_StoryMode/Text_StoryMode_Desc",
                   LocalizationManager.Get("mode_story_desc"));
        UpdateText("Panel_Content/Panel_Body/Panel_FreeMode/Btn_FreeMode/Text_FreeMode_Title",
                   LocalizationManager.Get("mode_free_title"));
        UpdateText("Panel_Content/Panel_Body/Panel_FreeMode/Btn_FreeMode/Text_FreeMode_Desc",
                   LocalizationManager.Get("mode_free_desc"));
    }

    private void UpdateText(string path, string value)
    {
        Transform t = panelModeSelect.transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning($"[MainMenuControl] Path bulunamadı: {path}");
            return;
        }
        TMPro.TextMeshProUGUI txt = t.GetComponent<TMPro.TextMeshProUGUI>();
        if (txt != null) txt.text = value;
    }

    // ─── OYNA BUTONU ──────────────────────────────────────────────
    public void OnPlayClicked()
    {
        modeSelectPopup?.OpenPopup();
    }

    // ─── MOD SEÇİMİ ───────────────────────────────────────────────
    public void OnStoryModeClicked()
    {
        if (isLeavingScene) return;
        isLeavingScene = true;
        SceneManager.LoadScene("LevelSelect");
    }

    public void OnFreeModeClicked()
    {
        if (isLeavingScene) return;
        isLeavingScene = true;
        SceneManager.LoadScene("FreeMode");
    }

    public void CloseModeSelect()
    {
        modeSelectPopup?.ClosePopup();
    }
}