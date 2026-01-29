using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject[] fruitPrefabs;

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

    public GameObject GetFruitPrefab(FruitType type)
    {
        int index = (int)type;

        if (fruitPrefabs == null || index < 0 || index >= fruitPrefabs.Length)
        {
            Debug.LogError($"Fruit prefab not found for type: {type}");
            return null;
        }

        return fruitPrefabs[index];
    }

    public int GetFruitDataCount()
    {
        return fruitPrefabs != null ? fruitPrefabs.Length : 0;
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