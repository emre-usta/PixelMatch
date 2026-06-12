using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

#if UNITY_ANDROID
    private const string REWARDED_ID = "ca-app-pub-6613024242187744/7789293047";
    private const string INTERSTITIAL_ID = "ca-app-pub-6613024242187744/2545089668";
#else
    private const string REWARDED_ID     = "unused";
    private const string INTERSTITIAL_ID = "unused";
#endif

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private bool isInitialized = false;

    private const string KEY_LEVEL_COUNT = "ad_level_count";
    private const int INTERSTITIAL_EVERY = 3;
    private const float MIN_INTERSTITIAL_INTERVAL = 180f;
    private float lastInterstitialTime = -999f;

    private Action onInterstitialClosed;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsInterstitialReady => interstitialAd != null && interstitialAd.CanShowAd();

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

        MobileAds.Initialize(initStatus =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadRewarded();
                LoadInterstitial();
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

    public void ShowRewarded(Action onSuccess, Action onFail = null)
    {
        if (!IsRewardedReady)
        {
            onFail?.Invoke();
            LoadRewarded();
            return;
        }

        // Local capture → global state sorunu yok
        Action localSuccess = onSuccess;
        Action localFail = onFail;

        void OnClosed()
        {
            rewardedAd.OnAdFullScreenContentClosed -= OnClosed;
            rewardedAd.OnAdFullScreenContentFailed -= OnFailed;
            LoadRewarded();
        }

        void OnFailed(AdError error)
        {
            rewardedAd.OnAdFullScreenContentClosed -= OnClosed;
            rewardedAd.OnAdFullScreenContentFailed -= OnFailed;
            localFail?.Invoke();
            localFail = null;
            LoadRewarded();
        }

        rewardedAd.OnAdFullScreenContentClosed += OnClosed;
        rewardedAd.OnAdFullScreenContentFailed += OnFailed;

        rewardedAd.Show(reward =>
        {
            localSuccess?.Invoke();
            localSuccess = null;
        });
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
        onInterstitialClosed = onClosed;

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

        interstitialAd.OnAdFullScreenContentClosed += OnInterstitialClosed;
        interstitialAd.OnAdFullScreenContentFailed += OnInterstitialFailed;

        lastInterstitialTime = Time.realtimeSinceStartup;
        interstitialAd.Show();
    }

    private void OnInterstitialClosed()
    {
        interstitialAd.OnAdFullScreenContentClosed -= OnInterstitialClosed;
        interstitialAd.OnAdFullScreenContentFailed -= OnInterstitialFailed;
        onInterstitialClosed?.Invoke();
        onInterstitialClosed = null;
        LoadInterstitial();
    }

    private void OnInterstitialFailed(AdError error)
    {
        interstitialAd.OnAdFullScreenContentClosed -= OnInterstitialClosed;
        interstitialAd.OnAdFullScreenContentFailed -= OnInterstitialFailed;
        onInterstitialClosed?.Invoke();
        onInterstitialClosed = null;
        LoadInterstitial();
    }
}