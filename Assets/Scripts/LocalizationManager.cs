using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { TR, EN }
    public static Language CurrentLanguage { get; private set; } = Language.EN;

    private const string KEY_LANGUAGE = "settings_language";
    private static Dictionary<string, string> strings = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
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

        var allTexts = FindObjectsOfType<LocalizedText>(true);
        foreach (var t in allTexts)
            t.UpdateText();

        DailyChallengeManager.Instance?.UpdateUI();
        BadgeManager.Instance?.UpdateBadge();

        if (LevelSelectManager.Instance != null &&
            LevelSelectManager.Instance.gameObject.scene.isLoaded)
        {
            LevelSelectManager.Instance.LoadCategory(0);
            LevelSelectManager.Instance.UpdatePowerUpUI();
        }

        if (FreeModeManager.Instance != null &&
            FreeModeManager.Instance.gameObject.scene.isLoaded)
        {
            FreeModeManager.Instance.UpdateButtonLabels();
        }
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
        strings["ad_unlock_hint"] = "Reklam ile Aç";
        strings["powerup_peek"] = "GÖZETLE";
        strings["powerup_hint"] = "İPUCU";
        strings["powerup_freeze"] = "DONDUR";

        // Ads
        strings["ad_powerup_title"] = "GÜÇLENME KAZAN!";
        strings["ad_powerup_desc"] = "Reklam izle, 1 adet kazan:";
        strings["ad_second_chance_title"] = "İKİNCİ ŞANS!";
        strings["ad_second_chance_time"] = "Reklam izle, +30 saniye kazan!";
        strings["ad_second_chance_move"] = "Reklam izle, +10 hamle kazan!";
        strings["ad_unlock_title"] = "SEVİYEYİ AÇ!";
        strings["ad_unlock_desc"] = "Reklam izleyerek bu seviyeyi kalıcı aç!";
        strings["ad_watch"] = "REKLAMI İZLE";
        strings["ad_decline"] = "HAYIR, TEŞEKKÜRLER";
        strings["btn_ok"] = "TAMAM";
        strings["ad_locked_title"] = "SEVİYE KİLİTLİ!";
        strings["ad_locked_desc"] = "Reklam ile açtığın seviyeyi\ntamamlamadan yeni seviye açamazsın.";


        // Game Mode Select
        strings["select_mode"] = "MOD SEÇ";
        strings["classic_title"] = "KLASİK";
        strings["classic_desc"] = "Süre bitmeden tüm\nkartları eşleştir.";
        strings["classic_avail"] = "LVL 1-10 AKTİF";
        strings["movement_title"] = "HAMLE";
        strings["movement_desc"] = "Sınırlı hamle hakkı.\nHer hamle önemli.";
        strings["movement_avail"] = "LVL 1-10 AKTİF";

        // Daily Challenge
        strings["dc_title"] = "GÜNLÜK GÖREV";
        strings["dc_play"] = "OYNA";
        strings["dc_completed"] = "TAMAMLANDI";
        strings["dc_failed"] = "Yarın \ntekrar dene!";
        strings["dc_play_hint"] = "Oynamak için \ndokun!";

        // Zorluk
        strings["difficulty_easy"] = "KOLAY";
        strings["difficulty_medium"] = "ORTA";
        strings["difficulty_hard"] = "ZOR";

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
        strings["new_record"] = "YENİ REKOR!";
        strings["btn_next_mission"] = "SONRAKI GÖREV";
        strings["reward_title"] = "KAZANDIĞIN ÖDÜLLER";
        strings["perf_perfect"] = "MÜKEMMEL!";
        strings["perf_good"] = "İYİ!";
        strings["perf_completed"] = "TAMAMLANDI";
        strings["stat_moves_used"] = "KULLANILAN HAMLE";
        strings["stat_time_remaining"] = "KALAN SÜRE";
        strings["stat_performance"] = "PERFORMANS";
        strings["total_stars"] = "TOPLAM";

        // Game Over
        strings["game_over"] = "OYUN BİTTİ";
        strings["out_of_time"] = "SÜRE BİTTİ";
        strings["out_of_moves"] = "HAMLE HAKKIN BİTTİ";
        strings["tiles_remaining"] = "KALAN KART";
        strings["btn_retry"] = "TEKRAR";
        strings["btn_menu"] = "MENÜ";
        strings["dc_no_attempts"] = "HAKKIN DOLDU";
        strings["completion_label"] = "TAMAMLANMA";

        // Tutorial
        strings["tutorial_step1"] = "Kartları bulmak için\nüzerlerine dokun!";
        strings["tutorial_step2"] = "Aynı kartları eşleştirerek\ntüm çiftleri bul!";
        strings["tutorial_step3"] = "Art arda eşleştirerek\nCOMBO kazan ve bonus al!";
        strings["tutorial_step4"] = "Power-up'ları kullanarak\navantaj kazan!";
        strings["tutorial_step5"] = "Kafatası kartı yanlış\neşleştirilirse ceza alırsın!";
        strings["tutorial_step6"] = "Hızlı bitirirsen daha fazla\nyıldız ve ödül kazanırsın!";
        strings["tutorial_step7"] = "KLASİK modda süreyle,\nHAMLE modunda sınırlı hamleyle oyna!";
        strings["tutorial_step8"] = "Hazır mısın? Tüm çiftleri\nbul ve kazan!";
        strings["tutorial_start"] = "BAŞLA";
        strings["tutorial_title"] = "NASIL OYNANIR";
        strings["tutorial_next"] = "DEVAM";
        strings["tutorial_tap_hint"] = "DEVAM ETMEK İÇİN DOKUN";

        // Serbest Mod
        strings["free_mode_title"] = "SERBEST MOD";
        strings["category_label"] = "KATEGORİ";
        strings["grid_label"] = "GRİD";
        strings["difficulty_label"] = "ZORLUK";
        strings["mode_label"] = "MOD";
        strings["mode_classic"] = "KLASİK";
        strings["mode_move"] = "HAMLE";
        strings["time_label"] = "SÜRE";
        strings["move_label"] = "HAMLE";
        strings["btn_play_start"] = "OYNA";

        // Kategori isimleri
        strings["Animals"] = "HAYVANLAR";
        strings["Foods"] = "YİYECEKLER";
        strings["Natures"] = "DOĞA";
        strings["Space"] = "UZAY";
        strings["Legends"] = "EFSANELER"; // ← düzeltildi

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
        strings["combo_moves"] = "HAMLE";
        strings["combo_seconds"] = "SN";

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
        strings["ad_unlock_hint"] = "Watch Ad";
        strings["powerup_peek"] = "PEEK";
        strings["powerup_hint"] = "HINT";
        strings["powerup_freeze"] = "FREEZE";

        // Ads
        strings["ad_powerup_title"] = "EARN A POWER-UP!";
        strings["ad_powerup_desc"] = "Watch an ad to earn 1:";
        strings["ad_second_chance_title"] = "SECOND CHANCE!";
        strings["ad_second_chance_time"] = "Watch ad, earn +30 seconds!";
        strings["ad_second_chance_move"] = "Watch ad, earn +10 moves!";
        strings["ad_unlock_title"] = "UNLOCK LEVEL!";
        strings["ad_unlock_desc"] = "Watch an ad to permanently unlock!";
        strings["ad_watch"] = "WATCH AD";
        strings["ad_decline"] = "NO, THANKS";
        strings["btn_ok"] = "OK";
        strings["ad_locked_title"] = "LEVEL LOCKED!";
        strings["ad_locked_desc"] = "Complete the level you unlocked\nwith an ad before opening a new one.";

        // Game Mode Select
        strings["select_mode"] = "SELECT MODE";
        strings["classic_title"] = "CLASSIC";
        strings["classic_desc"] = "Match all tiles before\nthe timer runs out.";
        strings["classic_avail"] = "LVL 1-10 AVAILABLE";
        strings["movement_title"] = "MOVEMENT";
        strings["movement_desc"] = "Limited number of flips.\nEvery move counts.";
        strings["movement_avail"] = "LVL 1-10 AVAILABLE";

        // Daily Challenge
        strings["dc_title"] = "DAILY CHALLENGE";
        strings["dc_play"] = "PLAY";
        strings["dc_completed"] = "COMPLETED";
        strings["dc_failed"] = "Try again \ntomorrow!";
        strings["dc_play_hint"] = "Tap to \nplay!";

        // Zorluk
        strings["difficulty_easy"] = "EASY";
        strings["difficulty_medium"] = "MEDIUM";
        strings["difficulty_hard"] = "HARD";

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
        strings["new_record"] = "NEW RECORD!";
        strings["btn_next_mission"] = "NEXT MISSION";
        strings["reward_title"] = "REWARDS EARNED";
        strings["perf_perfect"] = "PERFECT!";
        strings["perf_good"] = "GOOD!";
        strings["perf_completed"] = "COMPLETED";
        strings["stat_moves_used"] = "MOVES USED";
        strings["stat_time_remaining"] = "TIME LEFT";
        strings["stat_performance"] = "PERFORMANCE";
        strings["total_stars"] = "TOTAL";

        // Game Over
        strings["game_over"] = "GAME OVER";
        strings["out_of_time"] = "OUT OF TIME";
        strings["out_of_moves"] = "OUT OF MOVES";
        strings["tiles_remaining"] = "TILES REMAINING";
        strings["btn_retry"] = "RETRY";
        strings["btn_menu"] = "MENU";
        strings["dc_no_attempts"] = "NO ATTEMPTS LEFT";
        strings["completion_label"] = "COMPLETION";

        // Tutorial
        strings["tutorial_step1"] = "Tap on cards\nto find matches!";
        strings["tutorial_step2"] = "Match the same cards\nto find all pairs!";
        strings["tutorial_step3"] = "Match in a row\nto earn a COMBO bonus!";
        strings["tutorial_step4"] = "Use power-ups\nto gain an advantage!";
        strings["tutorial_step5"] = "Wrong match with skull card\nearns a penalty!";
        strings["tutorial_step6"] = "Finish faster to earn\nmore stars and rewards!";
        strings["tutorial_step7"] = "CLASSIC mode uses a timer,\nMOVE mode uses limited moves!";
        strings["tutorial_step8"] = "Ready? Find all pairs\nand win!";
        strings["tutorial_start"] = "START";
        strings["tutorial_title"] = "HOW TO PLAY";
        strings["tutorial_next"] = "NEXT";
        strings["tutorial_tap_hint"] = "TAP TO CONTINUE";

        // Serbest Mod
        strings["free_mode_title"] = "FREE MODE";
        strings["category_label"] = "CATEGORY";
        strings["grid_label"] = "GRID";
        strings["difficulty_label"] = "DIFFICULTY";
        strings["mode_label"] = "MODE";
        strings["mode_classic"] = "CLASSIC";
        strings["mode_move"] = "MOVEMENT";
        strings["time_label"] = "TIME";
        strings["move_label"] = "MOVES";
        strings["btn_play_start"] = "PLAY";

        // Kategori isimleri
        strings["Animals"] = "ANIMALS";
        strings["Foods"] = "FOODS";
        strings["Natures"] = "NATURES";
        strings["Space"] = "SPACE";
        strings["Legends"] = "LEGENDS"; // ← düzeltildi

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
        strings["combo_moves"] = "MOVES";
        strings["combo_seconds"] = "SEC";

        // Badge
        strings["badge_rookie"] = "ROOKIE";
        strings["badge_novice"] = "NOVICE";
        strings["badge_explorer"] = "EXPLORER";
        strings["badge_master"] = "MASTER";
        strings["badge_legend"] = "LEGEND";
    }
}