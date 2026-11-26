using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections;
using System.Collections.Generic;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

#if UNITY_ANDROID
  #if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Google'ın herkese açık Android test reklam birimleri (yalnızca editör / development build).
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    private const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
    private const string REWARDED_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/5354046379";
  #else
    private const string REWARDED_ID = "ca-app-pub-6613024242187744/7789293047";
    private const string INTERSTITIAL_ID = "ca-app-pub-6613024242187744/2545089668";
    private const string REWARDED_INTERSTITIAL_ID = "ca-app-pub-6613024242187744/8771707777";
  #endif
#else
    private const string REWARDED_ID              = "unused";
    private const string INTERSTITIAL_ID          = "unused";
    private const string REWARDED_INTERSTITIAL_ID = "unused";
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Test cihazı ID'leri (AdMob TestDeviceIds ve UMP TestDeviceHashedIds için ortak liste).
    // Cihaz ID'si, logcat'te UMP'nin yazdığı "addTestDeviceHashedId(...)" satırından alınır.
    private static readonly List<string> TEST_DEVICE_IDS = new List<string>
    {
        // "CIHAZ_ID_BURAYA",
    };
#endif

#if DEVELOPMENT_BUILD
    // true: UMP rıza akışı cihaz AB'deymiş (EEA) gibi test edilir. Yalnızca development build'de derlenir.
    private static readonly bool DEBUG_FORCE_EEA = false;
#endif

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private RewardedInterstitialAd rewardedInterstitialAd;
    private bool isInitialized = false;

    // ─── INTERSTITIAL SIKILIK ─────────────────────────────────────
    private const string KEY_LEVEL_COUNT = "ad_level_count";
    private const int INTERSTITIAL_EVERY = 3;
    private const float MIN_INTERSTITIAL_INTERVAL = 180f;
    private float lastInterstitialTime = -999f;

    // ─── REWARDED INTERSTITIAL SIKILIK ────────────────────────────
    private const string KEY_LAST_REWARDED_INT = "last_rewarded_int_time";
    private const int REWARDED_INT_EVERY = 3;
    private const float MIN_REWARDED_INT_INTERVAL = 180f;

    // ─── GÖSTERİM DURUMU ──────────────────────────────────────────
    // Gösterim başladıktan sonra SDK'dan hiç sonuç gelmezse bu süre sonunda gösterim bitmiş sayılır.
    private const float AD_RESULT_TIMEOUT = 90f;
    // Reklam ödülsüz kapandıysa, geç gelebilecek ödül callback'i için en fazla bu kadar beklenir.
    private const float LATE_REWARD_GRACE = 1f;

    private bool isShowingRewarded = false;
    private bool isShowingInterstitial = false;
    private bool isShowingRewardedInterstitial = false;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsInterstitialReady => interstitialAd != null && interstitialAd.CanShowAd();
    public bool IsRewardedInterstitialReady => rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RequestConsentInfoUpdate();
    }

    // ─── UMP / GDPR ───────────────────────────────────────────────

    private void RequestConsentInfoUpdate()
    {
        ConsentRequestParameters requestParameters = new ConsentRequestParameters();

#if DEVELOPMENT_BUILD
        if (DEBUG_FORCE_EEA)
        {
            requestParameters.ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA,
                TestDeviceHashedIds = TEST_DEVICE_IDS
            };
        }
#endif

        ConsentInformation.Update(requestParameters, (FormError updateError) =>
        {
            if (updateError != null)
            {
                Debug.LogError("Consent update error: " + updateError.Message);
                Initialize();
                return;
            }

            Initialize();

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
            {
                if (showError != null)
                    Debug.LogError("Consent form error: " + showError.Message);

                Initialize();
            });
        });
    }

    public void ShowPrivacyOptionsForm()
    {
        ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
        {
            if (showError != null)
                Debug.LogError("Privacy form error: " + showError.Message);
        });
    }

    // ─── BAŞLATMA ─────────────────────────────────────────────────

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
  #if DEVELOPMENT_BUILD
        Debug.Log(DEBUG_FORCE_EEA ? "[AdManager] TEST ADS ACTIVE (UMP debug: EEA)" : "[AdManager] TEST ADS ACTIVE");
  #else
        Debug.Log("[AdManager] TEST ADS ACTIVE");
  #endif
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            TestDeviceIds = TEST_DEVICE_IDS
        });
