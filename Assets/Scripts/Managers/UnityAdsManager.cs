using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class UnityAdsManager : MonoBehaviour
{
    private static UnityAdsManager instance;
    public static UnityAdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UnityAdsManager");
                instance = go.AddComponent<UnityAdsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("LevelPlay Settings")]
    [SerializeField] private string appKey = "YOUR_APP_KEY";

    [Header("Ad Unit IDs")]
    [SerializeField] private string androidRewardedAdUnitId = "3qr3vgi9qnx52n2u";
    [SerializeField] private string iOSRewardedAdUnitId = "3qr3vgi9qnx52n2u";
    [SerializeField] private string androidBannerAdUnitId = "9ulhleug5p8oljo8";
    [SerializeField] private string iOSBannerAdUnitId = "9ulhleug5p8oljo8";

    private string rewardedAdUnitId;
    private string bannerAdUnitId;

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayBannerAd bannerAd;

    private bool isInitialized = false;
    private bool isAdLoaded = false;
    private bool isLoadingAd = false;
    private bool isBannerLoaded = false;

    public event Action OnRewardEarned;
    public event Action OnAdClosed;
    public event Action OnAdFailedToLoad;
    public event Action OnAdFailedToShow;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID
            rewardedAdUnitId = androidRewardedAdUnitId;
            bannerAdUnitId = androidBannerAdUnitId;
#elif UNITY_IOS
            rewardedAdUnitId = iOSRewardedAdUnitId;
            bannerAdUnitId = iOSBannerAdUnitId;
#else
            rewardedAdUnitId = androidRewardedAdUnitId;
            bannerAdUnitId = androidBannerAdUnitId;
