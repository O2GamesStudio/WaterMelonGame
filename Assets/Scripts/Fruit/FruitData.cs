using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "Game/FruitData")]
public class FruitData : ScriptableObject
{
    public FruitType type;
    public float radius;
    public Color color;
    public int score;
    public float mass = 1f;
    public float pushStrength = 3f;
}