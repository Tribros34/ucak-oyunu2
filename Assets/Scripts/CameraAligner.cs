using UnityEngine;

public class CameraAligner : MonoBehaviour
{
    public Transform airportObject;
    public Camera targetCamera;

    [Header("Zoom Ayarları")]
    [Range(-1f, 1f)]
    public float zoomOffset = 0f; // Pozitif → uzaklaş, Negatif → yaklaş

    public float extraZoomOutFactor = 0.01f; // % boşluk payı (isteğe bağlı)

    void Start()
    {
        AlignCamera();
    }

#if UNITY_EDITOR
    // Oyun çalışırken ayar değişince anında gör
    void OnValidate()
    {
        if (airportObject != null && targetCamera != null)
            AlignCamera();
    }
#endif

    void AlignCamera()
    {
        if (airportObject == null || targetCamera == null)
        {
            Debug.LogError("Eksik referans!");
            return;
        }

        SpriteRenderer sr = airportObject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("SpriteRenderer eksik.");
            return;
        }

        Bounds bounds = sr.bounds;

        // Kamera'yı merkeze yerleştir
        Vector3 center = bounds.center;
        targetCamera.transform.position = new Vector3(center.x, center.y, targetCamera.transform.position.z);

        // Kamera zoom: tam oturt
        float screenAspect = (float)Screen.width / Screen.height;
        float targetAspect = bounds.size.x / bounds.size.y;
        float size;

        if (screenAspect >= targetAspect)
        {
            // Ekran daha geniş, yüksekliğe göre zoom
            size = bounds.size.y / 2f;
        }
        else
        {
            // Ekran daha dar, genişliğe göre zoom
            size = (bounds.size.x / screenAspect) / 2f;
        }

        // Ekstra boşluk + zoomOffset uygula
        size *= (1f + extraZoomOutFactor + zoomOffset);

        targetCamera.orthographicSize = size;
    }
}
