using System.Collections.Generic;
using UnityEngine;

public class MultiGridManager : MonoBehaviour
{
    public static MultiGridManager Instance;

    [Header("Grid Ayarları")]
    public Transform gridAreaTransform;
    public float cellSize = 1f;
    public Color gizmoColor = Color.gray;

    [Header("Uçaklar")]
    public List<AirplaneController> allAirplanes = new List<AirplaneController>();

    private Vector3 origin;
    private int width;
    private int height;

    // Hangi hücre dolu veya engelli tutulur
    private bool[,] blockedCells;

    private void Awake()
    {
        Instance = this;

        if (gridAreaTransform == null)
        {
            Debug.LogError("GridAreaTransform atanmamış!");
            return;
        }

        var bounds = gridAreaTransform.GetComponent<Renderer>()?.bounds;

        if (bounds == null)
        {
            Debug.LogError("GridAreaTransform'da Renderer yok!");
            return;
        }

        origin = bounds.Value.min;
        width = Mathf.FloorToInt(bounds.Value.size.x / cellSize);
        height = Mathf.FloorToInt(bounds.Value.size.y / cellSize);

        blockedCells = new bool[width, height];

        MarkObstacles();
    }

    /// <summary>
    /// Obstacle tagli objeleri grid'e bloklar
    /// </summary>
    /// 
    ///   
public bool IsValidPlacement(Vector3 position, float width)
{
    // Uçağın kapladığı alanın uygunluğunu kontrol et
    bool isBlocked = Physics2D.OverlapBox(
        position + new Vector3(width * 0.5f, 0, 0),
        new Vector2(width, 1f),
        0f,
        LayerMask.GetMask("Obstacle")
    );

    return !isBlocked;
}





    private void MarkObstacles()
    {
        Collider2D[] obstacles = Physics2D.OverlapBoxAll(
            origin + new Vector3(width * cellSize / 2f, height * cellSize / 2f),
            new Vector2(width * cellSize, height * cellSize),
            0f,
            LayerMask.GetMask("Obstacle")  // Layer'ı kontrol et!
        );

        foreach (var col in obstacles)
        {
            Vector2Int cell = GetGridPosition(col.transform.position);

            if (IsWithinBounds(cell))
                blockedCells[cell.x, cell.y] = true;
        }
    }

    public void StartAllAirplanes()

    {
    Debug.Log("✅ StartAllAirplanes() ÇAĞRILDI!");

        foreach (var plane in allAirplanes)
        {
            if (plane != null)
                plane.StartRolling();
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos - origin).x / cellSize);
        int y = Mathf.FloorToInt((worldPos - origin).y / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return origin + new Vector3(x * cellSize, y * cellSize, 0);
    }

    public bool IsWithinBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public bool IsCellBlocked(Vector2Int cell)
    {
        if (!IsWithinBounds(cell)) return true;
        return blockedCells[cell.x, cell.y];
    }

    private void OnDrawGizmos()
    {
        if (gridAreaTransform == null) return;

        var bounds = gridAreaTransform.GetComponent<Renderer>()?.bounds;
        if (bounds == null) return;

        origin = bounds.Value.min;
        width = Mathf.FloorToInt(bounds.Value.size.x / cellSize);
        height = Mathf.FloorToInt(bounds.Value.size.y / cellSize);

        Gizmos.color = gizmoColor;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GetWorldPosition(x, y) + Vector3.one * cellSize * 0.5f;

                Gizmos.color = blockedCells != null && blockedCells[x, y] ? Color.red : gizmoColor;
                Gizmos.DrawWireCube(center, Vector3.one * cellSize);
            }
        }
    }
}
