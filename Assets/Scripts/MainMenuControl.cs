using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject quitPanel;

    private void Start()
    {
        if (quitPanel != null) quitPanel.SetActive(false);
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