#endif

            Debug.Log($"[INFO] UnityAdsManager 초기화 - Ad Unit: {rewardedAdUnitId}");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(appKey) || appKey == "YOUR_APP_KEY")
        {
            Debug.LogError("=== [ERROR] App Key 미입력 ===");
            Debug.LogError("LevelPlay Dashboard에서 App Key를 확인하고 Inspector에 입력하세요!");
            Debug.LogError("App Settings → App Key");
            Debug.LogError("===========================");
            return;
        }

        StartCoroutine(InitializeLevelPlay());
    }

    private IEnumerator InitializeLevelPlay()
    {
        Debug.Log("[INFO] Unity LevelPlay 초기화 시작...");

        var initTask = UnityServices.InitializeAsync();

        while (!initTask.IsCompleted)
        {
            yield return null;
        }

        if (initTask.IsFaulted)
        {
            Debug.LogError($"[ERROR] Unity Services 초기화 실패: {initTask.Exception?.Message}");
            yield break;
        }

        Debug.Log("[INFO] Unity Services 초기화 완료");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log($"[SUCCESS] LevelPlay 초기화 완료 (App Key: {appKey})");
        isInitialized = true;

#if !UNITY_EDITOR
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log("=== 테스트 디바이스 등록 정보 ===");
        Debug.Log($"Device ID: {deviceId}");
        Debug.Log($"Device Model: {SystemInfo.deviceModel}");
        Debug.Log($"OS: {SystemInfo.operatingSystem}");
        Debug.Log("이 Device ID를 LevelPlay Dashboard에 등록하세요!");
        Debug.Log("===============================");
#endif

        SetupRewardedAd();
        SetupBannerAd();
        LoadBannerAd();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[ERROR] LevelPlay 초기화 실패: {error.ErrorMessage}");
        isInitialized = false;
    }

    #region 보상형 광고

    private void SetupRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += OnRewardedAdLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnRewardedAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
        rewardedAd.OnAdClosed += OnRewardedAdClosedInternal;
        rewardedAd.OnAdRewarded += OnRewardedAdRewardedInternal;

        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        if (isLoadingAd)
        {
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[WARN] LevelPlay 초기화 대기 중...");
            StartCoroutine(LoadAdWithDelay(3f));
            return;
        }

        isLoadingAd = true;
        isAdLoaded = false;

        try
        {
            Debug.Log("[INFO] 보상형 광고 로딩 중...");
            rewardedAd?.LoadAd();
        }
        catch (Exception e)
        {
            isLoadingAd = false;
            Debug.LogError($"[ERROR] 광고 로드 예외: {e.Message}");
            OnAdFailedToLoad?.Invoke();
            StartCoroutine(LoadAdWithDelay(10f));
        }
    }

    private IEnumerator LoadAdWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadRewardedAd();
    }

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
        isLoadingAd = false;
        isAdLoaded = true;
        Debug.Log($"[SUCCESS] 보상형 광고 로드 완료 (Ad Unit: {adInfo.AdUnitId})");
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        isLoadingAd = false;
        isAdLoaded = false;
        Debug.LogError("=== [ERROR] 광고 로드 실패 ===");
        Debug.LogError($"오류 코드: {error.ErrorCode}");
        Debug.LogError($"메시지: {error.ErrorMessage}");
        Debug.LogError($"Ad Unit ID: {rewardedAdUnitId}");
        Debug.LogError($"App Key: {appKey}");
        Debug.LogError("=============================");

        OnAdFailedToLoad?.Invoke();
        StartCoroutine(LoadAdWithDelay(10f));
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[INFO] 보상형 광고 표시됨");
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[ERROR] 광고 표시 실패: {error.ErrorMessage}");
        isAdLoaded = false;
        OnAdFailedToShow?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdClosedInternal(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[INFO] 광고 닫힘");
        OnAdClosed?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdRewardedInternal(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[SUCCESS] 광고 시청 완료 - 보상 지급 ({reward.Amount} {reward.Name})");
        OnRewardEarned?.Invoke();
    }

    public void ShowRewardedAd()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[WARN] LevelPlay 초기화되지 않음");
            OnAdFailedToShow?.Invoke();
            return;
        }

        if (rewardedAd != null && isAdLoaded && rewardedAd.IsAdReady())
        {
            try
            {
                rewardedAd.ShowAd();
                isAdLoaded = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ERROR] 광고 표시 예외: {e.Message}");
                isAdLoaded = false;
                OnAdFailedToShow?.Invoke();
                StartCoroutine(LoadAdWithDelay(0.5f));
            }
        }
        else
        {
            Debug.LogWarning("[WARN] 광고 미로드 - 로드 재시도");
            OnAdFailedToShow?.Invoke();

            if (!isLoadingAd)
            {
                LoadRewardedAd();
            }
        }
    }

    #endregion

    #region 배너 광고

    private void SetupBannerAd()
    {
        try
        {
            var configBuilder = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false);

            LevelPlayBannerAd.Config bannerConfig = configBuilder.Build();

            bannerAd = new LevelPlayBannerAd(bannerAdUnitId, bannerConfig);

            bannerAd.OnAdLoaded += OnBannerAdLoaded;
            bannerAd.OnAdLoadFailed += OnBannerAdLoadFailed;
            bannerAd.OnAdDisplayed += OnBannerAdDisplayed;
            bannerAd.OnAdDisplayFailed += OnBannerAdDisplayFailed;

            Debug.Log($"[INFO] 배너 광고 설정 완료 (Ad Unit: {bannerAdUnitId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ERROR] 배너 설정 실패: {e.Message}");
        }
    }

    public void LoadBannerAd()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[WARN] LevelPlay 초기화 대기 중...");
            StartCoroutine(LoadBannerAfterInit());
            return;
        }

        try
        {
            Debug.Log("[INFO] 배너 광고 로딩 중...");
            bannerAd?.LoadAd();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ERROR] 배너 로드 예외: {e.Message}");
        }
    }

    private IEnumerator LoadBannerAfterInit()
    {
        float waitTime = 0f;
        while (!isInitialized && waitTime < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        if (isInitialized)
        {
            LoadBannerAd();
        }
        else
        {
            Debug.LogError("[ERROR] LevelPlay 초기화 타임아웃 - 배너 로드 실패");
        }
    }

    private void OnBannerAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[SUCCESS] 배너 광고 로드 완료 (Ad Unit: {adInfo.AdUnitId})");
        isBannerLoaded = true;
        ShowBanner();
    }

    private void OnBannerAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[ERROR] 배너 로드 실패: {error.ErrorMessage}");
        isBannerLoaded = false;
    }

    private void OnBannerAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[INFO] 배너 광고 표시됨");
    }

    private void OnBannerAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[ERROR] 배너 표시 실패: {error.ErrorMessage}");
    }

    public void ShowBanner()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[WARN] LevelPlay 초기화되지 않음");
            return;
        }

        if (isBannerLoaded)
        {
            bannerAd?.ShowAd();
        }
        else
        {
            bannerAd?.LoadAd();
        }
    }

    public void HideBanner()
    {
        bannerAd?.HideAd();
    }

    public void DestroyBanner()
    {
        bannerAd?.DestroyAd();
        isBannerLoaded = false;
    }

    public bool IsBannerLoaded()
    {
        return isBannerLoaded;
    }

    #endregion

    #region GoogleAdsManager 호환 메서드

    public bool IsAdLoaded()
    {
        return isAdLoaded && rewardedAd != null && rewardedAd.IsAdReady();
    }

    public bool IsLoadingAd()
    {
        return isLoadingAd;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public string GetAdUnitId()
    {
        return rewardedAdUnitId;
    }

    public string GetAppKey()
    {
        return appKey;
    }

    #endregion

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnRewardedAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnRewardedAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnRewardedAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedAdDisplayFailed;
            rewardedAd.OnAdClosed -= OnRewardedAdClosedInternal;
            rewardedAd.OnAdRewarded -= OnRewardedAdRewardedInternal;
        }

        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnBannerAdLoaded;
            bannerAd.OnAdLoadFailed -= OnBannerAdLoadFailed;
            bannerAd.OnAdDisplayed -= OnBannerAdDisplayed;
            bannerAd.OnAdDisplayFailed -= OnBannerAdDisplayFailed;
            bannerAd.DestroyAd();
        }

        Debug.Log("[INFO] UnityAdsManager 제거됨");
    }
}