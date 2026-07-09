using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

public class InAppUpdateManager : MonoBehaviour
{
    public static InAppUpdateManager Instance { get; private set; }

#if UNITY_ANDROID
    private AppUpdateManager _appUpdateManager;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(CheckForUpdate());
#else
        Debug.Log("[InAppUpdate] Skipped in Editor.");
#endif
    }

    // ─── GÜNCELLEME KONTROL ───────────────────────────────────────

#if UNITY_ANDROID
    private IEnumerator CheckForUpdate()
    {
        _appUpdateManager = new AppUpdateManager();

        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation =
            _appUpdateManager.GetAppUpdateInfo();

        yield return appUpdateInfoOperation;

        if (appUpdateInfoOperation.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppUpdate] Update info request failed: {appUpdateInfoOperation.Error}");
            yield break;
        }

        AppUpdateInfo appUpdateInfo = appUpdateInfoOperation.GetResult();

        Debug.Log($"[InAppUpdate] Update availability: {appUpdateInfo.UpdateAvailability}");

        if (appUpdateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable)
        {
            Debug.Log("[InAppUpdate] Update available, launching flexible update.");
            StartCoroutine(StartFlexibleUpdate(appUpdateInfo));
        }
        else
        {
            Debug.Log("[InAppUpdate] No update available.");
        }
    }

    // ─── FLEXIBLE UPDATE BAŞLAT ───────────────────────────────────

    private IEnumerator StartFlexibleUpdate(AppUpdateInfo appUpdateInfo)
    {
        var startUpdateRequest = _appUpdateManager.StartUpdate(
            appUpdateInfo,
            AppUpdateOptions.FlexibleAppUpdateOptions()
        );

        yield return startUpdateRequest;

        if (startUpdateRequest.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppUpdate] Start update failed: {startUpdateRequest.Error}");
            yield break;
        }

        StartCoroutine(WaitForDownloadComplete());
    }

    // ─── İNDİRME TAMAMLANDI ───────────────────────────────────────

    private IEnumerator WaitForDownloadComplete()
    {
        while (true)
        {
            PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> statusOperation =
                _appUpdateManager.GetAppUpdateInfo();

            yield return statusOperation;

            if (statusOperation.Error != AppUpdateErrorCode.NoError)
                yield break;

            AppUpdateInfo info = statusOperation.GetResult();

            if (info.UpdateAvailability == UpdateAvailability.UpdateNotAvailable)
            {
                Debug.Log("[InAppUpdate] Download complete, completing update.");
                _appUpdateManager.CompleteUpdate();
                yield break;
            }

            yield return new WaitForSeconds(3f);
        }
    }
#endif
}