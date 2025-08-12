using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AirplaneData))]
[RequireComponent(typeof(Collider2D))]       // OnMouse… için gerekli
[RequireComponent(typeof(SpriteRenderer))]
public class DraggableAirplane : MonoBehaviour
{
    [Header("Tap / Click Ayarları")]
    [SerializeField] private float tapMaxDuration = 0.25f;       // tek tık max süre (sn)
    [SerializeField] private float tapMaxMove = 0.15f;           // tek tıkta izinli world hareketi
    [SerializeField] private float doubleTapMaxDelay = 0.40f;    // iki tık arası max süre (sn)
    [SerializeField] private float doubleTapScreenMaxDist = 22f; // iki tık arası ekran mesafesi (px)
    [SerializeField] private int rotateStep = 90;

    private Vector3 offset;
    private Vector3 originalPositionWorld;
    private Vector2Int originalGridPos;
    private int originalRotation;
    private bool isDragging;

    // world-tab ölçümü (tek tık için)
    private float tapDownTime;
    private Vector3 tapDownPosWorld;

    // screen-tab ölçümü (çift tık için)
    private float lastTapDownTime = -10f;
    private Vector2 lastTapDownScreenPos;

    // tek tıkı gecikmeli çalıştırmak için
    private float lastTapUpTime = -10f;
    private Vector3 lastTapUpWorldPos;
    private Coroutine pendingSingleTap;

