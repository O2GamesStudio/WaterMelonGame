using UnityEngine;
using UnityEngine.InputSystem;

public class FruitSpawner : MonoBehaviour
{
    public static FruitSpawner Instance { get; private set; }

    public GameObject fruitPrefab;
    public Transform spawnPoint;
    public float spawnHeight = 4f;
    public float moveRangeX = 2f;

    private Fruit currentFruit;
    private FruitType nextFruitType;
    private Camera mainCamera;
    private bool isDragging = false;

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
            float clampedX = Mathf.Clamp(worldPos.x, -moveRangeX, moveRangeX);
            currentFruit.transform.position = new Vector3(clampedX, spawnHeight, 0f);

            if (!isDragging)
            {
                isDragging = true;
            }
        }

        if (isDragging && !IsInputActive())
        {
            isDragging = false;
            DropFruit();
        }
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
        GameObject fruitObj = Instantiate(fruitPrefab, spawnPoint.position, Quaternion.identity);
        currentFruit = fruitObj.GetComponent<Fruit>();
        currentFruit.Initialize(nextFruitType);
        currentFruit.DisablePhysics();

        nextFruitType = GetRandomFruitType();
    }

    void DropFruit()
    {
        currentFruit.EnablePhysics();
        currentFruit = null;
        Invoke(nameof(SpawnNextFruit), 1f);
    }

    public void SpawnMergedFruit(FruitType type, Vector3 position)
    {
        GameObject fruitObj = Instantiate(fruitPrefab, position, Quaternion.identity);
        Fruit fruit = fruitObj.GetComponent<Fruit>();
        fruit.Initialize(type);
        fruit.EnablePhysics();
    }

    FruitType GetRandomFruitType()
    {
        return (FruitType)Random.Range(0, 5);
    }
}