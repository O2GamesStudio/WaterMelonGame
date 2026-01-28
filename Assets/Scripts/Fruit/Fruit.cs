using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    public FruitType fruitType;
    public float explosionRadiusMultiplier = 2f;
    public float explosionForce = 5f;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer sr;
    private bool canMerge = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = CreateCircleSprite();
        }
    }

    Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color color = distance <= radius ? Color.white : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    public void Initialize(FruitType type)
    {
        fruitType = type;
        FruitData data = GameManager.Instance.GetFruitData(type);

        transform.localScale = Vector3.one * data.radius * 2f;
        col.radius = 0.5f;

        Color color = data.color;
        color.a = 1f;
        sr.color = color;

        rb.gravityScale = 1f;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void EnablePhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        canMerge = true;
    }

    public void DisablePhysics()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        canMerge = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canMerge) return;

        Fruit otherFruit = collision.gameObject.GetComponent<Fruit>();
        if (otherFruit != null && otherFruit.canMerge && otherFruit.fruitType == fruitType)
        {
            if (fruitType != FruitType.Watermelon)
            {
                Merge(otherFruit);
            }
        }
    }

    void Merge(Fruit other)
    {
        if (GetInstanceID() < other.GetInstanceID()) return;

        Vector3 mergePosition = (transform.position + other.transform.position) / 2f;
        FruitType nextType = (FruitType)((int)fruitType + 1);

        FruitData data = GameManager.Instance.GetFruitData(fruitType);
        GameManager.Instance.AddScore(data.score);

        ApplyExplosionForce(mergePosition, data.radius);

        FruitSpawner.Instance.SpawnMergedFruit(nextType, mergePosition);

        Destroy(other.gameObject);
        Destroy(gameObject);
    }

    void ApplyExplosionForce(Vector3 explosionPosition, float fruitRadius)
    {
        float explosionRadius = fruitRadius * explosionRadiusMultiplier;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius);

        foreach (Collider2D col in colliders)
        {
            Rigidbody2D targetRb = col.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb != rb)
            {
                Vector2 direction = (col.transform.position - explosionPosition).normalized;
                float distance = Vector2.Distance(col.transform.position, explosionPosition);
                float forceMagnitude = explosionForce * (1f - distance / explosionRadius);

                targetRb.AddForce(direction * forceMagnitude, ForceMode2D.Impulse);
            }
        }
    }
}