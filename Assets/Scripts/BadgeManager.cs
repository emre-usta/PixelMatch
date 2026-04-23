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

    private void Start()
    {
        UpdateBadge();
    }

    public void UpdateBadge()
    {
        if (txtBadge == null) return;

        int totalStars = GetTotalStars();
        string title = GetTitle(totalStars);
        txtBadge.text = title;

        // Renge göre unvan
        txtBadge.color = GetTitleColor(totalStars);
    }

    public int GetTotalStars()
    {
        return PlayerPrefs.GetInt("total_stars", 0);
    }

    public void AddStars(int amount)
    {
        int current = PlayerPrefs.GetInt("total_stars", 0);
        PlayerPrefs.SetInt("total_stars", current + amount);
        PlayerPrefs.Save();
    }

    private string GetTitle(int stars)
    {
        if (stars >= 100) return "LEGEND";
        if (stars >= 60) return "MASTER";
        if (stars >= 30) return "EXPLORER";
        if (stars >= 6) return "NOVICE";
        return "ROOKIE";
    }

    private Color GetTitleColor(int stars)
    {
        if (stars >= 100) return new Color(0.32f, 0.93f, 0.51f); // #51ED82 yeşil
        if (stars >= 60) return new Color(0.96f, 0.65f, 0.14f); // #F5A623 amber
        if (stars >= 30) return new Color(0.27f, 0.67f, 1.00f); // #44AAFF mavi
        if (stars >= 10) return new Color(0.98f, 0.78f, 0.46f); // #FAC775 sarı
        return new Color(0.67f, 0.67f, 0.67f);                   // #AAAAAA gri
    }
}