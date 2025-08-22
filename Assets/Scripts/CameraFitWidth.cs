using UnityEngine;

/// <summary>
/// Verilen sahne alanını her zaman YATAYDA ekrana sığdırır (Fit Width).
/// Hedefin genişliği tamamen görünür; dikeyde taşma olabilir.
/// </summary>
[ExecuteAlways]
public class CameraFitWidth : MonoBehaviour
{
    [Header("Referanslar")]
    public Camera cam;                 // Orthographic kamera
    public Transform targetRoot;       // Havaalanı / tüm alanın parent'ı

    [Header("Ayarlar")]
    [Tooltip("Etrafında bırakılacak yüzde boşluk (0.02 = %2)")]
    [Range(0f, 0.2f)] public float padding = 0.02f;

    [Tooltip("Kamerayı dikeyde biraz kaydırmak için (world units)")]
    public float yOffset = 0f;

    void Reset()
    {
        cam = Camera.main;
        if (targetRoot == null) targetRoot = transform;
    }

    void OnEnable()  { Apply(); }
#if UNITY_EDITOR
    void OnValidate(){ Apply(); }
#endif

    public void Apply()
    {
        if (!cam || !targetRoot) return;
        if (!cam.orthographic)
        {
            Debug.LogWarning("CameraFitWidth: Kamera ortografik değil. Orthographic moduna alın.");
            return;
        }

        // 1) targetRoot altındaki TÜM Renderer'ların birleşik Bounds'unu al
        var renderers = targetRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // 2) Kamerayı merkeze yerleştir
        var p = cam.transform.position;
        cam.transform.position = new Vector3(b.center.x, b.center.y + yOffset, p.z);

        // 3) ORTHO SIZE: GENİŞLİĞE GÖRE HESAP
        float screenAspect = (float)Screen.width / Screen.height;
        float size = (b.size.x / screenAspect) * 0.5f;

        // 4) Padding uygula
        size *= 1f + padding;

        cam.orthographicSize = size;
    }
}
