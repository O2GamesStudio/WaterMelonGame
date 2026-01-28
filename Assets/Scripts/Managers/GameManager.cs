using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public FruitData[] fruitDataArray;
    public int score;

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
        return fruitDataArray[(int)type];
    }

    public void AddScore(int points)
    {
        score += points;
    }
}