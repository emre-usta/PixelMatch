using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    private void Start()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
    }
    
    public GameObject CikisPanel;
    public void ExitGame()
    {
        CikisPanel.SetActive(true);
    }
    
    public void Reply(string reply)
    {
        if (reply == "Exit")
        {
            Application.Quit();
        }
        else
        {
            CikisPanel.SetActive(false);
        }
    }
    
    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }
}
