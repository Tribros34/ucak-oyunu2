using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // restart için

[RequireComponent(typeof(AirplaneData))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class AirplaneController : MonoBehaviour
{
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

    [Header("Restart")]
    [Tooltip("Çarpışma animasyonundan sonra sahneyi yeniden yükleme gecikmesi")]
    public float restartDelay = 0.1f;

    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Collider2D col;

    // === EVENTLER ===
    public static System.Action OnPlaneTookOff;
    public static System.Action OnPlaneCrashed;

    private float movedDistance = 0f;
    private bool isMoving = false;
    private bool isCrashing = false;
    private Coroutine moveCo;
    private Vector3 defaultScale;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; // tetikleyici çarpışma
    }

    private void Start()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        defaultScale = transform.localScale;

        SyncMoveDirectionFromRotation();
    }

    public void StartRolling()
    {
        if (isMoving || isCrashing) return;

        isMoving = true;
        movedDistance = 0f;

        SyncMoveDirectionFromRotation();
        moveCo = StartCoroutine(RollAndTakeOff());
    }

    private IEnumerator RollAndTakeOff()
    {
        // Burnun nereye bakıyorsa oraya ilerle
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

        if (isCrashing) yield break;

        // Başarılı kalkış → sallanıp fade
        yield return StartCoroutine(TakeOffAnimation());

        // Kalkış event'i
        OnPlaneTookOff?.Invoke();

        // Objeyi kaldır
        Destroy(gameObject);
    }

    private IEnumerator TakeOffAnimation()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        float rotationAmount = 10f; // sağa sola eğilme (derece)
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

    // === ÇARPIŞMA: pre-flight'ta kapalı, sonrasında spin+fade ve SAHNE RESTART ===
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCrashing) return;
        if (MultiGridManager.PreFlightPhase) return; // kalkıştan önce çarpışma yok
        if (!other || other.gameObject == this.gameObject) return;

        var otherPlane = other.GetComponent<AirplaneController>();
        if (!otherPlane) return; // sadece uçak-ucak

        Vector2 awayFromMe = (transform.position - otherPlane.transform.position).normalized;
        Vector2 awayFromOther = -awayFromMe;

        otherPlane.TriggerCrash(awayFromOther, alsoRestart:false);
        TriggerCrash(awayFromMe, alsoRestart:true); // bir taraf restart tetikler
    }

    public void TriggerCrash(Vector2 awayDir, bool alsoRestart)
    {
        if (isCrashing) return;
        StartCoroutine(CrashSpinFadeThenRestart(awayDir, alsoRestart));
    }

    private IEnumerator CrashSpinFadeThenRestart(Vector2 awayDir, bool alsoRestart)
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

        // Kaza event'i
        OnPlaneCrashed?.Invoke();

        if (alsoRestart)
        {
            yield return new WaitForSeconds(restartDelay);
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex); // SAHNEYİ YENİDEN YÜKLE
        }
    }

    // === YÖN SENKRONU ===
    public void SyncMoveDirectionFromRotation()
    {
        Vector3 fwd = (forwardAxis == ForwardAxis.Right ? transform.right : transform.up);
        Vector2 snapped = SnapToCardinal(fwd);

        data.direction = new Vector2Int(Mathf.RoundToInt(snapped.x), Mathf.RoundToInt(snapped.y));
        data.moveDirection = new Vector3(snapped.x, snapped.y, 0f);
    }

    private static Vector2 SnapToCardinal(Vector3 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }
}
