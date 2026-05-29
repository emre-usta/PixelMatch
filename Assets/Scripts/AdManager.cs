using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

#if UNITY_ANDROID
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    private const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
#else
    private const string REWARDED_ID     = "unused";
    private const string INTERSTITIAL_ID = "unused";
#endif

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;

    private const string KEY_LEVEL_COUNT = "ad_level_count";
    private const int INTERSTITIAL_EVERY = 3;
    private const float MIN_INTERSTITIAL_INTERVAL = 180f;
    private float lastInterstitialTime = -999f;

    private Action onRewardEarned;
    private Action onRewardFailed;
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
        Initialize();
    }

    // ─── BAŞLATMA ─────────────────────────────────────────────────

    private void Initialize()
    {
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

        onRewardEarned = onSuccess;
        onRewardFailed = onFail;

        rewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed += OnRewardedFailed;

        rewardedAd.Show(reward =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                onRewardEarned?.Invoke();
                onRewardEarned = null;
            });
        });
    }

    private void OnRewardedClosed()
    {
        rewardedAd.OnAdFullScreenContentClosed -= OnRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed -= OnRewardedFailed;
        LoadRewarded();
    }

    private void OnRewardedFailed(AdError error)
    {
        rewardedAd.OnAdFullScreenContentClosed -= OnRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed -= OnRewardedFailed;
        onRewardFailed?.Invoke();
        onRewardFailed = null;
        LoadRewarded();
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