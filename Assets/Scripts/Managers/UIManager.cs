using TMPro;
using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float countUpSpeed = 100f;

    private int currentDisplayScore = 0;
    private int targetScore = 0;
    private Coroutine countUpCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreText(0);
    }

    public void UpdateScoreUI(int newScore)
    {
        targetScore = newScore;

        if (countUpCoroutine == null)
        {
            countUpCoroutine = StartCoroutine(CountUpScore());
        }
    }

    IEnumerator CountUpScore()
    {
        while (currentDisplayScore < targetScore)
        {
            int difference = targetScore - currentDisplayScore;
            int increment = Mathf.Max(1, Mathf.CeilToInt(difference * Time.deltaTime * countUpSpeed / 10f));

            currentDisplayScore = Mathf.Min(currentDisplayScore + increment, targetScore);
            UpdateScoreText(currentDisplayScore);

            yield return null;
        }

        countUpCoroutine = null;
    }

    void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}