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
    [SerializeField] private TextMeshProUGUI txtStepIndicator;

    private int currentStep = 0;
    private readonly int totalSteps = 4;
    private bool isActive = false;

    private readonly string[] stepKeys = {
        "tutorial_step1",
        "tutorial_step2",
        "tutorial_step3",
        "tutorial_step4"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        bool isDone = PlayerPrefs.GetInt(KEY_TUTORIAL_DONE, 0) == 1;
        bool isFirstLevel = LevelSelectManager.SelectedLevel != null &&
                            LevelSelectManager.SelectedLevel.levelID == 0;

        if (isDone || !isFirstLevel)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        btnAction?.onClick.AddListener(OnActionButtonClicked);
        ShowTutorial();
    }

    // ─── TUTORIAL BAŞLAT ──────────────────────────────────────────

    private void ShowTutorial()
    {
        isActive = true;
        currentStep = 0;
        Time.timeScale = 0f;

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        UpdateUI();
    }

    // ─── EKRANA TIKLAMA ───────────────────────────────────────────

    public void OnScreenTapped()
    {
        if (!isActive) return;

        // Son adımda ekrana tıklama engelle
        if (currentStep >= totalSteps - 1) return;

        currentStep++;
        UpdateUI();
    }

    private void OnActionButtonClicked()
    {
        if (!isActive) return;
        CompleteTutorial();
    }

    // ─── UI GÜNCELLEME ────────────────────────────────────────────

    private void UpdateUI()
    {
        // Tutorial metni
        if (txtTutorial != null)
            txtTutorial.text = LocalizationManager.Get(stepKeys[currentStep]);

        // Adım göstergesi — sadece son adımda gizle
        if (txtStepIndicator != null)
        {
            txtStepIndicator.gameObject.SetActive(currentStep < totalSteps - 1);
            txtStepIndicator.text = $"{currentStep + 1} / {totalSteps}";
        }

        // Başla butonu — sadece son adımda görünsün
        if (btnAction != null)
        {
            btnAction.gameObject.SetActive(currentStep == totalSteps - 1);
            if (txtActionButton != null)
                txtActionButton.text = LocalizationManager.Get("tutorial_start");
        }
    }

    // ─── TAMAMLA ──────────────────────────────────────────────────

    private void CompleteTutorial()
    {
        isActive = false;
        PlayerPrefs.SetInt(KEY_TUTORIAL_DONE, 1);
        PlayerPrefs.Save();

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        Time.timeScale = 1f;
        GameStateManager.Instance?.OnGridReady();

        Debug.Log("[TutorialManager] Tutorial tamamlandı, oyun başlıyor.");
    }
}