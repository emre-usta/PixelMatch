using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject quitPanel;

    private const string KEY_STARTER = "starter_powerups_given";

    private void Start()
    {
        if (quitPanel != null) quitPanel.SetActive(false);
        GiveStarterPowerUps();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ayarlar açýksa kapat
            if (SettingsManager.Instance != null &&
                SettingsManager.Instance.IsSettingsOpen)
            {
                SettingsManager.Instance.CloseSettings();
                return;
            }

            // Quit popup açýksa kapat
            if (quitPanel != null && quitPanel.activeSelf)
            {
                OnQuitCancelled();
                return;
            }

            // Hiçbiri açýk deðilse exit popup aç
            OnExitClicked();
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

    public void OnPlayClicked() => SceneManager.LoadScene("LevelSelect");
    public void OnFreeModeClicked() => SceneManager.LoadScene("FreeMode");

    public void OnExitClicked()
    {
        if (quitPanel != null) quitPanel.SetActive(true);
    }

    public void OnQuitConfirmed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnQuitCancelled()
    {
        if (quitPanel != null) quitPanel.SetActive(false);
    }
}