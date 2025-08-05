using UnityEngine;

public class AirplaneData : MonoBehaviour
{
    public int size = 3;
    public Vector2Int direction = Vector2Int.right;

    [Tooltip("Kalkış mesafesi (birim olarak)")]
    public float requiredDistance = 3f;

    [Tooltip("Uçuş yönü (örnek: sağa doğru)")]
    public Vector3 moveDirection = Vector3.right;

    [Tooltip("Hareket hızı (birim/saniye)")]
    public float moveSpeed = 2f;
}
