using UnityEngine;
using UnityEngine.InputSystem;

public class FruitSpawner : MonoBehaviour
{
    public static FruitSpawner Instance { get; private set; }

    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float containerHalfWidth = 2f;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private TextAsset probabilityConfigJson;
    [SerializeField] private float lineExtensionMultiplier = 3f;
    [SerializeField] private float spawnDelay = 1f;

    private Fruit currentFruit;
    private FruitType nextFruitType;
    private Camera mainCamera;
    private bool isDragging = false;
    private float currentFruitRadius = 0f;
    private SpawnProbabilityConfig probabilityConfig;
    private int containerFruitLayerMask;
    private RaycastHit2D[] raycastHitBuffer = new RaycastHit2D[10];
    private const float maxRayDistance = 20f;

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

        mainCamera = Camera.main;
        containerFruitLayerMask = LayerMask.GetMask("Container", "Fruit");

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = 2;
        }

        LoadProbabilityConfig();
    }

    void Start()
    {
        nextFruitType = GetRandomFruitType();
        SpawnNextFruit();
    }

    void Update()
    {
        if (currentFruit == null) return;

        Vector2 touchPosition = GetInputPosition();
        if (touchPosition != Vector2.zero)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10f));
            float clampedX = Mathf.Clamp(worldPos.x, -containerHalfWidth + currentFruitRadius, containerHalfWidth - currentFruitRadius);
            currentFruit.transform.position = new Vector3(clampedX, transform.position.y, 0f);

            if (!isDragging)
            {
                isDragging = true;
                if (trajectoryLine != null)
                {
                    trajectoryLine.enabled = true;
                }
            }

            UpdateTrajectory(currentFruit.transform.position);
        }

        if (isDragging && !IsInputActive())
        {
            isDragging = false;
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
            DropFruit();
        }
    }

    void LoadProbabilityConfig()
    {
        if (probabilityConfigJson != null)
        {
            probabilityConfig = JsonUtility.FromJson<SpawnProbabilityConfig>(probabilityConfigJson.text);
        }
        else
        {
            Debug.LogError("Probability config JSON not assigned!");
        }
    }

    void UpdateTrajectory(Vector3 startPosition)
    {
        if (trajectoryLine == null) return;

        int hitCount = Physics2D.RaycastNonAlloc(startPosition, Vector2.down, raycastHitBuffer, maxRayDistance, containerFruitLayerMask);

        float closestDistance = maxRayDistance;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (raycastHitBuffer[i].collider.gameObject == currentFruit.gameObject)
            {
                continue;
            }

            if (raycastHitBuffer[i].distance < closestDistance)
            {
                closestDistance = raycastHitBuffer[i].distance;
                foundHit = true;
            }
        }

        float totalDistance = foundHit ? closestDistance * lineExtensionMultiplier : maxRayDistance;
        float segmentLength = 0.15f;
        int segments = Mathf.FloorToInt(totalDistance / segmentLength);

        trajectoryLine.positionCount = segments * 2;

        int posIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            if (i % 2 == 0)
            {
                float startDist = i * segmentLength;
                float endDist = (i + 0.5f) * segmentLength;

                trajectoryLine.SetPosition(posIndex++, startPosition + Vector3.down * startDist);
                trajectoryLine.SetPosition(posIndex++, startPosition + Vector3.down * endDist);
            }
        }

        trajectoryLine.positionCount = posIndex;
    }

    Vector2 GetInputPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }

    bool IsInputActive()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return false;
    }

    void SpawnNextFruit()
    {
        GameObject prefab = GameManager.Instance.GetFruitPrefab(nextFruitType);
        if (prefab == null) return;

        GameObject fruitObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        currentFruit = fruitObj.GetComponent<Fruit>();
        currentFruit.DisablePhysics();

        currentFruitRadius = currentFruit.GetRadius();

        nextFruitType = GetRandomFruitType();
        UIManager.Instance.UpdateNextFruitUI(nextFruitType);
    }

    void DropFruit()
    {
        currentFruit.EnablePhysics();
        currentFruit = null;
        currentFruitRadius = 0f;
        GameManager.Instance.IncrementNoMerge();
        Invoke(nameof(SpawnNextFruit), spawnDelay);
    }

    public void SpawnMergedFruit(FruitType type, Vector3 position)
    {
        GameObject prefab = GameManager.Instance.GetFruitPrefab(type);
        if (prefab == null) return;

        GameObject fruitObj = Instantiate(prefab, position, Quaternion.identity);
        Fruit fruit = fruitObj.GetComponent<Fruit>();

        float newRadius = fruit.GetRadius();
        Vector3 clampedPosition = ClampMergePosition(position, newRadius);
        fruitObj.transform.position = clampedPosition;

        fruit.EnablePhysics();
    }
    Vector3 ClampMergePosition(Vector3 position, float newRadius)
    {
        float minX = -containerHalfWidth + newRadius;
        float maxX = containerHalfWidth - newRadius;

        position.x = Mathf.Clamp(position.x, minX, maxX);

        return position;
    }

    public float GetContainerHalfWidth()
    {
        return containerHalfWidth;
    }

    FruitType GetRandomFruitType()
    {
        int maxLevel = GameManager.Instance.GetCurrentMaxLevel();
        int activeFruits = GameManager.Instance.GetActiveFruitCount();
        int noMergeCount = GameManager.Instance.GetConsecutiveNoMerge();

        float[] probabilities = GetProbabilities(maxLevel, activeFruits, noMergeCount);

        float randomValue = Random.Range(0f, 1f);
        float cumulative = 0f;

        for (int i = 0; i < 5; i++)
        {
            cumulative += probabilities[i];
            if (randomValue <= cumulative)
            {
                return (FruitType)i;
            }
        }

        return FruitType.Cherry;
    }

    float[] GetProbabilities(int maxLevel, int activeFruits, int noMergeCount)
    {
        if (probabilityConfig == null) return new float[] { 1f, 0f, 0f, 0f, 0f };

        float[] probs = new float[5];

        foreach (var set in probabilityConfig.probabilitySets)
        {
            if (maxLevel <= set.maxLevelThreshold)
            {
                int copyLength = Mathf.Min(probs.Length, set.probabilities.Length);
                System.Array.Copy(set.probabilities, probs, copyLength);
                break;
            }
        }

        if (activeFruits >= probabilityConfig.emergencyFruitCount)
        {
            float boost = probabilityConfig.emergencyBoost;
            probs[0] += boost * 0.7f;
            probs[1] += boost * 0.3f;
            probs[2] *= 0.85f;
            probs[3] *= 0.85f;
            probs[4] *= 0.85f;
        }

        if (noMergeCount >= probabilityConfig.noMergeThreshold)
        {
            probs[0] += probabilityConfig.noMergeBoost;
            probs[1] *= 0.8f;
            probs[2] *= 0.8f;
            probs[3] *= 0.8f;
            probs[4] *= 0.8f;
        }

        float sum = probs[0] + probs[1] + probs[2] + probs[3] + probs[4];
        float invSum = 1f / sum;
        probs[0] *= invSum;
        probs[1] *= invSum;
        probs[2] *= invSum;
        probs[3] *= invSum;
        probs[4] *= invSum;

        return probs;
    }
}