using UnityEngine;

public class AirplaneData : MonoBehaviour
{
    // Projende zaten var: tek boyut/metrik vs.
    public int size = 3;

    public Vector2Int direction = Vector2Int.right;

    [Tooltip("Kalkış mesafesi (birim olarak)")]
    public float requiredDistance = 3f;

    [Tooltip("Uçuş yönü (örnek: sağa doğru)")]
    public Vector3 moveDirection = Vector3.right;

    [Tooltip("Hareket hızı (birim/saniye)")]
    public float moveSpeed = 2f;

    // GRID FOOTPRINT (GxY, hücre)
    public Vector2Int gridSize = new Vector2Int(2, 1);

    // 0 / 90 / 180 / 270
    [Range(0, 359)]
    public int rotation = 0;
}
