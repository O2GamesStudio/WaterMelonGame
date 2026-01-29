using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private FruitData[] fruitDataArray;

    private int score;
    private int currentMaxLevel = 0;
    private int consecutiveNoMerge = 0;

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

    public FruitData GetFruitData(FruitType type)
    {
        int index = (int)type;

        if (fruitDataArray == null || index < 0 || index >= fruitDataArray.Length)
        {
            Debug.LogError($"FruitData not found for type: {type}, index: {index}");
            return null;
        }

        if (fruitDataArray[index] == null)
        {
            Debug.LogError($"FruitData at index {index} is null!");
            return null;
        }

        return fruitDataArray[index];
    }

    public int GetFruitDataCount()
    {
        return fruitDataArray != null ? fruitDataArray.Length : 0;
    }

    public void AddScore(int points)
    {
        score += points;
        UIManager.Instance.UpdateScoreUI(score);
    }

    public int GetScore()
    {
        return score;
    }

    public void UpdateMaxLevel(int level)
    {
        if (level > currentMaxLevel)
        {
            currentMaxLevel = level;
        }
    }

    public int GetCurrentMaxLevel()
    {
        return currentMaxLevel;
    }

    public void IncrementNoMerge()
    {
        consecutiveNoMerge++;
    }

    public void ResetNoMerge()
    {
        consecutiveNoMerge = 0;
    }

    public int GetConsecutiveNoMerge()
    {
        return consecutiveNoMerge;
    }

    public int GetActiveFruitCount()
    {
        return FindObjectsByType<Fruit>(FindObjectsSortMode.None).Length;
    }
}