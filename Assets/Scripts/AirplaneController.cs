using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AirplaneData))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AirplaneController : MonoBehaviour
{
    // Sprite’ın “burnu” defaultta nereye bakıyor?
    public enum ForwardAxis { Right, Up }
    [Header("Heading")]
    [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

    [Header("Crash Spin + Fade")]
    [Tooltip("Çarpışma animasyon süresi (sn)")]
    public float crashDuration = 0.9f;
    [Tooltip("Kaç tur dönsün (2 = 720°)")]
    public float crashSpins = 2f;
    [Tooltip("Çarpışınca hafif geri tepme (world units/sn)")]
    public float knockbackSpeed = 2f;
    [Tooltip("Çarpışma sırasında ne kadar küçülsün (0.4 = %40)")]
    public float shrinkAmount = 0.4f;

    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Rigidbody2D rb;
    private Collider2D col;

    private float movedDistance = 0f;
    private bool isMoving = false;
    private bool isCrashing = false;
    private Coroutine moveCo;

    private void Start()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // Fizik ayarları (trigger çarpışma için gerekli)
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Sahne başında da yönü mevcut dönüşten türet
        SyncMoveDirectionFromRotation();
    }

    public void StartRolling()
    {
        Debug.Log($"{gameObject.name} StartRolling çağrıldı");

        if (!isMoving && !isCrashing)
        {
            isMoving = true;
            movedDistance = 0f;

            // Kalkıştan hemen önce de güncelle (az önce döndürmüş olabilirsin)
            SyncMoveDirectionFromRotation();

            moveCo = StartCoroutine(RollAndTakeOff());
        }
    }

    private IEnumerator RollAndTakeOff()
    {
        Debug.Log($"{gameObject.name} hareket etmeye başladı");

        // 🔥 Yönü BURADAN al: burnun nereye bakıyorsa oraya.
        Vector3 dirWorld = (forwardAxis == ForwardAxis.Right ? transform.right : transform.up).normalized;

        float z = transform.position.z;
        while (!isCrashing && movedDistance < data.requiredDistance)
        {
            float moveStep = data.moveSpeed * Time.deltaTime;
            transform.position += dirWorld * moveStep;
            transform.position = new Vector3(transform.position.x, transform.position.y, z);
            movedDistance += moveStep;
            yield return null;
        }

        if (!isCrashing)
        {
            yield return StartCoroutine(TakeOffAnimation());
            Destroy(gameObject);
        }
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

    // === ÇARPIŞMA: spin + fade ===
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCrashing) return;

        // Tag yerine direkt component kontrolü daha güvenli
        var otherPlane = other.GetComponent<AirplaneController>();
        if (otherPlane && otherPlane != this)
        {
            // Karşılıklı olarak iki uçağa da çarpışma animasyonu uygula
            Vector2 awayFromMe = (transform.position - otherPlane.transform.position).normalized;
            Vector2 awayFromOther = -awayFromMe;

            otherPlane.TriggerCrash(awayFromOther);
            TriggerCrash(awayFromMe);
        }
    }

    public void TriggerCrash(Vector2 awayDir)
    {
        if (isCrashing) return;
        StartCoroutine(CrashSpinFade(awayDir));
    }

    private IEnumerator CrashSpinFade(Vector2 awayDir)
    {
        isCrashing = true;
        isMoving = false;

        if (moveCo != null) StopCoroutine(moveCo);
        if (col) col.enabled = false;

        float z = transform.position.z;
        float startAngle = transform.eulerAngles.z;
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;

        float t = 0f;
        while (t < crashDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / crashDuration);
            // EaseOutQuad
            float e = 1f - (1f - u) * (1f - u);

            // Spin
            float angle = startAngle + e * (360f * crashSpins);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Hafif geri tepme (zamana göre azalan)
            float kb = knockbackSpeed * (1f - u);
            transform.position += (Vector3)(awayDir * kb * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, transform.position.y, z);

            // Fade + shrink
            float a = 1f - u;
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, a);
            transform.localScale = Vector3.Lerp(startScale, startScale * (1f - shrinkAmount), e);

            yield return null;
        }

        Destroy(gameObject);
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
