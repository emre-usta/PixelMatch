using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

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

        // Adet metinleri
        if (txtRevealCount) txtRevealCount.text = revealCount.ToString();
        if (txtHintCount) txtHintCount.text = hintCount.ToString();
        if (txtFreezeCount) txtFreezeCount.text = freezeCount.ToString();

        // 0 ise buton gri
        if (btnReveal) btnReveal.interactable = revealCount > 0;
        if (btnHint) btnHint.interactable = hintCount > 0;
        if (btnFreeze) btnFreeze.interactable = freezeCount > 0;
    }

    public void ShowFreezeFeedback()
    {
        if (freezeFeedbackPanel != null)
            freezeFeedbackPanel.SetActive(true);
        Invoke(nameof(HideFreezeFeedback), 5f);
    }

    public void HideFreezeFeedback()
    {
        if (freezeFeedbackPanel != null)
            freezeFeedbackPanel.SetActive(false);
    }
}