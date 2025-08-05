using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Ayarları")]
    public int width = 10;
    public int height = 6;
    public float cellSize = 1f;
    public Vector3 origin = new Vector3(-4, -2, 0);

    private GameObject[,] grid;

    [Header("Uçak Listesi")]
    public List<AirplaneController> allAirplanes;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        grid = new GameObject[width, height];
    }

    /// Dünya pozisyonundan grid hücresi elde et
    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos - origin).x / cellSize);
        int y = Mathf.FloorToInt((worldPos - origin).y / cellSize);
        return new Vector2Int(x, y);
    }

    /// Grid hücresinden dünya pozisyonu elde et
    public Vector3 GetWorldPosition(int x, int y)
    {
        return origin + new Vector3(x * cellSize, y * cellSize, 0);
    }

    /// Belirli boy ve yönle başlayan tüm hücreler boş mu?
    public bool AreCellsEmpty(Vector2Int start, int size, Vector2Int direction)
    {
        for (int i = 0; i < size; i++)
        {
            int x = start.x + i * direction.x;
            int y = start.y + i * direction.y;

            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            if (grid[x, y] != null)
                return false;
        }

        return true;
    }

    /// Uçağı grid’e yerleştir
    public void PlaceAirplane(Vector2Int start, GameObject airplane, int size, Vector2Int direction)
    {
        for (int i = 0; i < size; i++)
        {
            int x = start.x + i * direction.x;
            int y = start.y + i * direction.y;

            grid[x, y] = airplane;
        }
    }

    /// Uçağı grid’den kaldır
    public void ClearAirplane(GameObject airplane)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == airplane)
                {
                    grid[x, y] = null;
                }
            }
        }
    }

    /// Uçakları harekete geçir
    public void StartAllAirplanes()
    {
            Debug.Log("StartAllAirplanes çağrıldı");

        foreach (var plane in allAirplanes)
        {
            if (plane != null)
                plane.StartRolling();
        }
    }

    /// Editörde grid çizimi
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GetWorldPosition(x, y) + Vector3.one * cellSize * 0.5f;
                Gizmos.DrawWireCube(center, Vector3.one * cellSize);
            }
        }
    }
}
