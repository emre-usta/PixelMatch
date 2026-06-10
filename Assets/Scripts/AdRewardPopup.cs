using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdRewardPopup : MonoBehaviour
{
    public static AdRewardPopup Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject popupPanel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtWatchButton;
    [SerializeField] private TextMeshProUGUI txtDeclineButton;
    [SerializeField] private Button btnWatch;
    [SerializeField] private Button btnDecline;

    private System.Action onWatched;
    private System.Action onDeclined;

    private bool secondChanceUsedThisSession = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        secondChanceUsedThisSession = false;

        btnWatch?.onClick.AddListener(OnWatchClicked);
        btnDecline?.onClick.AddListener(OnDeclineClicked);

        if (popupPanel != null) popupPanel.SetActive(false);
    }

    // ─── POWERUP ──────────────────────────────────────────────────

    public void ShowForPowerUp(PowerUpType type, System.Action onSuccess)
    {
        string powerUpName = type switch
        {
            PowerUpType.XRay => LocalizationManager.Get("powerup_peek"),
            PowerUpType.MindFreeze => LocalizationManager.Get("powerup_hint"),
            PowerUpType.TimeFreeze => LocalizationManager.Get("powerup_freeze"),
            _ => "?"
        };

        onWatched = onSuccess;
        onDeclined = null;

        if (btnDecline != null) btnDecline.gameObject.SetActive(true);

        SetupAndShow(
            LocalizationManager.Get("ad_powerup_title"),
            $"{LocalizationManager.Get("ad_powerup_desc")} {powerUpName}",
            LocalizationManager.Get("ad_watch"),
            LocalizationManager.Get("ad_decline")
        );
    }

    // ─── İKİNCİ ŞANS ─────────────────────────────────────────────

    public void ShowForSecondChance(System.Action onSuccess, System.Action onDecline)
    {
        // Bu denemede zaten ikinci şans kullanıldıysa direkt game over
        if (secondChanceUsedThisSession)
        {
            onDecline?.Invoke();
            return;
        }

        bool isMoveMode = LevelSelectManager.SelectedMode == LevelSelectManager.GameMode.Move;

        onWatched = () =>
        {
            secondChanceUsedThisSession = true;
            onSuccess?.Invoke();
        };
        onDeclined = onDecline;

        if (btnDecline != null) btnDecline.gameObject.SetActive(true);

        SetupAndShow(
            LocalizationManager.Get("ad_second_chance_title"),
            isMoveMode
                ? LocalizationManager.Get("ad_second_chance_move")
                : LocalizationManager.Get("ad_second_chance_time"),
            LocalizationManager.Get("ad_watch"),
            LocalizationManager.Get("ad_decline")
        );
    }

    // ─── KİLİTLİ LEVEL ───────────────────────────────────────────

    public void ShowForLockedLevel(int categoryID, int levelID, System.Action onSuccess)
    {
        onWatched = () =>
        {
            PlayerPrefs.SetInt($"ad_unlocked_{categoryID}_{levelID}", 1);
            PlayerPrefs.Save();
            onSuccess?.Invoke();
        };
        onDeclined = null;

        if (btnDecline != null) btnDecline.gameObject.SetActive(true);

        SetupAndShow(
            LocalizationManager.Get("ad_unlock_title"),
            LocalizationManager.Get("ad_unlock_desc"),
            LocalizationManager.Get("ad_watch"),
            LocalizationManager.Get("ad_decline")
        );
    }

    // ─── BİLGİLENDİRME ───────────────────────────────────────────

    public void ShowInfo(string title, string description)
    {
        onWatched = null;
        onDeclined = null;

        if (btnDecline != null) btnDecline.gameObject.SetActive(false);

        SetupAndShow(
            title,
            description,
            LocalizationManager.Get("btn_ok"),
            ""
        );
    }

    // ─── YARDIMCI ─────────────────────────────────────────────────

    private void SetupAndShow(string title, string desc, string watchBtn, string declineBtn)
    {
        if (txtTitle != null) txtTitle.text = title;
        if (txtDescription != null) txtDescription.text = desc;
        if (txtWatchButton != null) txtWatchButton.text = watchBtn;
        if (txtDeclineButton != null) txtDeclineButton.text = declineBtn;

        if (popupPanel != null) popupPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (btnDecline != null) btnDecline.gameObject.SetActive(true);
        Time.timeScale = 1f;
    }

    private void OnWatchClicked()
    {
        Hide();

        if (onWatched == null) return;

        if (AdManager.Instance == null || !AdManager.Instance.IsRewardedReady)
        {
            onWatched?.Invoke();
            onWatched = null;
            return;
        }

        AdManager.Instance.ShowRewarded(
            onSuccess: () => { onWatched?.Invoke(); onWatched = null; },
            onFail: () => { onDeclined?.Invoke(); onDeclined = null; }
        );
    }

    private void OnDeclineClicked()
    {
        Hide();
        onDeclined?.Invoke();
        onDeclined = null;
    }
}