#endif

        MobileAds.Initialize(initStatus =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadRewarded();
                LoadInterstitial();
                LoadRewardedInterstitial();
            });
        });
    }

    // ─── REWARDED ─────────────────────────────────────────────────

    private void LoadRewarded()
    {
        if (rewardedAd != null) { rewardedAd.Destroy(); rewardedAd = null; }

        RewardedAd.Load(REWARDED_ID, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null) return;
            rewardedAd = ad;
        });
    }

    // Her gösterimde onSuccess ya da onFail'den tam olarak biri, bir kez çağrılır.
    public void ShowRewarded(Action onSuccess, Action onFail = null, string adPlacement = "powerup")
    {
        // Gösterim sürerken gelen ikinci istek reddedilir; ilk isteğin sonucu etkilenmez.
        if (isShowingRewarded)
        {
            onFail?.Invoke();
            return;
        }

        if (!IsRewardedReady)
        {
            onFail?.Invoke();
            LoadRewarded();
            return;
        }

        isShowingRewarded = true;

        RewardedAd ad = rewardedAd;
        bool settled = false;
        bool finished = false;
        bool rewardEarned = false;
        Coroutine timeoutRoutine = null;

        void Succeed()
        {
            if (settled) return;
            settled = true;
            onSuccess?.Invoke();
        }

        void Fail()
        {
            if (settled) return;
            settled = true;
            onFail?.Invoke();
        }

        // Gösterimi kapatır: zaman aşımını durdurur, abonelikleri kaldırır, bayrağı temizler, yeni reklam yükler.
        void Finish()
        {
            if (finished) return;
            finished = true;
            if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
            ad.OnAdFullScreenContentClosed -= OnClosed;
            ad.OnAdFullScreenContentFailed -= OnFailed;
            isShowingRewarded = false;
            LoadRewarded();
        }

        void OnReward()
        {
            if (rewardEarned) return;
            rewardEarned = true;
            FirebaseManager.Instance?.LogAdWatched(
                "rewarded",
                adPlacement,
                LevelSelectManager.SelectedLevel?.levelID ?? 0);
            Succeed();
        }

        void OnClosed()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (finished) return;
                if (rewardEarned) { Finish(); return; }
                StartCoroutine(WaitForLateReward());
            });
        }

        void OnFailed(AdError error)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (finished) return;
                Finish();
                Fail();
            });
        }

        IEnumerator WaitForLateReward()
        {
            float end = Time.realtimeSinceStartup + LATE_REWARD_GRACE;
            while (!rewardEarned && Time.realtimeSinceStartup < end)
                yield return null;

            if (finished) yield break;
            Finish();
            if (!rewardEarned) Fail();
        }

        IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(AD_RESULT_TIMEOUT);
            timeoutRoutine = null;
            if (finished) yield break;
            Finish();
            Fail();
        }

        ad.OnAdFullScreenContentClosed += OnClosed;
        ad.OnAdFullScreenContentFailed += OnFailed;
        timeoutRoutine = StartCoroutine(Timeout());

        try
        {
            ad.Show(reward => MobileAdsEventExecutor.ExecuteInUpdate(OnReward));
        }
        catch (Exception e)
        {
            Debug.LogError("Rewarded show error: " + e.Message);
            Finish();
            Fail();
        }
    }

    // ─── INTERSTITIAL ─────────────────────────────────────────────

    private void LoadInterstitial()
    {
        if (interstitialAd != null) { interstitialAd.Destroy(); interstitialAd = null; }

        InterstitialAd.Load(INTERSTITIAL_ID, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null) return;
            interstitialAd = ad;
        });
    }

    public void ShowInterstitialIfReady(Action onClosed = null)
    {
        // Gösterim sürerken gelen istek yok sayılır; ilk isteğin kapanış callback'i korunur.
        if (isShowingInterstitial) return;

        int count = PlayerPrefs.GetInt(KEY_LEVEL_COUNT, 0) + 1;
        PlayerPrefs.SetInt(KEY_LEVEL_COUNT, count);
        PlayerPrefs.Save();

        bool countReady = count % INTERSTITIAL_EVERY == 0;
        bool timeReady = Time.realtimeSinceStartup - lastInterstitialTime > MIN_INTERSTITIAL_INTERVAL;

        if (!countReady || !timeReady || !IsInterstitialReady)
        {
            onClosed?.Invoke();
            return;
        }

        isShowingInterstitial = true;

        InterstitialAd ad = interstitialAd;
        bool finished = false;
        Coroutine timeoutRoutine = null;

        // Gösterimi kapatır (kapandı / gösterilemedi / zaman aşımı) ve kapanış callback'ini bir kez çağırır.
        void Finish()
        {
            if (finished) return;
            finished = true;
            if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
            ad.OnAdFullScreenContentClosed -= OnClosed;
            ad.OnAdFullScreenContentFailed -= OnFailed;
            isShowingInterstitial = false;
            onClosed?.Invoke();
            LoadInterstitial();
        }

        void OnClosed() => MobileAdsEventExecutor.ExecuteInUpdate(Finish);

        void OnFailed(AdError error) => MobileAdsEventExecutor.ExecuteInUpdate(Finish);

        IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(AD_RESULT_TIMEOUT);
            timeoutRoutine = null;
            Finish();
        }

        ad.OnAdFullScreenContentClosed += OnClosed;
        ad.OnAdFullScreenContentFailed += OnFailed;
        timeoutRoutine = StartCoroutine(Timeout());

        lastInterstitialTime = Time.realtimeSinceStartup;
        FirebaseManager.Instance?.LogAdWatched(
            "interstitial",
            "level_transition",
            LevelSelectManager.SelectedLevel?.levelID ?? 0);

        try
        {
            ad.Show();
        }
        catch (Exception e)
        {
            Debug.LogError("Interstitial show error: " + e.Message);
            Finish();
        }
    }

    // ─── REWARDED INTERSTITIAL ────────────────────────────────────

    private void LoadRewardedInterstitial()
    {
        if (rewardedInterstitialAd != null) { rewardedInterstitialAd.Destroy(); rewardedInterstitialAd = null; }

        RewardedInterstitialAd.Load(REWARDED_INTERSTITIAL_ID, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null) return;
            rewardedInterstitialAd = ad;
        });
    }

    public void ShowRewardedInterstitialIfReady(Action onSuccess = null, Action onSkipped = null, string adPlacement = "dc_start")
    {
        // Gösterim sürerken gelen istek yok sayılır; ilk isteğin callback'i korunur.
        if (isShowingRewardedInterstitial) return;

        int count = PlayerPrefs.GetInt(KEY_LEVEL_COUNT, 0);
        float lastTime = PlayerPrefs.GetFloat(KEY_LAST_REWARDED_INT, -999f);

        bool countReady = count % REWARDED_INT_EVERY == 0;
        bool timeReady = Time.realtimeSinceStartup - lastTime > MIN_REWARDED_INT_INTERVAL;

        if (!countReady || !timeReady || !IsRewardedInterstitialReady)
        {
            onSkipped?.Invoke();
            return;
        }

        PlayerPrefs.SetFloat(KEY_LAST_REWARDED_INT, Time.realtimeSinceStartup);
        PlayerPrefs.Save();

        isShowingRewardedInterstitial = true;

        RewardedInterstitialAd ad = rewardedInterstitialAd;
        bool settled = false;
        bool finished = false;
        bool rewardEarned = false;
        Coroutine timeoutRoutine = null;

        void Succeed()
        {
            if (settled) return;
            settled = true;
            onSuccess?.Invoke();
        }

        void Fail()
        {
            if (settled) return;
            settled = true;
            onSkipped?.Invoke();
        }

        // Gösterimi kapatır: zaman aşımını durdurur, abonelikleri kaldırır, bayrağı temizler, yeni reklam yükler.
        void Finish()
        {
            if (finished) return;
            finished = true;
            if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
            ad.OnAdFullScreenContentClosed -= OnClosed;
            ad.OnAdFullScreenContentFailed -= OnFailed;
            isShowingRewardedInterstitial = false;
            LoadRewardedInterstitial();
        }

        void OnReward()
        {
            rewardEarned = true;
            FirebaseManager.Instance?.LogAdWatched(
                "rewarded_interstitial",
                adPlacement,
                LevelSelectManager.SelectedLevel?.levelID ?? 0);
        }

        void OnClosed()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (finished) return;
                if (rewardEarned) { Finish(); Succeed(); return; }
                StartCoroutine(WaitForLateReward());
            });
        }

        void OnFailed(AdError error)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (finished) return;
                Finish();
                Fail();
            });
        }

        IEnumerator WaitForLateReward()
        {
            float end = Time.realtimeSinceStartup + LATE_REWARD_GRACE;
            while (!rewardEarned && Time.realtimeSinceStartup < end)
                yield return null;

            if (finished) yield break;
            Finish();
            if (rewardEarned) Succeed(); else Fail();
        }

        IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(AD_RESULT_TIMEOUT);
            timeoutRoutine = null;
            if (finished) yield break;
            Finish();
            Fail();
        }

        ad.OnAdFullScreenContentClosed += OnClosed;
        ad.OnAdFullScreenContentFailed += OnFailed;
        timeoutRoutine = StartCoroutine(Timeout());

        try
        {
            ad.Show(reward => MobileAdsEventExecutor.ExecuteInUpdate(OnReward));
        }
        catch (Exception e)
        {
            Debug.LogError("Rewarded interstitial show error: " + e.Message);
            Finish();
            Fail();
        }
    }
}