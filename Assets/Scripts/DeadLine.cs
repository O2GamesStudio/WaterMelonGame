using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DeadLine : MonoBehaviour
{
    public float checkDelay = 3f;
    public Vector2 colliderSize = new Vector2(4f, 0.1f);

    private float timer = 0f;
    private bool isGameOver = false;
    private BoxCollider2D col;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = colliderSize;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (isGameOver) return;

        Fruit fruit = collision.GetComponent<Fruit>();
        if (fruit != null)
        {
            timer += Time.deltaTime;

            if (timer >= checkDelay)
            {
                GameOver();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        Fruit fruit = collision.GetComponent<Fruit>();
        if (fruit != null)
        {
            timer = 0f;
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over! Final Score: " + GameManager.Instance.score);
        Time.timeScale = 0f;
    }
}