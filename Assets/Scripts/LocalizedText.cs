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
        // tmp null ise tekrar almayý dene
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();

        // Hala null ise sessizce çýk — hata verme
        if (tmp == null) return;

        if (string.IsNullOrEmpty(localizationKey)) return;

        tmp.text = LocalizationManager.Get(localizationKey);
    }
}