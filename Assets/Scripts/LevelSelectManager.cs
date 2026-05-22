using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LevelSelectManager : MonoBehaviour
{
    public enum GameMode { Classic, Move }

    public static GameMode SelectedMode { get; set; }
    public static LevelSelectManager Instance { get; private set; }

    [Header("Kategoriler")]
    [SerializeField] private CategoryConfig[] categories;
    [SerializeField] private GameObject[] categoryButtons;

    [Header("Levellar")]
    [SerializeField] private Transform levelContainer;
    [SerializeField] private GameObject levelCardPrefab;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtProgress;
    [SerializeField] private TextMeshProUGUI txtScrollArrow;

    [Header("Power-up UI")]
    [SerializeField] private TextMeshProUGUI txtPeekCount;
    [SerializeField] private TextMeshProUGUI txtHintCount;
    [SerializeField] private TextMeshProUGUI txtFreezeCount;

    private readonly Color colorCatActive = new Color(0.10f, 0.23f, 0.06f);
    private readonly Color colorCatInactive = new Color(0.05f, 0.07f, 0.09f);
    private readonly Color colorBorderActive = new Color(0.27f, 0.80f, 0.40f);
    private readonly Color colorBorderInactive = new Color(0.16f, 0.19f, 0.25f);
    private readonly Color colorTextActive = new Color(0.27f, 0.80f, 0.40f);
    private readonly Color colorTextInactive = new Color(0.23f, 0.29f, 0.35f);

    private CategoryConfig selectedCategory;
    private int selectedCategoryIndex = 0;

    public static LevelConfig SelectedLevel { get; private set; }
    public static int SelectedCategoryID { get; private set; }
    public static int SelectedCategoryLevelCount { get; private set; }
    public static int TotalCategoryCount =>
        Instance != null && Instance.categories != null ? Instance.categories.Length : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticState()
    {
        SelectedLevel = null;
        SelectedCategoryID = 0;
        SelectedCategoryLevelCount = 0;
        SelectedMode = GameMode.Classic;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (categories == null || categories.Length == 0) return;

        UpdatePowerUpUI();
        LoadCategory(SelectedCategoryID);

        if (txtScrollArrow != null)
            StartCoroutine(PulseScrollArrow());
    }

    // ─── SCROLL ARROW PULSE ───────────────────────────────────────

    private IEnumerator PulseScrollArrow()
    {
        while (true)
        {
            float elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.3f, 1f, elapsed / 0.6f);
                txtScrollArrow.color = new Color(0.27f, 0.80f, 0.40f, alpha);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0.3f, elapsed / 0.6f);
                txtScrollArrow.color = new Color(0.27f, 0.80f, 0.40f, alpha);
                yield return null;
            }
        }
    }

    // ─── KATEGORİ YÜKLEMESİ ──────────────────────────────────────

    public void LoadCategory(int index)
    {
        if (index < 0 || index >= categories.Length) return;

        selectedCategoryIndex = index;
        selectedCategory = categories[index];

        UpdateCategoryButtons(index);
        UpdateProgress(index);

        foreach (Transform child in levelContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < selectedCategory.levels.Length; i++)
        {
            LevelConfig level = selectedCategory.levels[i];

            bool isUnlocked = LevelProgressManager.Instance != null
                ? LevelProgressManager.Instance.IsLevelUnlocked(selectedCategory.categoryID, i)
                : i == 0;

            bool adUnlockedCheck = PlayerPrefs.GetInt(
                $"ad_unlocked_{selectedCategory.categoryID}_{i}", 0) == 1;
            if (adUnlockedCheck) isUnlocked = true;

            bool isNew = isUnlocked &&
                i > 0 &&
                LevelProgressManager.Instance != null &&
                LevelProgressManager.Instance.GetBestStars(selectedCategory.categoryID, i) == 0 &&
                (
                    LevelProgressManager.Instance.GetBestStars(selectedCategory.categoryID, i - 1) > 0 ||
                    PlayerPrefs.GetInt($"ad_unlocked_{selectedCategory.categoryID}_{i - 1}", 0) == 1 ||
                    adUnlockedCheck
                );

            CreateLevelCard(level, i, isUnlocked, isNew);
        }
    }

    public void OnCategoryClicked(int index) => LoadCategory(index);

    // ─── KATEGORİ BUTON GÜNCELLEMESİ ─────────────────────────────

    private void UpdateCategoryButtons(int activeIndex)
    {
        if (categoryButtons == null) return;

        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;

            bool isActive = (i == activeIndex);

            Image bg = categoryButtons[i].GetComponent<Image>();
            if (bg != null) bg.color = isActive ? colorCatActive : colorCatInactive;

            string[] borderNames = { "Border_Top", "Border_Bottom", "Border_Left", "Border_Right" };
            foreach (string borderName in borderNames)
            {
                Transform border = categoryButtons[i].transform.Find(borderName);
                if (border != null)
                {
                    Image borderImg = border.GetComponent<Image>();
                    if (borderImg != null)
                        borderImg.color = isActive ? colorBorderActive : colorBorderInactive;
                }
            }

            TextMeshProUGUI txt = categoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = isActive ? colorTextActive : colorTextInactive;
        }
    }

    // ─── PROGRESS GÜNCELLEMESİ ────────────────────────────────────

    private void UpdateProgress(int categoryIndex)
    {
        if (txtProgress == null || LevelProgressManager.Instance == null) return;

        CategoryConfig cat = categories[categoryIndex];
        int total = cat.levels.Length;
        if (total == 0)
        {
            txtProgress.text = $"{LocalizationManager.Get("progress")}: %0";
            return;
        }

        int completed = 0;
        for (int i = 0; i < total; i++)
            if (LevelProgressManager.Instance.GetBestStars(cat.categoryID, i) > 0)
                completed++;

        int percent = Mathf.RoundToInt((float)completed / total * 100f);
        txtProgress.text = $"{LocalizationManager.Get("progress")}: %{percent}";
    }

    // ─── LEVEL KARTI OLUŞTURMA ────────────────────────────────────

    private void CreateLevelCard(LevelConfig level, int levelIndex, bool isUnlocked, bool isNew)
    {
        GameObject card = Instantiate(levelCardPrefab, levelContainer);

        int bestStars = LevelProgressManager.Instance != null
            ? LevelProgressManager.Instance.GetBestStars(selectedCategory.categoryID, levelIndex)
            : 0;

        Color borderColor = !isUnlocked
            ? new Color(0.12f, 0.16f, 0.19f)
            : isNew
                ? new Color(0.27f, 0.67f, 1.00f)
                : new Color(0.27f, 0.80f, 0.40f);

        string[] borderNames = { "Border_Top", "Border_Bottom", "Border_Left", "Border_Right" };
        foreach (string b in borderNames)
        {
            Transform border = card.transform.Find(b);
            if (border != null)
            {
                Image img = border.GetComponent<Image>();
                if (img != null) img.color = borderColor;
            }
        }

        // Text_LevelName
        Transform levelNameT = card.transform.Find("Panel_CardInfo/Text_LevelName");
        if (levelNameT != null)
        {
            TextMeshProUGUI txt = levelNameT.GetComponent<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"LVL {levelIndex + 1:D2}";
                txt.color = !isUnlocked
                    ? new Color(0.16f, 0.19f, 0.25f)
                    : isNew
                        ? new Color(0.27f, 0.67f, 1.00f)
                        : new Color(0.27f, 0.80f, 0.40f);
            }
        }

        // Text_LevelStat
        Transform levelStatT = card.transform.Find("Panel_CardInfo/Text_LevelStat");
        if (levelStatT != null)
        {
            TextMeshProUGUI stat = levelStatT.GetComponent<TextMeshProUGUI>();
            if (stat != null)
            {
                if (!isUnlocked || bestStars == 0)
                {
                    stat.text = "";
                }
                else
                {
                    string classicKey = $"record_{selectedCategory.categoryID}_{levelIndex}_0";
                    string moveKey = $"record_{selectedCategory.categoryID}_{levelIndex}_1";
                    float classicRecord = PlayerPrefs.GetFloat(classicKey, 0f);
                    int moveRecord = PlayerPrefs.GetInt(moveKey + "_moves", 0);
                    string movesLabel = LocalizationManager.Get("move_label");

                    if (classicRecord > 0f && moveRecord > 0)
                        stat.text = $"{classicRecord:F1}s / {moveRecord}{movesLabel}";
                    else if (classicRecord > 0f)
                        stat.text = $"{classicRecord:F1}s";
                    else if (moveRecord > 0)
                        stat.text = $"{moveRecord} {movesLabel}";
                    else
                        stat.text = "";
                }
                stat.color = new Color(0.67f, 0.67f, 0.67f);
            }
        }

        // Img_LockIcon
        Transform lockIcon = card.transform.Find("Img_LevelArt/Img_LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(!isUnlocked);

        // Img_LevelArt
        Transform levelArt = card.transform.Find("Img_LevelArt");
        if (levelArt != null)
        {
            Transform artImg = levelArt.Find("Img_Art");
            if (artImg != null)
            {
                Image img = artImg.GetComponent<Image>();
                if (img != null)
                {
                    if (isUnlocked && level.cardBackSprite != null)
                    {
                        img.sprite = level.previewSprite;
                        img.gameObject.SetActive(true);
                    }
                    else
                    {
                        img.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Tag_New
        Transform tagNew = card.transform.Find("Img_LevelArt/Tag_New");
        if (tagNew != null)
        {
            Transform textNew = tagNew.Find("Text_New");
            if (textNew != null)
            {
                TextMeshProUGUI newTxt = textNew.GetComponent<TextMeshProUGUI>();
                if (newTxt != null)
                    newTxt.text = LocalizationManager.Get("new_tag");
            }
            tagNew.gameObject.SetActive(isNew);
            if (isNew) StartCoroutine(AnimateNewTag(tagNew.gameObject));
        }

        // Panel_Stars
        Transform starsPanel = card.transform.Find("Panel_Stars");
        if (starsPanel != null)
        {
            for (int s = 0; s < starsPanel.childCount; s++)
            {
                Image starImg = starsPanel.GetChild(s).GetComponent<Image>();
                if (starImg != null)
                    starImg.color = (s < bestStars)
                        ? new Color(0.98f, 0.78f, 0.46f)
                        : new Color(0.16f, 0.13f, 0.06f);
            }
        }

        // Text_Difficulty
        Transform diffT = card.transform.Find("Text_Difficulty");
        if (diffT != null)
        {
            TextMeshProUGUI diff = diffT.GetComponent<TextMeshProUGUI>();
            if (diff != null)
            {
                if (isUnlocked)
                {
                    string diffKey = level.difficulty switch
                    {
                        DifficultyLevel.Easy => "difficulty_easy",
                        DifficultyLevel.Medium => "difficulty_medium",
                        DifficultyLevel.Hard => "difficulty_hard",
                        _ => "difficulty_easy"
                    };
                    diff.text = LocalizationManager.Get(diffKey);
                    diff.color = level.difficulty switch
                    {
                        DifficultyLevel.Easy => new Color(0.59f, 0.77f, 0.35f),
                        DifficultyLevel.Medium => new Color(0.98f, 0.78f, 0.46f),
                        DifficultyLevel.Hard => new Color(0.89f, 0.29f, 0.29f),
                        _ => Color.gray
                    };
                }
                else
                {
                    diff.text = "";
                }
            }
        }

        // Kilitli kart görünümü
        if (!isUnlocked)
        {
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null)
                cardImage.color = new Color(0.05f, 0.04f, 0.03f);
        }

        // Button
        Button btn = card.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            int capturedIndex = levelIndex;

            if (isUnlocked)
            {
                btn.onClick.AddListener(() =>
                    LevelPopupManager.Instance.ShowPopup(level, capturedIndex));
            }
            else
            {
                bool canUnlock = IsNextUnlockableLevel(selectedCategory, levelIndex);

                if (canUnlock)
                {
                    btn.onClick.AddListener(() =>
                    {
                        AdRewardPopup.Instance?.ShowForLockedLevel(
                            selectedCategory.categoryID,
                            capturedIndex,
                            onSuccess: () =>
                            {
                                LevelProgressManager.Instance?.UnlockLevel(
                                    selectedCategory.categoryID, capturedIndex);
                                LevelPopupManager.Instance.ShowPopup(level, capturedIndex);
                                LoadCategory(selectedCategoryIndex);
                            }
                        );
                    });
                }
                else
                {
                    // Tamamlanmamış reklam leveli var mı kontrol et
                    bool hasUnfinishedAdLevel = false;
                    for (int i = 0; i < selectedCategory.levels.Length; i++)
                    {
                        bool adUnlocked = PlayerPrefs.GetInt(
                            $"ad_unlocked_{selectedCategory.categoryID}_{i}", 0) == 1;
                        int stars = LevelProgressManager.Instance?.GetBestStars(
                            selectedCategory.categoryID, i) ?? 0;

                        if (adUnlocked && stars == 0)
                        {
                            hasUnfinishedAdLevel = true;
                            break;
                        }
                    }

                    if (hasUnfinishedAdLevel)
                    {
                        btn.onClick.AddListener(() =>
                            AdRewardPopup.Instance?.ShowInfo(
                                LocalizationManager.Get("ad_locked_title"),
                                LocalizationManager.Get("ad_locked_desc")
                            ));
                    }
                    else
                    {
                        btn.interactable = false;
                    }
                }
            }
        }
    }

    // ─── REKLAM İLE AÇILABİLİR Mİ ───────────────────────────────

    private bool IsNextUnlockableLevel(CategoryConfig category, int levelIndex)
    {
        if (LevelProgressManager.Instance == null) return false;

        int lastUnlockedIndex = -1;
        for (int i = 0; i < category.levels.Length; i++)
        {
            bool unlocked = LevelProgressManager.Instance.IsLevelUnlocked(category.categoryID, i);
            bool adUnlocked = PlayerPrefs.GetInt($"ad_unlocked_{category.categoryID}_{i}", 0) == 1;

            if (unlocked || adUnlocked)
                lastUnlockedIndex = i;
            else
                break;
        }

        if (levelIndex != lastUnlockedIndex + 1) return false;

        for (int i = 0; i <= lastUnlockedIndex; i++)
        {
            bool adUnlocked = PlayerPrefs.GetInt($"ad_unlocked_{category.categoryID}_{i}", 0) == 1;
            int stars = LevelProgressManager.Instance.GetBestStars(category.categoryID, i);

            if (adUnlocked && stars == 0) return false;
        }

        return true;
    }

    // ─── NEW TAG ANİMASYONU ───────────────────────────────────────

    private IEnumerator AnimateNewTag(GameObject tag)
    {
        RectTransform rt = tag.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 originalScale = rt.localScale;
        float elapsed = 0f;
        float duration = 0.8f;

        while (tag != null && tag.activeInHierarchy)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / duration, 1f);
            float scale = Mathf.Lerp(0.85f, 1.15f, t);
            rt.localScale = originalScale * scale;
            yield return null;
        }

        if (rt != null) rt.localScale = originalScale;
    }

    // ─── POWER-UP UI ──────────────────────────────────────────────

    public void UpdatePowerUpUI()
    {
        int peekCount = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.GetCount(PowerUpType.XRay)
            : PlayerPrefs.GetInt("powerup_reveal", 0);

        int hintCount = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.GetCount(PowerUpType.MindFreeze)
            : PlayerPrefs.GetInt("powerup_hint", 0);

        int freezeCount = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.GetCount(PowerUpType.TimeFreeze)
            : PlayerPrefs.GetInt("powerup_freeze", 0);

        if (txtPeekCount != null)
            txtPeekCount.text = $"{LocalizationManager.Get("powerup_peek")}({peekCount})";
        if (txtHintCount != null)
            txtHintCount.text = $"{LocalizationManager.Get("powerup_hint")}({hintCount})";
        if (txtFreezeCount != null)
            txtFreezeCount.text = $"{LocalizationManager.Get("powerup_freeze")}({freezeCount})";
    }

    public void OnPowerUpClicked(int powerUpType)
    {
        PowerUpType type = (PowerUpType)powerUpType;
        AdRewardPopup.Instance?.ShowForPowerUp(type, () =>
        {
            PowerUpManager.Instance?.AddPowerUp(type, 1);
            UpdatePowerUpUI();
        });
    }

    // ─── LEVEL SEÇİMİ ─────────────────────────────────────────────

    public static void SetSelectedLevel(LevelConfig level, int levelIndex)
    {
        SelectedLevel = level;
        SelectedCategoryID = Instance.selectedCategory.categoryID;
        SelectedCategoryLevelCount = Instance.selectedCategory.levels.Length;
    }

    public static bool IsLastLevelInCategory(int levelID)
    {
        if (Instance == null || Instance.categories == null) return false;

        CategoryConfig category = null;
        foreach (var cat in Instance.categories)
            if (cat.categoryID == SelectedCategoryID)
                category = cat;

        if (category == null) return false;
        return levelID >= category.levels.Length - 1;
    }

    public void OnBackClicked() => SceneManager.LoadScene("MainMenu");
}