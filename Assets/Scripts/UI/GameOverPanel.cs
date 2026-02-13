using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    private const string HIGH_SCORE_KEY = "HighScore";

    [SerializeField] private Button retryBtn;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private void Awake()
    {
        retryBtn.onClick.AddListener(OnRetryClicked);
        SetupAds();
    }

    private void SetupAds()
    {
        if (UnityAdsManager.Instance != null)
        {
            UnityAdsManager.Instance.OnRewardEarned += OnAdRewardEarned;
            UnityAdsManager.Instance.OnAdClosed += OnAdClosed;
            UnityAdsManager.Instance.OnAdFailedToShow += OnAdFailedToShow;
        }
    }

    public void Show(int currentScore)
    {
        gameObject.SetActive(true);

        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        scoreText.text = currentScore.ToString();
        highScoreText.text = highScore.ToString();
    }

    private void OnRetryClicked()
    {
        if (UnityAdsManager.Instance != null && UnityAdsManager.Instance.IsAdLoaded())
        {
            UnityAdsManager.Instance.ShowRewardedAd();
        }
        else
        {
            RestartGame();
        }
    }

    private void OnAdRewardEarned()
    {
        RestartGame();
    }

    private void OnAdClosed()
    {
        RestartGame();
    }

    private void OnAdFailedToShow()
    {
        RestartGame();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        retryBtn.onClick.RemoveListener(OnRetryClicked);

        if (UnityAdsManager.Instance != null)
        {
            UnityAdsManager.Instance.OnRewardEarned -= OnAdRewardEarned;
            UnityAdsManager.Instance.OnAdClosed -= OnAdClosed;
            UnityAdsManager.Instance.OnAdFailedToShow -= OnAdFailedToShow;
        }
    }
}