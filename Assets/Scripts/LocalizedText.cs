using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    private TextMeshProUGUI tmp;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        UpdateText();
    }

    private void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();

        if (tmp == null) return;

        if (string.IsNullOrEmpty(localizationKey)) return;

        tmp.text = LocalizationManager.Get(localizationKey);
    }
}