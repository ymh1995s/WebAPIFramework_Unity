using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : Singleton<AdsManager>
{
    LevelPlayInterstitialAd _interstitialAd;
    LevelPlayRewardedAd _rewardedAd;
    Action _rewardedCallback;
    bool _initialized; // 중복 Init 방지 플래그

    // 광고 제거 상품 보유 여부 — IAPManager.ApplyNoAdsEntitlement에서 설정
    public bool IsAdsRemoved { get; private set; }

    #region 보상 처리
    public void ShowInterstitialAds()
    {
        // 광고 제거 상품 보유 시 전면 광고 건너뜀 — 사용자가 결제한 경우 광고 표시 안 함
        if (IsAdsRemoved) return;

        _interstitialAd.LoadAd();
    }

    // 광고 제거 상태로 전환 — IAPManager가 보유 확인 후 호출 (PlayerPrefs 캐시는 호출부가 책임)
    public void DisableInterstitial()
    {
        IsAdsRemoved = true;
    }

    private void ShowInterstitialAds_AfterLoading()
    {
        if (_interstitialAd.IsAdReady())
            _interstitialAd.ShowAd();
    }

    public void ShowRewardedAds(Action rewardedCallback)
    {
        _rewardedCallback = rewardedCallback;
        _rewardedAd.LoadAd();
    }

    private void ShowRewardedAds_AfterLoading()
    {
        if (_rewardedAd.IsAdReady())
            _rewardedAd.ShowAd();
    }

    private void HandleRewards_AfterRewardedAd()
    {
        _rewardedCallback?.Invoke();
        _rewardedCallback = null;
    }
    #endregion

    public void Init()
    {
        // 중복 초기화 방지 — GameManager가 여러 번 호출하더라도 SDK 이벤트가 중복 등록되지 않도록
        if (_initialized) return;
        _initialized = true;

        // PlayerPrefs 캐시 복원 — 앱 재시작 시 광고 제거 상품 보유 여부를 즉시 반영
        IsAdsRemoved = PlayerPrefs.GetInt(PlayerPrefsKey.AdsRemoved, 0) == 1;

        Debug.Log("[LevelPlaySample] LevelPlay.ValidateIntegration");
        LevelPlay.ValidateIntegration();

        Debug.Log($"[LevelPlaySample] Unity version {LevelPlay.UnityVersion}");

        Debug.Log("[LevelPlaySample] Register initialization callbacks");
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

        // SDK init
        Debug.Log("[LevelPlaySample] LevelPlay SDK initialization");
        LevelPlay.Init(DataManager.Instance.AdsConfig.GetAppKey());
    }

    void EnableAds()
    {
        // Register to ImpressionDataReadyEvent
        LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;

        // Create Rewarded Video object
        _rewardedAd = new LevelPlayRewardedAd(DataManager.Instance.AdsConfig.GetRewardedVideoAdUnitId());

        // Register to Rewarded Video events
        _rewardedAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
        _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
        _rewardedAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
        _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayedFailedEvent;
        _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
        _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
        _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
        _rewardedAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;

        // Create Interstitial object
        _interstitialAd = new LevelPlayInterstitialAd(DataManager.Instance.AdsConfig.GetInterstitialAdUnitId());

        // Register to Interstitial events
        _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        _interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Init에서 등록한 SDK 초기화 콜백 해제
        LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;

        // EnableAds에서 등록한 임프레션 콜백 해제
        LevelPlay.OnImpressionDataReady -= ImpressionDataReadyEvent;

        // 보상형 광고 이벤트 해제 — EnableAds 호출 전 Destroy 시 null 가드
        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdLoaded -= RewardedVideoOnLoadedEvent;
            _rewardedAd.OnAdLoadFailed -= RewardedVideoOnAdLoadFailedEvent;
            _rewardedAd.OnAdDisplayed -= RewardedVideoOnAdDisplayedEvent;
            _rewardedAd.OnAdDisplayFailed -= RewardedVideoOnAdDisplayedFailedEvent;
            _rewardedAd.OnAdRewarded -= RewardedVideoOnAdRewardedEvent;
            _rewardedAd.OnAdClicked -= RewardedVideoOnAdClickedEvent;
            _rewardedAd.OnAdClosed -= RewardedVideoOnAdClosedEvent;
            _rewardedAd.OnAdInfoChanged -= RewardedVideoOnAdInfoChangedEvent;
        }

        // 전면 광고 이벤트 해제 — EnableAds 호출 전 Destroy 시 null 가드
        if (_interstitialAd != null)
        {
            _interstitialAd.OnAdLoaded -= InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed -= InterstitialOnAdLoadFailedEvent;
            _interstitialAd.OnAdDisplayed -= InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdDisplayFailed -= InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClicked -= InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdClosed -= InterstitialOnAdClosedEvent;
            _interstitialAd.OnAdInfoChanged -= InterstitialOnAdInfoChangedEvent;
        }
    }

    #region 로그
    void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        // OnDestroy 이후 SDK 콜백이 도착하는 경쟁 상태 방지
        if (this == null) return;
        Debug.Log($"[LevelPlaySample] Received SdkInitializationCompletedEvent with Config: {config}");
        EnableAds();
    }

    void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.Log($"[LevelPlaySample] Received SdkInitializationFailedEvent with Error: {error}");
    }

    void RewardedVideoOnLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnLoadedEvent With AdInfo: {adInfo}");
        ShowRewardedAds_AfterLoading();
    }

    void RewardedVideoOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdLoadFailedEvent With Error: {error}");
    }

    void RewardedVideoOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdDisplayedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdDisplayedFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdDisplayedFailedEvent With AdInfo: {adInfo} and Error: {error}");
    }

    void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdRewardedEvent With AdInfo: {adInfo} and Reward: {reward}");
        HandleRewards_AfterRewardedAd();
    }

    void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdClickedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdClosedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdInfoChangedEvent With AdInfo {adInfo}");
    }

    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdLoadedEvent With AdInfo: {adInfo}");
        ShowInterstitialAds_AfterLoading();
    }

    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdLoadFailedEvent With Error: {error}");
    }

    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdDisplayedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdDisplayFailedEvent With AdInfo: {adInfo} and Error: {error}");
    }

    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdClickedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdClosedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdInfoChangedEvent With AdInfo: {adInfo}");
    }

    void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
    {
        Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent ToString(): {impressionData}");
        Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent allData: {impressionData.AllData}");
    }
    #endregion

}