using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { TR, EN }
    public static Language CurrentLanguage { get; private set; } = Language.EN;

    private const string KEY_LANGUAGE = "settings_language";
    private static Dictionary<string, string> strings = new();

    // ─── STATİK CONSTRUCTOR — sahne yüklenmeden önce çalışır ─────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // Dili en başta yükle — herhangi bir sahne açılmadan önce
        if (PlayerPrefs.HasKey(KEY_LANGUAGE))
        {
            string saved = PlayerPrefs.GetString(KEY_LANGUAGE, "EN");
            CurrentLanguage = saved == "TR" ? Language.TR : Language.EN;
        }
        else
        {
            string systemLang = Application.systemLanguage.ToString();
            CurrentLanguage = systemLang == "Turkish" ? Language.TR : Language.EN;
            PlayerPrefs.SetString(KEY_LANGUAGE, CurrentLanguage.ToString());
            PlayerPrefs.Save();
        }

        LoadStrings();
        Debug.Log($"[LocalizationManager] Başlatıldı: {CurrentLanguage}");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        PlayerPrefs.SetString(KEY_LANGUAGE, lang.ToString());
        PlayerPrefs.Save();
        LoadStrings();

        // inactive objeler dahil tüm LocalizedText'leri güncelle
        var allTexts = FindObjectsOfType<LocalizedText>(true);
        foreach (var t in allTexts)
            t.UpdateText();

        // DC UI'ını da güncelle
        DailyChallengeManager.Instance?.UpdateUI();

        // Badge güncelle
        BadgeManager.Instance?.UpdateBadge();

        // LevelSelect güncelle — sadece LevelSelect sahnesindeyse
        if (LevelSelectManager.Instance != null &&
            LevelSelectManager.Instance.gameObject.scene.isLoaded)
        {
            LevelSelectManager.Instance.LoadCategory(0);
            LevelSelectManager.Instance.UpdatePowerUpUI();
        }

        // FreeMode güncelle — sadece FreeMode sahnesindeyse
        if (FreeModeManager.Instance != null &&
            FreeModeManager.Instance.gameObject.scene.isLoaded)
        {
            FreeModeManager.Instance.UpdateButtonLabels();
        }

        Debug.Log($"[LocalizationManager] Dil: {lang}");
    }

    public static string Get(string key)
    {
        if (strings.Count == 0)
            LoadStrings();

        if (strings.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[LocalizationManager] Key bulunamadı: {key}");
        return key;
    }

    private static void LoadStrings()
    {
        strings.Clear();
        if (CurrentLanguage == Language.TR)
            LoadTR();
        else
            LoadEN();
    }

    // ─── TÜRKÇE ───────────────────────────────────────────────────
    private static void LoadTR()
    {
        // Ana Menü
        strings["app_name"] = "PIXEL MATCH";
        strings["btn_play"] = "OYNA";
        strings["btn_free_mode"] = "SERBEST MOD";
        strings["btn_quit"] = "ÇIKIŞ";
        strings["quit_title"] = "ÇIKIŞ";
        strings["quit_message"] = "Oyundan çıkmak\nistediğine emin\nmisin?";
        strings["btn_yes"] = "EVET";
        strings["btn_no"] = "HAYIR";
        strings["btn_sub"] = "Hikaye Modu";
        strings["btn_fm_sub"] = "Kendi gridini seç";

        // Ayarlar
        strings["settings_title"] = "AYARLAR";
        strings["settings_music"] = "MÜZİK";
        strings["settings_sfx"] = "SES EFEKTİ";
        strings["settings_language"] = "DİL";
        strings["btn_close"] = "KAPAT";

        // Level Select
        strings["level_select_title"] = "SEVİYE SEÇ";
        strings["level_gallery"] = "BÖLÜMLER";
        strings["progress"] = "İLERLEME";
        strings["btn_animals"] = "HAYVANLAR";
        strings["btn_food"] = "YİYECEKLER";
        strings["btn_nature"] = "DOĞA";
        strings["locked"] = "KİLİTLİ";
        strings["new_tag"] = "YENİ!";
        strings["powerup_peek"] = "GÖZETLE";
        strings["powerup_hint"] = "İPUCU";
        strings["powerup_freeze"] = "DONDUR";

        // Game Mode Select
        strings["select_mode"] = "MOD SEÇ";
        strings["classic_title"] = "KLASİK";
        strings["classic_desc"] = "Süre bitmeden tüm\nkartları eşleştir.";
        strings["classic_avail"] = "LVL 1-10 AKTİF";
        strings["movement_title"] = "HAMLE";
        strings["movement_desc"] = "Sınırlı hamle hakkı.\nHer hamle önemli.";
        strings["movement_avail"] = "LVL 1-10 AKTİF";

        // Daily Challenge Mode Select
        strings["dc_title"] = "GÜNLÜK GÖREV";
        strings["dc_play"] = "OYNA";
        strings["difficulty_easy"] = "KOLAY";
        strings["difficulty_medium"] = "ORTA";
        strings["difficulty_hard"] = "ZOR";
        strings["dc_completed"] = "TAMAMLANDI";
        strings["dc_failed"] = "Bugün başarısız oldun.\nYarın tekrar dene!";
        strings["dc_play_hint"] = "Oynamak için tıkla!";

        // HUD
        strings["hud_time"] = "SÜRE";
        strings["hud_moves"] = "HAMLE";
        strings["hud_restart"] = "YENİDEN";

        // Pause
        strings["pause_title"] = "DURAKLATILDI";
        strings["btn_resume"] = "DEVAM ET";
        strings["btn_restart"] = "YENİDEN BAŞLAT";
        strings["btn_settings"] = "AYARLAR";
        strings["btn_main_menu"] = "ANA MENÜ";

        // Win
        strings["you_win"] = "KAZANDIN!";
        strings["mission_accomplished"] = "GÖREV TAMAMLANDI";
        strings["remaining_time"] = "KALAN SÜRE";
        strings["used_moves"] = "KULLANILAN HAMLE";
        strings["performance"] = "PERFORMANS";
        strings["perf_excellent"] = "MÜKEMMEL!";
        strings["perf_good"] = "İYİ!";
        strings["perf_complete"] = "TAMAMLANDI";
        strings["new_record"] = "YENİ REKOR!";
        strings["btn_next_mission"] = "SONRAKI GÖREV";
        strings["total_stars"] = "TOPLAM";
        strings["reward_title"] = "KAZANDIĞIN ÖDÜLLER";

        // Game Over
        strings["game_over"] = "OYUN BİTTİ";
        strings["out_of_time"] = "SÜRE BİTTİ";
        strings["out_of_moves"] = "HAMLE HAKKIN BİTTİ";
        strings["tiles_remaining"] = "KALAN KART";
        strings["btn_retry"] = "TEKRAR";
        strings["btn_menu"] = "MENÜ";
        strings["dc_no_attempts"] = "HAKKIN DOLDU";

        // Serbest Mod
        strings["free_mode_title"] = "SERBEST MOD";
        strings["category_label"] = "KATEGORİ";
        strings["grid_label"] = "GRİD";
        strings["difficulty_label"] = "ZORLUK";
        strings["mode_label"] = "MOD";
        strings["difficulty_easy"] = "KOLAY";
        strings["difficulty_medium"] = "ORTA";
        strings["difficulty_hard"] = "ZOR";
        strings["mode_classic"] = "KLASİK";
        strings["mode_move"] = "HAMLE";
        strings["time_label"] = "SÜRE";
        strings["move_label"] = "HAMLE";
        strings["btn_play_start"] = "OYNA";
        strings["Animals"] = "HAYVANLAR";
        strings["Foods"] = "YİYECEKLER";
        strings["Natures"] = "DOĞA";

        // Efektler
        strings["time_thief_title"] = "ZAMAN HIRSIZI";
        strings["move_thief_title"] = "HAMLE HIRSIZI";
        strings["time_thief_desc"] = "Bu kart yanlış eşleştirilirse\n<color=#FFB4AB>-5 SANİYE</color> ceza alırsın!";
        strings["move_thief_desc"] = "Bu kart yanlış eşleştirilirse\n<color=#FFB4AB>-3 HAMLE</color> ceza alırsın!";
        strings["btn_understood"] = "ANLADIM";
        strings["freeze_banner"] = "SÜRE DONDURULDU";
        strings["freeze_sub"] = "5 SANİYE";
        strings["banner_time_thief"] = "ZAMAN HIRSIZI!";
        strings["banner_move_thief"] = "HAMLE HIRSIZI!";

        // Badge
        strings["badge_rookie"] = "ÇAYLAK";
        strings["badge_novice"] = "ACEMİ";
        strings["badge_explorer"] = "KAŞİF";
        strings["badge_master"] = "USTA";
        strings["badge_legend"] = "EFSANE";
    }

    // ─── İNGİLİZCE ───────────────────────────────────────────────
    private static void LoadEN()
    {
        // Ana Menü
        strings["app_name"] = "PIXEL MATCH";
        strings["btn_play"] = "PLAY";
        strings["btn_free_mode"] = "FREE MODE";
        strings["btn_quit"] = "QUIT";
        strings["quit_title"] = "QUIT";
        strings["quit_message"] = "Are you sure you\nwant to quit?";
        strings["btn_yes"] = "YES";
        strings["btn_no"] = "NO";
        strings["btn_sub"] = "Story Mode";
        strings["btn_fm_sub"] = "Choose your own grid";

        // Ayarlar
        strings["settings_title"] = "SETTINGS";
        strings["settings_music"] = "MUSIC";
        strings["settings_sfx"] = "SFX";
        strings["settings_language"] = "LANGUAGE";
        strings["btn_close"] = "CLOSE";

        // Level Select
        strings["level_select_title"] = "SELECT LEVEL";
        strings["level_gallery"] = "LEVEL GALLERY";
        strings["progress"] = "PROGRESS";
        strings["btn_animals"] = "ANIMALS";
        strings["btn_food"] = "FOOD";
        strings["btn_nature"] = "NATURE";
        strings["locked"] = "LOCKED";
        strings["new_tag"] = "NEW!";
        strings["powerup_peek"] = "PEEK";
        strings["powerup_hint"] = "HINT";
        strings["powerup_freeze"] = "FREEZE";

        // Game Mode Select
        strings["select_mode"] = "SELECT MODE";
        strings["classic_title"] = "CLASSIC";
        strings["classic_desc"] = "Match all tiles before\nthe timer runs out.";
        strings["classic_avail"] = "LVL 1-10 AVAILABLE";
        strings["movement_title"] = "MOVEMENT";
        strings["movement_desc"] = "Limited number of flips.\nEvery move counts.";
        strings["movement_avail"] = "LVL 1-10 AVAILABLE";

        // Daily Challenge Mode Select
        strings["dc_title"] = "DAILY CHALLENGE";
        strings["dc_play"] = "PLAY";
        strings["difficulty_easy"] = "EASY";
        strings["difficulty_medium"] = "MEDIUM";
        strings["difficulty_hard"] = "HARD";
        strings["dc_completed"] = "COMPLETED ✓";
        strings["dc_failed"] = "Failed today.\nTry again tomorrow!";
        strings["dc_play_hint"] = "Tap to play!";
        // HUD
        strings["hud_time"] = "TIME";
        strings["hud_moves"] = "MOVES";
        strings["hud_restart"] = "RESTART";

        // Pause
        strings["pause_title"] = "PAUSED";
        strings["btn_resume"] = "RESUME";
        strings["btn_restart"] = "RESTART";
        strings["btn_settings"] = "SETTINGS";
        strings["btn_main_menu"] = "MAIN MENU";

        // Win
        strings["you_win"] = "YOU WIN!";
        strings["mission_accomplished"] = "MISSION ACCOMPLISHED";
        strings["remaining_time"] = "TIME REMAINING";
        strings["used_moves"] = "MOVES USED";
        strings["performance"] = "PERFORMANCE";
        strings["perf_excellent"] = "EXCELLENT!";
        strings["perf_good"] = "GOOD!";
        strings["perf_complete"] = "COMPLETE";
        strings["new_record"] = "NEW RECORD!";
        strings["btn_next_mission"] = "NEXT MISSION";
        strings["total_stars"] = "TOTAL";
        strings["reward_title"] = "REWARDS EARNED";

        // Game Over
        strings["game_over"] = "GAME OVER";
        strings["out_of_time"] = "OUT OF TIME";
        strings["out_of_moves"] = "OUT OF MOVES";
        strings["tiles_remaining"] = "TILES REMAINING";
        strings["btn_retry"] = "RETRY";
        strings["btn_menu"] = "MENU";
        strings["dc_no_attempts"] = "NO ATTEMPTS LEFT";

        // Serbest Mod
        strings["free_mode_title"] = "FREE MODE";
        strings["category_label"] = "CATEGORY";
        strings["grid_label"] = "GRID";
        strings["difficulty_label"] = "DIFFICULTY";
        strings["mode_label"] = "MODE";
        strings["difficulty_easy"] = "EASY";
        strings["difficulty_medium"] = "MEDIUM";
        strings["difficulty_hard"] = "HARD";
        strings["mode_classic"] = "CLASSIC";
        strings["mode_move"] = "MOVEMENT";
        strings["time_label"] = "TIME";
        strings["move_label"] = "MOVES";
        strings["btn_play_start"] = "PLAY";
        strings["Animals"] = "ANIMALS";
        strings["Foods"] = "FOODS";
        strings["Natures"] = "NATURES";

        // Efektler
        strings["time_thief_title"] = "TIME THIEF";
        strings["move_thief_title"] = "MOVE THIEF";
        strings["time_thief_desc"] = "Wrong match with this card\ncosts <color=#FFB4AB>-5 SECONDS</color>!";
        strings["move_thief_desc"] = "Wrong match with this card\ncosts <color=#FFB4AB>-3 MOVES</color>!";
        strings["btn_understood"] = "GOT IT";
        strings["freeze_banner"] = "TIME FROZEN";
        strings["freeze_sub"] = "5 SECONDS";
        strings["banner_time_thief"] = "TIME THIEF!";
        strings["banner_move_thief"] = "MOVE THIEF!";

        // Badge
        strings["badge_rookie"] = "ROOKIE";
        strings["badge_novice"] = "NOVICE";
        strings["badge_explorer"] = "EXPLORER";
        strings["badge_master"] = "MASTER";
        strings["badge_legend"] = "LEGEND";
    }
}