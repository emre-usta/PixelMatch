using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpUI : MonoBehaviour
{
    public static PowerUpUI Instance { get; private set; }

    [Header("Açık Bak")]
    [SerializeField] private Button btnReveal;
    [SerializeField] private TextMeshProUGUI txtRevealCount;

    [Header("İpucu")]
    [SerializeField] private Button btnHint;
    [SerializeField] private TextMeshProUGUI txtHintCount;

    [Header("Süre Dondur")]
    [SerializeField] private Button btnFreeze;
    [SerializeField] private TextMeshProUGUI txtFreezeCount;

    [Header("Freeze Feedback")]
    [SerializeField] private GameObject freezeFeedbackPanel;
    [SerializeField] private TextMeshProUGUI txtFreezeTitle;
    [SerializeField] private TextMeshProUGUI txtFreezeSub;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
        btnReveal?.onClick.AddListener(() => PowerUpManager.Instance?.UseXRay());
        btnHint?.onClick.AddListener(() => PowerUpManager.Instance?.UseHint());
        btnFreeze?.onClick.AddListener(() => PowerUpManager.Instance?.UseTimeFreeze());
    }

    public void UpdateUI()
    {
        if (PowerUpManager.Instance == null) return;

        int revealCount = PowerUpManager.Instance.GetCount(PowerUpType.XRay);
        int hintCount = PowerUpManager.Instance.GetCount(PowerUpType.MindFreeze);
        int freezeCount = PowerUpManager.Instance.GetCount(PowerUpType.TimeFreeze);

        if (txtRevealCount) txtRevealCount.text = revealCount.ToString();
        if (txtHintCount) txtHintCount.text = hintCount.ToString();
        if (txtFreezeCount) txtFreezeCount.text = freezeCount.ToString();

        if (btnReveal) btnReveal.interactable = true;
        if (btnHint) btnHint.interactable = true;
        if (btnFreeze) btnFreeze.interactable = true;
    }

    public void ShowFreezeFeedback()
    {
        if (freezeFeedbackPanel != null) freezeFeedbackPanel.SetActive(true);

        bool isMoveMode = GameSession.IsMoveMode;

        if (txtFreezeTitle != null)
            txtFreezeTitle.text = LocalizationManager.Get("freeze_title");

        if (txtFreezeSub != null)
            txtFreezeSub.text = isMoveMode
                ? LocalizationManager.Get("freeze_moves_feedback")
                : LocalizationManager.Get("freeze_time_feedback");

        if (!isMoveMode)
            Invoke(nameof(HideFreezeFeedback), 5f);
    }

    public void HideFreezeFeedback()
    {
        if (freezeFeedbackPanel != null) freezeFeedbackPanel.SetActive(false);
    }
}