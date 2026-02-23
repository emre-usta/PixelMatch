using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public int TargerSuccess;
    int InstantSuccess;
    int selectedNumber;
    
    GameObject selectedButton;
    GameObject currentButton;
    
    public AudioSource[] voices;
    public GameObject[] buttons;
    public TextMeshProUGUI counter;
    public GameObject[] endGamePanels;
    
    public float TotalTime = 120;
    float Minute;
    float Second;
    bool Timer;

    public GameObject Grid;
    public GameObject ObjectPool;
    private bool CreateStatus;
    private int createSize;
    private int totalElementCount;
    
    void Start()
    {
        selectedNumber = 0;
        Timer = true;
        CreateStatus = true;
        createSize = 0;
        totalElementCount = ObjectPool.transform.childCount;

        StartCoroutine(CreateObject());
    }
    
    void Update()
    {
        if (Timer && TotalTime > 1)
        {
            TotalTime -= Time.deltaTime;
            Minute = Mathf.FloorToInt(TotalTime / 60);
            Second = Mathf.FloorToInt(TotalTime % 60);
        
            //counter.text = Mathf.FloorToInt(TotalTime).ToString();
            counter.text = string.Format("{0:00}:{1:00}", Minute, Second);
        }
        else
        {
            counter.text = "Süre Bitti";
            Timer = false;
            GameOver();
        }
        
    }
    
    IEnumerator CreateObject()
    {
        yield return new WaitForSeconds(.1f);

        while (CreateStatus)
        {
            int RandomObject = Random.Range(0, ObjectPool.transform.childCount - 1);

            if (ObjectPool.transform.GetChild(RandomObject).GameObject() != null)
            {
                ObjectPool.transform.GetChild(RandomObject).transform.SetParent(Grid.transform);
                createSize++;

                if (createSize == totalElementCount)
                {
                    CreateStatus = false;
                    Destroy(ObjectPool.gameObject);
                }
            }
            
        }
        
    }

    public Sprite defultSprite;

    public void giveObject(GameObject myObject)
    {
        currentButton = myObject;
        currentButton.GetComponent<Image>().sprite = currentButton.GetComponentInChildren<SpriteRenderer>().sprite;
        currentButton.GetComponent<Image>().raycastTarget = false;
        voices[0].Play();
    }

    void SetButtonsActiveState(bool state)
    {
        foreach(var item in buttons)
        {
            if (item != null)
            {
                item.GetComponent<Image>().raycastTarget = state;
            }
        }
    }

    public void ButtonClicked(int value)
    {
        Controls(value);
    }

    void Controls(int IncomingValue)
    {
        if (selectedNumber == 0)
        {
            selectedNumber = IncomingValue;
            selectedButton = currentButton;
        }
        else
        {
            StartCoroutine(CheckMatchWithDelay(IncomingValue));
        }
    }

    IEnumerator CheckMatchWithDelay(int IncomingValue)
    {
        SetButtonsActiveState(false);
        yield return new WaitForSeconds(1);

        if (selectedNumber == IncomingValue)
        {
            InstantSuccess++;
            selectedButton.GetComponent<Image>().enabled = false;
            currentButton.GetComponent<Image>().enabled = false;
            selectedButton.GetComponent<Button>().enabled= false;
            currentButton.GetComponent<Button>().enabled = false;
            selectedNumber = 0;
            selectedButton = null;
            SetButtonsActiveState(true);

            if (TargerSuccess == InstantSuccess)
            {
                Win();
            }
        }
        else
        {
            voices[1].Play();
            selectedButton.GetComponent<Image>().sprite = defultSprite;
            currentButton.GetComponent<Image>().sprite = defultSprite;
            selectedNumber = 0;
            selectedButton = null;
            SetButtonsActiveState(true);
        }
    }
    
    
    void GameOver()
    {
        endGamePanels[0].SetActive(true);
    }

    public void PlayPause()
    {
        endGamePanels[2].SetActive(true);
        Time.timeScale = 0;
    }
    
    public void PlayContinue()
    {
        endGamePanels[2].SetActive(false);
        Time.timeScale = 1;
    }
    void Win()
    {
        endGamePanels[1].SetActive(true);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
}
