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

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("Level1");
    }

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