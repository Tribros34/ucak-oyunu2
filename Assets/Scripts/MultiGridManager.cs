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

    // Engeller (hücre işaretleme)
    private bool[,] blockedCells;

    // Uçak işgali
    private bool[,] occupiedCells;
    private readonly Dictionary<AirplaneData, List<Vector2Int>> planeCells = new();

    private void Awake()
    {
        Instance = this;

        if (!gridAreaTransform)
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
        occupiedCells = new bool[width, height];

        MarkObstacles();
    }

    private void MarkObstacles()
    {
        // Basit: obstacle pivotunun geldiği hücreyi işaretler (geniş alanlar için
        // asıl kontrolü OverlapBox ile yapıyoruz)
        Collider2D[] obstacles = Physics2D.OverlapBoxAll(
            origin + new Vector3(width * cellSize / 2f, height * cellSize / 2f),
            new Vector2(width * cellSize, height * cellSize),
            0f,
            LayerMask.GetMask("Obstacle")
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
        foreach (var plane in allAirplanes)
            if (plane != null)
                plane.StartRolling();
    }

    // ----- Grid yardımcıları -----
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

    public bool IsCellOccupied(Vector2Int cell)
    {
        if (!IsWithinBounds(cell)) return true;
        return occupiedCells[cell.x, cell.y];
    }

    // ----- Yerleştirme / Döndürme API'ları -----

    // Fiziksel engel kesişimi (Obstacle layer) — footprint kutusuyla
    public bool OverlapsObstacle(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        GetFootprintBox(data.gridSize, gridPos, rotation, out Vector3 center, out Vector2 size);
        return Physics2D.OverlapBox(center, size, 0f, LayerMask.GetMask("Obstacle"));
    }

    // Konulabilir mi? (sınır + işgal + fiziksel engel)
    public bool CanPlace(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        foreach (var c in GetFootprintCells(data.gridSize, gridPos, rotation))
        {
            if (!IsWithinBounds(c)) return false;
            if (IsCellOccupied(c)) return false;   // diğer uçaklarla çakışma
        }
        // Engelleri fizik üzerinden geniş alan olarak test et
        if (OverlapsObstacle(data, gridPos, rotation)) return false;

        return true;
    }

    public bool OccupyIfAvailable(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        if (!CanPlace(data, gridPos, rotation)) return false;
        Occupy(data, gridPos, rotation);
        return true;
    }

    public void Occupy(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        Release(data); // eski işgali temizle

        var list = new List<Vector2Int>();
        foreach (var c in GetFootprintCells(data.gridSize, gridPos, rotation))
        {
            occupiedCells[c.x, c.y] = true;
            list.Add(c);
        }
        planeCells[data] = list;
    }

    public void Release(AirplaneData data)
    {
        if (!planeCells.TryGetValue(data, out var list)) return;
        foreach (var c in list)
            if (IsWithinBounds(c)) occupiedCells[c.x, c.y] = false;
        planeCells.Remove(data);
    }

    // ----- Footprint yardımcıları -----
    private IEnumerable<Vector2Int> GetFootprintCells(Vector2Int size, Vector2Int originCell, int rotation)
    {
        int w = size.x, h = size.y;
        if ((rotation % 180) != 0) { int t = w; w = h; h = t; }

        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
                yield return new Vector2Int(originCell.x + dx, originCell.y + dy);
    }

    // OverlapBox için merkez ve boyutu hesapla
    public void GetFootprintBox(Vector2Int size, Vector2Int originCell, int rotation, out Vector3 center, out Vector2 worldSize)
    {
        int w = size.x, h = size.y;
        if ((rotation % 180) != 0) { int t = w; w = h; h = t; }

        Vector3 worldOrigin = GetWorldPosition(originCell.x, originCell.y);
        worldSize = new Vector2(w * cellSize, h * cellSize);
        center = worldOrigin + new Vector3(worldSize.x * 0.5f, worldSize.y * 0.5f, 0f);
    }

    private void OnDrawGizmos()
    {
        if (!gridAreaTransform) return;
        var bounds = gridAreaTransform.GetComponent<Renderer>()?.bounds;
        if (bounds == null) return;

        origin = bounds.Value.min;
        width = Mathf.FloorToInt(bounds.Value.size.x / cellSize);
        height = Mathf.FloorToInt(bounds.Value.size.y / cellSize);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GetWorldPosition(x, y) + Vector3.one * cellSize * 0.5f;
                Color c = gizmoColor;
                if (blockedCells != null && blockedCells[x, y]) c = Color.red;
                else if (occupiedCells != null && occupiedCells[x, y]) c = Color.yellow;

                Gizmos.color = c;
                Gizmos.DrawWireCube(center, Vector3.one * cellSize);
            }
        }
    }
}
