using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string KEY_TUTORIAL_DONE = "tutorial_done";

    [Header("UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI txtTutorial;
    [SerializeField] private TextMeshProUGUI txtActionButton;
    [SerializeField] private Button btnAction;
    [SerializeField] private Button btnNext;
    [SerializeField] private TextMeshProUGUI txtStepIndicator;

    [Header("Adım Görselleri")]
    [SerializeField] private Image imgTutorialIcon;
    [SerializeField] private Sprite[] tutorialSprites;

    private int currentStep = 0;
    private readonly int totalSteps = 8;
    private bool isActive = false;

    // Tutorial şu an ekranda mı (ShowTutorial → true, CompleteTutorial → false). Oyun başlatma guard'ı bunu kullanır.
    public bool IsShowing => isActive;

    private readonly string[] stepKeys =
    {
        "tutorial_step1",
        "tutorial_step2",
        "tutorial_step3",
        "tutorial_step4",
        "tutorial_step5",
        "tutorial_step6",
        "tutorial_step7",
        "tutorial_step8"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (GameSession.IsSource(SessionSource.DailyChallenge) ||
            GameSession.IsSource(SessionSource.FreeMode))
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        bool isDone = PlayerPrefs.GetInt(KEY_TUTORIAL_DONE, 0) == 1;
        SessionContext session = GameSession.CurrentOrLegacy();
        bool isFirstLevel = session != null &&
                            session.Source == SessionSource.Story &&
                            session.LevelID == 0;

        if (isDone || !isFirstLevel)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        btnAction?.onClick.AddListener(OnActionButtonClicked);
        ShowTutorial();
    }

    // ─── TUTORIAL ─────────────────────────────────────────────────

    private void ShowTutorial()
    {
        isActive = true;
        currentStep = 0;
        TimeScaleController.Request(TimeScaleReason.Tutorial);

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        UpdateUI();
    }

    public void OnScreenTapped()
    {
        if (!isActive) return;
        if (currentStep >= totalSteps - 1) return;

        currentStep++;
        UpdateUI();
    }

    private void OnActionButtonClicked()
    {
        if (!isActive) return;
        CompleteTutorial();
    }

    private void UpdateUI()
    {
        if (txtTutorial != null)
            txtTutorial.text = LocalizationManager.Get(stepKeys[currentStep]);

        if (imgTutorialIcon != null && tutorialSprites != null &&
            currentStep < tutorialSprites.Length &&
            tutorialSprites[currentStep] != null)
        {
            imgTutorialIcon.sprite = tutorialSprites[currentStep];
            imgTutorialIcon.gameObject.SetActive(true);
        }

        if (txtStepIndicator != null)
        {
            txtStepIndicator.gameObject.SetActive(currentStep < totalSteps - 1);
            txtStepIndicator.text = $"{currentStep + 1} / {totalSteps}";
        }

        if (btnAction != null)
        {
            btnAction.gameObject.SetActive(currentStep == totalSteps - 1);
            if (txtActionButton != null)
                txtActionButton.text = LocalizationManager.Get("tutorial_start");
        }

        if (btnNext != null)
            btnNext.gameObject.SetActive(currentStep < totalSteps - 1);
    }

    private void CompleteTutorial()
    {
        isActive = false;
        PlayerPrefs.SetInt(KEY_TUTORIAL_DONE, 1);
        PlayerPrefs.Save();

        FirebaseManager.Instance?.LogTutorialComplete();

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        TimeScaleController.Release(TimeScaleReason.Tutorial);
        GameStateManager.Instance?.OnTutorialCompleted();
    }
}