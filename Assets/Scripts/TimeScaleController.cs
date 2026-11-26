using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// AUDIT 1.18: Time.timeScale'in tek sahibi. Her duraklatma sebebi ayrı tutulur;
// en az bir sebep aktifse timeScale 0, hiçbiri yoksa 1.
public enum TimeScaleReason
{
    Pause,          // Pause menüsü
    GameEnd,        // Kazanma / kayıp (second chance ile bırakılır)
    Tutorial,       // İlk seviye tutorial'ı
    Popup,          // AdRewardPopup (power-up, second chance, seviye kilidi, bilgi)
    TimeThiefInfo   // Zaman Hırsızı ilk karşılaşma popup'ı
}

public static class TimeScaleController
{
    private static readonly HashSet<TimeScaleReason> activeReasons = new HashSet<TimeScaleReason>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        activeReasons.Clear();

        // Domain reload kapalıyken çift abonelik olmaması için önce çıkılır.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Apply();
    }

    // Yeni sahnede önceki sahneden kalan sebepler geçersizdir (unutulan bir Release dahil).
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        ClearAndApply();
    }

    public static void Request(TimeScaleReason reason)
    {
        if (activeReasons.Add(reason))
            Apply();
    }

    public static void Release(TimeScaleReason reason)
    {
        if (activeReasons.Remove(reason))
            Apply();
    }

    public static bool IsPaused(TimeScaleReason reason) => activeReasons.Contains(reason);

    // Yalnızca sahne çıkış noktalarında kullanılır (NextOrRetryWin, RestartGame, LoadMainMenu):
    // geçiş sırasındaki reklamlar ve callback'ler normal zamanla çalışsın.
    public static void ResetAll() => ClearAndApply();

    private static void ClearAndApply()
    {
        bool changed = activeReasons.Count > 0;
        activeReasons.Clear();
        Apply(changed);
    }

    private static void Apply(bool logChange = true)
    {
        Time.timeScale = activeReasons.Count > 0 ? 0f : 1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logChange)
            Debug.Log($"[TimeScale] {(activeReasons.Count > 0 ? string.Join(", ", activeReasons) : "none")} → {Time.timeScale}");
#endif
    }
}
