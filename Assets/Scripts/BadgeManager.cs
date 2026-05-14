using UnityEngine;
using TMPro;

public class BadgeManager : MonoBehaviour
{
    public static BadgeManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtBadge;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => UpdateBadge();

    public void UpdateBadge()
    {
        if (txtBadge == null) return;
        int totalStars = GetTotalStars();
        txtBadge.text = GetTitle(totalStars);
        txtBadge.color = GetTitleColor(totalStars);
    }

    public int GetTotalStars() => PlayerPrefs.GetInt("total_stars", 0);

    public void AddStars(int amount)
    {
        int current = PlayerPrefs.GetInt("total_stars", 0);
        PlayerPrefs.SetInt("total_stars", current + amount);
        PlayerPrefs.Save();
    }

    private string GetTitle(int stars)
    {
        if (stars >= 120) return LocalizationManager.Get("badge_legend");
        if (stars >= 80) return LocalizationManager.Get("badge_master");
        if (stars >= 45) return LocalizationManager.Get("badge_explorer");
        if (stars >= 15) return LocalizationManager.Get("badge_novice");
        return LocalizationManager.Get("badge_rookie");
    }

    private Color GetTitleColor(int stars)
    {
        if (stars >= 120) return new Color(0.32f, 0.93f, 0.51f);
        if (stars >= 80) return new Color(0.96f, 0.65f, 0.14f);
        if (stars >= 45) return new Color(0.27f, 0.67f, 1.00f);
        if (stars >= 15) return new Color(0.98f, 0.78f, 0.46f);
        return new Color(0.67f, 0.67f, 0.67f);
    }
}