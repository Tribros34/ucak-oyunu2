using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AirplaneData))]
[RequireComponent(typeof(SpriteRenderer))]
public class AirplaneController : MonoBehaviour
{
    // Sprite’ın “burnu” defaultta nereye bakıyor?
    public enum ForwardAxis { Right, Up }
    [Header("Heading")]
    [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float movedDistance = 0f;
    private bool isMoving = false;

    private void Start()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // Sahne başında da yönü mevcut dönüşten türet
        SyncMoveDirectionFromRotation();
    }

    public void StartRolling()
    {
        Debug.Log($"{gameObject.name} StartRolling çağrıldı");

        if (!isMoving)
        {
            isMoving = true;
            movedDistance = 0f;

            // Kalkıştan hemen önce de güncelle (az önce döndürmüş olabilirsin)
            SyncMoveDirectionFromRotation();

            StartCoroutine(RollAndTakeOff());
        }
    }

    private IEnumerator RollAndTakeOff()
    {
        Debug.Log($"{gameObject.name} hareket etmeye başladı");

        // 🔥 Yönü BURADAN al: burnun nereye bakıyorsa oraya.
        Vector3 dirWorld = (forwardAxis == ForwardAxis.Right ? transform.right : transform.up).normalized;

        float z = transform.position.z;
        while (movedDistance < data.requiredDistance)
        {
            float moveStep = data.moveSpeed * Time.deltaTime;
            transform.position += dirWorld * moveStep;
            transform.position = new Vector3(transform.position.x, transform.position.y, z);
            movedDistance += moveStep;
            yield return null;
        }

        yield return StartCoroutine(TakeOffAnimation());
        Destroy(gameObject);
    }

    private IEnumerator TakeOffAnimation()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        float rotationAmount = 10f; // sağa sola eğilme açısı (derece)

        // 🔥 Dalgalanmayı mevcut açı etrafında yap (burnu kaydırmaz)
        float baseAngle = transform.eulerAngles.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float angle = Mathf.Sin(t * Mathf.PI * 2f) * rotationAmount;
            transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + angle);

            // Saydamlaşarak yok olma
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - t);
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Airplane") && other.gameObject != this.gameObject)
        {
            Debug.Log($"{gameObject.name} çarpıştı → {other.gameObject.name}");
            Destroy(other.gameObject);
            Destroy(this.gameObject);
        }
    }

    // === YÖN SENKRONU ===
    public void SyncMoveDirectionFromRotation()
    {
        // Burnu sağa çizildiyse transform.right, yukarıysa transform.up
        Vector3 fwd = (forwardAxis == ForwardAxis.Right ? transform.right : transform.up);

        // (İsteğe bağlı) 4 ana yöne snap — grid oyunlarında daha güvenli
        Vector2 snapped = SnapToCardinal(fwd);

        data.direction = new Vector2Int(Mathf.RoundToInt(snapped.x), Mathf.RoundToInt(snapped.y));
        data.moveDirection = new Vector3(snapped.x, snapped.y, 0f);
    }

    private static Vector2 SnapToCardinal(Vector3 v)
    {
        // X veya Y bileşeni hangisi büyükse onu 1/-1 yap, diğeri 0
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }
}
