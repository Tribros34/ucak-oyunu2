using UnityEngine;

[ExecuteAlways]
public class CameraAligner : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;          // Boşsa otomatik Camera.main alınır
    [Tooltip("Level prefab instance veya tüm sahneyi kapsayan parent")]
    public Transform airportRoot;        // Genelde GameArea/PlayfieldBoard

    [Tooltip("Inspector referansını KİLİTLE. True ise runtime'da kimse değiştirmez.")]
    public bool lockAirportRootFromInspector = true;

    [Header("Fit & Padding")]
    public bool fitWidthInPortrait = true;   // Portrede genişliğe göre sığdır
    [Range(-1f, 1f)] public float zoomOffset = 0f;        // + uzaklaş / - yaklaş (ince ayar)
    [Min(0f)] public float extraZoomOutFactor = 0.06f;    // kenar payı
    [Min(0f)] public float portraitExtraZoom = 0.14f;     // portrede ekstra pay
    [Min(0.01f)] public float minOrthoSize = 0.01f;

    // İstersen GameFlowMinimal burayı çağırabilir;
    // fakat inspector kilitliyse airportRoot'u DEĞİŞTİRMEZ.
    public void AttachLevel(GameObject levelGO)
    {
        if (!lockAirportRootFromInspector || airportRoot == null)
            airportRoot = levelGO ? levelGO.transform : null;

        if (targetCamera == null) targetCamera = Camera.main;
        AlignCamera();
    }

    public void AlignNow() => AlignCamera();

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        AlignCamera();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && (airportRoot != null) && (targetCamera != null || Camera.main != null))
            AlignCamera();
    }
#endif

    public void AlignCamera()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (airportRoot == null || cam == null) return;

        // 1) airportRoot altındaki TÜM Renderer'ların birleşik bounds'ı
        var renderers = airportRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // 2) Kamera XY merkezini sahnenin merkezine taşı
        var pos = cam.transform.position;
        pos.x = b.center.x; pos.y = b.center.y;
        cam.transform.position = pos;

        // 3) Ekran oranı (safe area ile)
        Rect sa = Screen.safeArea;
        float aspect = sa.width / Mathf.Max(1f, sa.height);

        // 4) Ortografik kamera: fit width (portre) + paylar
        if (cam.orthographic)
        {
            float sizeByWidth  = (b.size.x / aspect) * 0.5f;
            float sizeByHeight =  b.size.y * 0.5f;

            float baseSize = (fitWidthInPortrait && Screen.height > Screen.width)
                ? sizeByWidth
                : Mathf.Max(sizeByWidth, sizeByHeight);

            float pad = 1f + extraZoomOutFactor + (Screen.height > Screen.width ? portraitExtraZoom : 0f) + zoomOffset;
            cam.orthographicSize = Mathf.Max(minOrthoSize, baseSize * Mathf.Max(0.01f, pad));
            return;
        }

        // 5) Perspektif için basit fit-width
        float halfWidth = b.size.x * 0.5f;
        float distance = halfWidth / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / aspect;
        distance *= 1f + extraZoomOutFactor + (Screen.height > Screen.width ? portraitExtraZoom : 0f) + zoomOffset;
        cam.transform.position = new Vector3(b.center.x, b.center.y, b.center.z - Mathf.Abs(distance));
    }
}
