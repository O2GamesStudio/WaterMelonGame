using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    private FruitType fruitType;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer sr;
    private bool canMerge = false;
    private bool canCheckGameOver = false;

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

        rb.mass = data.mass;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void EnablePhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        canMerge = true;
        Invoke(nameof(EnableGameOverCheck), 2f);
    }

    void EnableGameOverCheck()
    {
        canCheckGameOver = true;
    }

    public void DisablePhysics()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        canMerge = false;
        canCheckGameOver = false;
    }

    public bool CanCheckGameOver()
    {
        return canCheckGameOver;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canMerge) return;

        Fruit otherFruit = collision.gameObject.GetComponent<Fruit>();
        if (otherFruit != null && otherFruit.canMerge && otherFruit.fruitType == fruitType)
        {
            int nextIndex = (int)fruitType + 1;
            if (nextIndex < GameManager.Instance.GetFruitDataCount())
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
        GameManager.Instance.UpdateMaxLevel((int)nextType);
        GameManager.Instance.ResetNoMerge();

        ApplyPushForce(mergePosition);

        FruitSpawner.Instance.SpawnMergedFruit(nextType, mergePosition);

        Destroy(other.gameObject);
        Destroy(gameObject);
    }

    void ApplyPushForce(Vector3 explosionPosition)
    {
        int nextIndex = (int)fruitType + 1;
        FruitData nextData = GameManager.Instance.GetFruitData((FruitType)nextIndex);

        float newRadius = nextData.radius;
        float pushRadius = newRadius * 2f;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(explosionPosition, pushRadius);

        foreach (Collider2D col in colliders)
        {
            Rigidbody2D targetRb = col.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb != rb)
            {
                Vector2 pushDirection = ((Vector2)col.transform.position - (Vector2)explosionPosition).normalized;
                float distance = Vector2.Distance(col.transform.position, explosionPosition);

                float requiredSpace = newRadius + col.bounds.extents.x;

                if (distance < requiredSpace)
                {
                    float overlapRatio = 1f - (distance / requiredSpace);
                    float pushForce = nextData.pushStrength * overlapRatio;

                    targetRb.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}