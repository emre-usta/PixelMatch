#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Yalnızca editör: reklam akışlarını gerçek reklam olmadan test etmek için menü komutları.
public static class AdDebugMenu
{
    [MenuItem("PixelMatch/Debug Ads/Next rewarded: close without reward")]
    private static void NextRewardedCloseWithoutReward()
    {
        // SessionState, Play moduna girerken yapılan domain reload'dan etkilenmez.
        SessionState.SetBool(AdManager.DebugRewardedNoRewardKey, true);
        Debug.Log("[AdDebugMenu] Bir sonraki ShowRewarded çağrısı 'ödülsüz kapandı' olarak simüle edilecek (tek seferlik).");
    }
}
#endif
