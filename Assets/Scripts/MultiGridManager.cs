using System.Collections.Generic;
using UnityEngine;

public class MultiGridManager : MonoBehaviour
{
    public static MultiGridManager Instance;

    public enum FitMode { UseRowsCols, UseCellSize }

    [Header("Grid Ayarları")]
    public Transform gridAreaTransform;

    [Tooltip("Hücre kare boyu (UseCellSize modunda aktif)")]
    public float cellSize = 1f;

    [Tooltip("Izgarayı alana tam oturtmak için mod")]
    public FitMode fitMode = FitMode.UseRowsCols;

    [Tooltip("UseRowsCols modunda hedef sütun/satır sayısı")]
    public int columns = 8, rows = 12;

    public Color gizmoColor = Color.gray;

    [Header("Konum (Inspector'dan ayarlanabilir)")]
    [Tooltip("World-space kaydırma (birim cinsinden)")]
    public Vector2 gridOffset = Vector2.zero;

    [Tooltip("Hücre cinsinden kaydırma (tam hücre kaydırır)")]
    public Vector2Int cellOffset = Vector2Int.zero;

    [Header("Uçaklar")]
    public List<AirplaneController> allAirplanes = new List<AirplaneController>();

    private Vector3 origin; // gridin sol-alt köşe dünya koordinatı (padding + offset sonrası)
    private int width;      // hücre cinsinden genişlik
    private int height;     // hücre cinsinden yükseklik

    // Engeller (hücre işaretleme)
    private bool[,] blockedCells;

    // Uçak işgali
    private bool[,] occupiedCells;
    private readonly Dictionary<AirplaneData, List<Vector2Int>> planeCells = new();

    void Awake()
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

        RecalculateGrid(bounds.Value);   // <<< ortak hesap

        blockedCells  = new bool[width, height];
        occupiedCells = new bool[width, height];

        MarkObstacles();
    }

    // --- Beyaz alana göre origin / cellSize / width / height hesabı + offsetler ---
    private void RecalculateGrid(Bounds b)
    {
        Vector3 baseOrigin;

        if (fitMode == FitMode.UseRowsCols)
        {
            // Hücre kare kalmalı → X ve Y’den çıkan iki olası boydan küçük olanı al
            float csX = b.size.x / Mathf.Max(1, columns);
            float csY = b.size.y / Mathf.Max(1, rows);
            cellSize  = Mathf.Min(csX, csY);

            width  = columns;
            height = rows;

            // Kenar boşluklarını eşitle ve grid’i içte merkezle
            float padX = (b.size.x - width * cellSize)  * 0.5f;
            float padY = (b.size.y - height * cellSize) * 0.5f;
            baseOrigin = b.min + new Vector3(padX, padY, 0f);
        }
        else // FitMode.UseCellSize
        {
            width  = Mathf.FloorToInt(b.size.x / Mathf.Max(0.0001f, cellSize));
            height = Mathf.FloorToInt(b.size.y / Mathf.Max(0.0001f, cellSize));

            // Sığmayan kısım iki tarafa eşit dağıtılır → çizgiler kenara paralel olur
            float padX = (b.size.x - width * cellSize)  * 0.5f;
            float padY = (b.size.y - height * cellSize) * 0.5f;
            baseOrigin = b.min + new Vector3(padX, padY, 0f);
        }

        // --- Inspector offsetleri ---
        // 1) world-space offset
        baseOrigin += new Vector3(gridOffset.x, gridOffset.y, 0f);
        // 2) hücre cinsinden offset
        baseOrigin += new Vector3(cellOffset.x * cellSize, cellOffset.y * cellSize, 0f);

        origin = baseOrigin;
    }

    private void MarkObstacles()
    {
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
    public bool OverlapsObstacle(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        GetFootprintBox(data.gridSize, gridPos, rotation, out Vector3 center, out Vector2 size);
        return Physics2D.OverlapBox(center, size, 0f, LayerMask.GetMask("Obstacle"));
    }

    public bool CanPlace(AirplaneData data, Vector2Int gridPos, int rotation)
    {
        foreach (var c in GetFootprintCells(data.gridSize, gridPos, rotation))
        {
            if (!IsWithinBounds(c)) return false;
            if (IsCellOccupied(c)) return false;
        }
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
        Release(data);

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

    private IEnumerable<Vector2Int> GetFootprintCells(Vector2Int size, Vector2Int originCell, int rotation)
    {
        int w = size.x, h = size.y;
        if ((rotation % 180) != 0) { int t = w; w = h; h = t; }

        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
                yield return new Vector2Int(originCell.x + dx, originCell.y + dy);
    }

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

        // Scene görünümünde de aynı hesabı kullan
        RecalculateGrid(bounds.Value);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GetWorldPosition(x, y) + Vector3.one * cellSize * 0.5f;
                Color c = gizmoColor;
                if (blockedCells != null && x < blockedCells.GetLength(0) && y < blockedCells.GetLength(1) && blockedCells[x, y]) c = Color.red;
                else if (occupiedCells != null && x < occupiedCells.GetLength(0) && y < occupiedCells.GetLength(1) && occupiedCells[x, y]) c = Color.yellow;

                Gizmos.color = c;
                Gizmos.DrawWireCube(center, Vector3.one * cellSize);
            }
        }
    }
}
