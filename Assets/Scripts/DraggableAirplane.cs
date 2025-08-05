using UnityEngine;

[RequireComponent(typeof(AirplaneData))]
public class DraggableAirplane : MonoBehaviour
{
    private Vector3 offset;
    private Vector3 originalPosition;
    private bool isDragging = false;

    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Start()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnMouseDown()
    {
        originalPosition = transform.position;
        offset = transform.position - GetMouseWorldPosition();
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPosition() + offset;
            mousePos.z = transform.position.z;
            transform.position = mousePos;

            // Geçici hizalama pozisyonu
            Vector2Int gridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
            Vector3 snappedPos = MultiGridManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

            // Engel kontrolü
            bool isValid = !Physics2D.OverlapBox(
                snappedPos + new Vector3(data.size * 0.5f, 0, 0),
                new Vector2(data.size, 1f),
                0f,
                LayerMask.GetMask("Obstacle")
            );

            spriteRenderer.color = isValid ? Color.green : Color.red;
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;

        Vector2Int gridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
        Vector3 snappedPos = MultiGridManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

        bool isBlocked = Physics2D.OverlapBox(
            snappedPos + new Vector3(data.size * 0.5f, 0, 0),
            new Vector2(data.size, 1f),
            0f,
            LayerMask.GetMask("Obstacle")
        );

        if (isBlocked)
        {
            Debug.Log("Engellenmiş alana uçak konamaz!");
            transform.position = originalPosition;
        }
        else
        {
            transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        }

        spriteRenderer.color = originalColor;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(mouse);
    }
}
