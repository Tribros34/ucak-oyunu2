using UnityEngine;

public class CameraAligner : MonoBehaviour
{
    [Header("References")]
    public Transform airportRoot;   // TÜM sahnenin parent’ı (pist+hangarlar+terminal...)
    public Camera targetCamera;

    [Header("Zoom")]
    [Range(-1f, 1f)]
    public float zoomOffset = 0f;           // + uzaklaş / - yaklaş
    public float extraZoomOutFactor = 0.06f; // kenar payı
    public float portraitExtraZoom = 0.14f; // portrede ekstra pay

    void Start()
    {
        AlignCamera();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (airportRoot != null && targetCamera != null)
            AlignCamera();
    }
#endif

    public void AlignCamera()
    {
        if (!airportRoot || !targetCamera)
        {
            Debug.LogError("CameraAligner: Referans eksik!");
            return;
        }

        // 1) Tüm Renderer’ları kapsayan TOPLAM bounds
        var renderers = airportRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("CameraAligner: airportRoot altında Renderer yok.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // 2) Kamerayı merkeze koy
        Vector3 center = bounds.center;
        targetCamera.transform.position = new Vector3(center.x, center.y, targetCamera.transform.position.z);

        // 3) SAFE AREA’YA göre ekran oranı (çentik/alt bar telafisi)
        Rect sa = Screen.safeArea;
        float screenAspect = sa.width / sa.height;

        // 4) PORTREDE GENİŞLİĞE GÖRE Sığdır (fit width)
        // Ortho kamerada görünen yükseklik = orthographicSize * 2
        // Genişlik bazlı fit: size = (targetWidth / screenAspect) / 2
        float size = (bounds.size.x / screenAspect) * 0.5f;

        // 5) Paylar
        bool isPortrait = Screen.height > Screen.width; // zaten portrait kullanıyoruz
        float pad = 1f + extraZoomOutFactor + zoomOffset + (isPortrait ? portraitExtraZoom : 0f);
        size *= Mathf.Max(0.01f, pad);

        targetCamera.orthographicSize = Mathf.Max(0.01f, size);
    }
}
