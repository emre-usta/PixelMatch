using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PixelMatch — Ana Menü Kontrolcüsü
/// Buton baðlantýlarý ve sahne geçiþlerini yönetir.
/// </summary>
public class MainMenuControl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject quitPanel;

    private void Start()
    {
        // Panel baþlangýçta kapalý
        if (quitPanel != null)
            quitPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void OnFreeModeClicked()
    {
        SceneManager.LoadScene("FreeMode");
    }

    // Çýkýþ butonuna basýnca popup aç
    public void OnExitClicked()
    {
        if (quitPanel != null)
            quitPanel.SetActive(true);
    }

    // Popup'ta EVET'e basýnca
    public void OnQuitConfirmed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Popup'ta HAYIR'a basýnca
    public void OnQuitCancelled()
    {
        if (quitPanel != null)
            quitPanel.SetActive(false);
    }
}