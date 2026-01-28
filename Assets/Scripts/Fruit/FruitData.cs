using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "Game/FruitData")]
public class FruitData : ScriptableObject
{
    public FruitType type;
    public float radius;
    public Color color;
    public int score;
}