    // hangar (ilk yer)
    private Vector2Int hangarGridPos;
    private int hangarRotation;
    private bool hangarCaptured = false;

    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        // Level yerleşimleri için birkaç frame bekleyip ilk yeri yakala
        StartCoroutine(CaptureHangarDeferred());
    }

    private IEnumerator CaptureHangarDeferred()
    {
        for (int i = 0; i < 6; i++) yield return new WaitForEndOfFrame();
        if (MultiGridManager.Instance && !hangarCaptured)
        {
            hangarGridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
            hangarRotation = data.rotation;
            hangarCaptured = true;
        }
    }

    private void OnMouseDown()
    {
        // --- Çift tık tespiti (screen space) ---
        float now = Time.time;
        Vector2 currScreen = (Vector2)Input.mousePosition;
        bool isDoubleTapDown =
            (now - lastTapDownTime <= doubleTapMaxDelay) &&
            (Vector2.Distance(currScreen, lastTapDownScreenPos) <= doubleTapScreenMaxDist);

        lastTapDownTime = now;
        lastTapDownScreenPos = currScreen;

        if (isDoubleTapDown)
        {
            if (pendingSingleTap != null) { StopCoroutine(pendingSingleTap); pendingSingleTap = null; }
            TryReturnToHangar();   // <<< hangara dön (Obstacle yok sayılacak)
            isDragging = false;
            return;
        }

        // --- Drag hazırlığı ---
        originalPositionWorld = transform.position;
        originalGridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
        originalRotation = data.rotation;

        offset = transform.position - GetMouseWorldPosition();
        isDragging = true;

        // Sürüklerken kendi işgalini bırak ki CanPlace doğru çalışsın
        if (MultiGridManager.Instance) MultiGridManager.Instance.Release(data);

        // Tek tık ayrımı için world ölçümü
        tapDownTime = Time.time;
        tapDownPosWorld = GetMouseWorldPosition();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition() + offset;
        mousePos.z = transform.position.z;
        transform.position = mousePos;

        // Geçici grid hizalama ve önizleme (yeşil/kırmızı)
        Vector2Int gridPos = MultiGridManager.Instance.GetGridPosition(transform.position);

        bool overlapsObstacle = MultiGridManager.Instance.OverlapsObstacle(data, gridPos, data.rotation);
        bool overlapsOccupied = FootprintOverlapsOccupied(gridPos, data.rotation);

        bool isValid = !(overlapsObstacle || overlapsOccupied);
        spriteRenderer.color = isValid ? Color.green : Color.red;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector2Int gridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
        Vector3 snappedPos = MultiGridManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

        bool canPlace = MultiGridManager.Instance.CanPlace(data, gridPos, data.rotation);
        if (!canPlace)
        {
            // Geçersiz: eski konuma dön + eski işgali geri kur
            transform.position = originalPositionWorld;
            MultiGridManager.Instance.Occupy(data, originalGridPos, originalRotation);
            spriteRenderer.color = originalColor;
            return;
        }

        // Snap + işgal et
        transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        MultiGridManager.Instance.Occupy(data, gridPos, data.rotation);
        spriteRenderer.color = originalColor;

        // Hangar henüz alınmadıysa ilk başarılı snap'i hangar kabul et
        if (!hangarCaptured)
        {
            hangarGridPos = gridPos;
            hangarRotation = data.rotation;
            hangarCaptured = true;
        }

        // >>> YÖN SENKRONU: yerleştirme sonrası
        var acAfterPlace = GetComponent<AirplaneController>();
        if (acAfterPlace) acAfterPlace.SyncMoveDirectionFromRotation();

        // ---- Tek tık (gecikmeli) ----
        float tapDuration = Time.time - tapDownTime;
        float tapMove = Vector2.Distance(tapDownPosWorld, GetMouseWorldPosition());
        bool isTap = tapDuration <= tapMaxDuration && tapMove <= tapMaxMove;

        if (isTap)
        {
            lastTapUpTime = Time.time;
            lastTapUpWorldPos = GetMouseWorldPosition();

            if (pendingSingleTap != null) StopCoroutine(pendingSingleTap);
            pendingSingleTap = StartCoroutine(DelayedSingleTapToRotate(lastTapUpTime, lastTapUpWorldPos));
        }
    }

    private IEnumerator DelayedSingleTapToRotate(float tapTimeSnapshot, Vector3 tapPosSnapshot)
    {
        yield return new WaitForSeconds(doubleTapMaxDelay);

        bool stillSameTap = Mathf.Abs(lastTapUpTime - tapTimeSnapshot) < 0.0001f &&
                            Vector2.Distance(lastTapUpWorldPos, tapPosSnapshot) <= tapMaxMove;

        if (stillSameTap)
            TryRotate90AtCurrentCell();

        pendingSingleTap = null;
    }

    private void TryRotate90AtCurrentCell()
    {
        int oldRot = data.rotation;
        int newRot = (oldRot + rotateStep) % 360;
        Vector2Int pos = MultiGridManager.Instance.GetGridPosition(transform.position);

        // önce mevcut işgali bırak
        MultiGridManager.Instance.Release(data);

        if (MultiGridManager.Instance.CanPlace(data, pos, newRot))
        {
            data.rotation = newRot;
            transform.rotation = Quaternion.Euler(0, 0, newRot);

            Vector3 snappedPos = MultiGridManager.Instance.GetWorldPosition(pos.x, pos.y);
            transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);

            MultiGridManager.Instance.Occupy(data, pos, newRot);

            var acAfterRotate = GetComponent<AirplaneController>();
            if (acAfterRotate) acAfterRotate.SyncMoveDirectionFromRotation();
        }
        else
        {
            data.rotation = oldRot;
            transform.rotation = Quaternion.Euler(0, 0, oldRot);
            MultiGridManager.Instance.Occupy(data, pos, oldRot);
            StartCoroutine(FlashInvalid());
        }
    }

    private void TryReturnToHangar()
    {
        // Hangar hiç yakalanmadıysa, şimdiki konumu hangar kabul et (son çare)
        if (!hangarCaptured && MultiGridManager.Instance)
        {
            hangarGridPos = MultiGridManager.Instance.GetGridPosition(transform.position);
            hangarRotation = data.rotation;
            hangarCaptured = true;
        }

        MultiGridManager.Instance.Release(data);

        // >>> Burada OBSTACLE’I YOK SAYIYORUZ (sınır + occupied kontrolü var)
        if (hangarCaptured && CanReturnIgnoringObstacles(data, hangarGridPos, hangarRotation))
        {
            data.rotation = hangarRotation;
            transform.rotation = Quaternion.Euler(0, 0, hangarRotation);

            Vector3 snapped = MultiGridManager.Instance.GetWorldPosition(hangarGridPos.x, hangarGridPos.y);
            transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);

            MultiGridManager.Instance.Occupy(data, hangarGridPos, hangarRotation);

            var ac = GetComponent<AirplaneController>();
            if (ac) ac.SyncMoveDirectionFromRotation();
        }
        else
        {
            // Dönemiyorsak (sınır dışı veya hücreler dolu), mevcut hücreyi geri işgal et ve uyar
            Vector2Int curr = MultiGridManager.Instance.GetGridPosition(transform.position);
            MultiGridManager.Instance.Occupy(data, curr, data.rotation);
            StartCoroutine(FlashInvalid());
        }
    }

    // ----- Sadece hangara dönüş için: obstacle'ı YOK say -----
    private bool CanReturnIgnoringObstacles(AirplaneData d, Vector2Int gridPos, int rotation)
    {
        foreach (var c in GetFootprintCells(d.gridSize, gridPos, rotation))
        {
            if (!MultiGridManager.Instance.IsWithinBounds(c)) return false;
            if (MultiGridManager.Instance.IsCellOccupied(c)) return false; // diğer uçaklarla çakışma
        }
        return true; // obstacle kontrolü YOK
    }

    // -------- Önizleme yardımcıları --------
    private bool FootprintOverlapsOccupied(Vector2Int originCell, int rotation)
    {
        foreach (var c in GetFootprintCells(data.gridSize, originCell, rotation))
        {
            if (!MultiGridManager.Instance.IsWithinBounds(c)) return true;
            if (MultiGridManager.Instance.IsCellOccupied(c)) return true;
        }
        return false;
    }

    private IEnumerable<Vector2Int> GetFootprintCells(Vector2Int size, Vector2Int originCell, int rotation)
    {
        int w = size.x, h = size.y;
        if ((rotation % 180) != 0) { int t = w; w = h; h = t; }

        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
                yield return new Vector2Int(originCell.x + dx, originCell.y + dy);
    }

    private IEnumerator FlashInvalid()
    {
        var c = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = c;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(mouse);
    }
}
