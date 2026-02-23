using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System;
using System.Collections;
using TMPro;

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

    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI debugText;

    private string rewardedAdUnitId;
    private string bannerAdUnitId;

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayBannerAd bannerAd;

    private bool isInitialized = false;
    private bool isAdLoaded = false;
    private bool isLoadingAd = false;
    private bool isBannerLoaded = false;
    private bool isBannerDisplayed = false;

    private Coroutine bannerRetryCoroutine;
    private string logBuffer = "";
    private const int maxLogLines = 20;

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

            LogToUI($"UnityAdsManager 초기화 - Ad Unit: {rewardedAdUnitId}");
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
            LogToUI("ERROR: App Key 미입력");
            return;
        }

        StartCoroutine(InitializeLevelPlay());
    }

    private void LogToUI(string message)
    {
        if (debugText == null) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        logBuffer += $"[{timestamp}] {message}\n";

        string[] lines = logBuffer.Split('\n');
        if (lines.Length > maxLogLines)
        {
            logBuffer = string.Join("\n", lines, lines.Length - maxLogLines, maxLogLines);
        }

        debugText.text = logBuffer;
    }

    private IEnumerator InitializeLevelPlay()
    {
        LogToUI("LevelPlay 초기화 시작...");

        var initTask = UnityServices.InitializeAsync();

        while (!initTask.IsCompleted)
        {
            yield return null;
        }

        if (initTask.IsFaulted)
        {
            LogToUI($"ERROR: Unity Services 초기화 실패 - {initTask.Exception?.Message}");
            yield break;
        }

        LogToUI("Unity Services 초기화 완료");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        LogToUI($"SUCCESS: LevelPlay 초기화 완료");
        isInitialized = true;

        SetupRewardedAd();
        SetupBannerAd();
        LoadBannerAd();
        StartBannerRetry();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        LogToUI($"ERROR: LevelPlay 초기화 실패 - {error.ErrorMessage}");
        isInitialized = false;
    }

    private void StartBannerRetry()
    {
        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
        }
        bannerRetryCoroutine = StartCoroutine(BannerRetryRoutine());
    }

    private IEnumerator BannerRetryRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);

            LogToUI($"배너 상태 - Displayed:{isBannerDisplayed} Loaded:{isBannerLoaded} Init:{isInitialized}");

            if (!isBannerDisplayed && isInitialized)
            {
                LogToUI("배너 재시도 - 30초 경과");
                isBannerLoaded = false;
                LoadBannerAd();
            }
        }
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
            LogToUI("WARN: LevelPlay 초기화 대기 중...");
            StartCoroutine(LoadAdWithDelay(3f));
            return;
        }

        isLoadingAd = true;
        isAdLoaded = false;

        try
        {
            LogToUI("보상형 광고 로딩 중...");
            rewardedAd?.LoadAd();
        }
        catch (Exception e)
        {
            isLoadingAd = false;
            LogToUI($"ERROR: 광고 로드 예외 - {e.Message}");
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
        LogToUI($"SUCCESS: 보상형 광고 로드 완료");
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        isLoadingAd = false;
        isAdLoaded = false;
        LogToUI($"ERROR: 광고 로드 실패 - {error.ErrorCode}: {error.ErrorMessage}");
        OnAdFailedToLoad?.Invoke();
        StartCoroutine(LoadAdWithDelay(10f));
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
    {
        LogToUI("보상형 광고 표시됨");
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogToUI($"ERROR: 광고 표시 실패 - {error.ErrorMessage}");
        isAdLoaded = false;
        OnAdFailedToShow?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdClosedInternal(LevelPlayAdInfo adInfo)
    {
        LogToUI("광고 닫힘");
        OnAdClosed?.Invoke();
        StartCoroutine(LoadAdWithDelay(0.5f));
    }

    private void OnRewardedAdRewardedInternal(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        LogToUI($"SUCCESS: 광고 시청 완료 - 보상 지급");
        OnRewardEarned?.Invoke();
    }

    public void ShowRewardedAd()
    {
        if (!isInitialized)
        {
            LogToUI("WARN: LevelPlay 초기화되지 않음");
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
                LogToUI($"ERROR: 광고 표시 예외 - {e.Message}");
                isAdLoaded = false;
                OnAdFailedToShow?.Invoke();
                StartCoroutine(LoadAdWithDelay(0.5f));
            }
        }
        else
        {
            LogToUI("WARN: 광고 미로드 - 로드 재시도");
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

            LogToUI($"배너 광고 설정 완료");
        }
        catch (Exception e)
        {
            LogToUI($"ERROR: 배너 설정 실패 - {e.Message}");
        }
    }

    public void LoadBannerAd()
    {
        if (!isInitialized)
        {
            LogToUI("WARN: LevelPlay 초기화 대기 중...");
            StartCoroutine(LoadBannerAfterInit());
            return;
        }

        try
        {
            LogToUI("배너 광고 로딩 중...");
            bannerAd?.LoadAd();
        }
        catch (Exception e)
        {
            LogToUI($"ERROR: 배너 로드 예외 - {e.Message}");
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
            LogToUI("ERROR: LevelPlay 초기화 타임아웃 - 배너 로드 실패");
        }
    }

    private void OnBannerAdLoaded(LevelPlayAdInfo adInfo)
    {
        LogToUI($"SUCCESS: 배너 광고 로드 완료");
        isBannerLoaded = true;
        ShowBanner();
    }

    private void OnBannerAdLoadFailed(LevelPlayAdError error)
    {
        LogToUI($"ERROR: 배너 로드 실패 - {error.ErrorCode}: {error.ErrorMessage}");
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }

    private void OnBannerAdDisplayed(LevelPlayAdInfo adInfo)
    {
        LogToUI("SUCCESS: 배너 광고 표시됨");
        isBannerDisplayed = true;
    }

    private void OnBannerAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LogToUI($"ERROR: 배너 표시 실패 - {error.ErrorMessage}");
        isBannerDisplayed = false;
    }

    public void ShowBanner()
    {
        if (!isInitialized)
        {
            LogToUI("WARN: LevelPlay 초기화되지 않음");
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
        isBannerDisplayed = false;
    }

    public void DestroyBanner()
    {
        bannerAd?.DestroyAd();
        isBannerLoaded = false;
        isBannerDisplayed = false;
    }

    public bool IsBannerLoaded()
    {
        return isBannerLoaded;
    }

    public bool IsBannerDisplayed()
    {
        return isBannerDisplayed;
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
        if (bannerRetryCoroutine != null)
        {
            StopCoroutine(bannerRetryCoroutine);
        }

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

        LogToUI("UnityAdsManager 제거됨");
    }
}