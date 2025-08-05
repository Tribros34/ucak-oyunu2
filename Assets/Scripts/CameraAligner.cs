using UnityEngine;

public class CameraAligner : MonoBehaviour
{
    public Transform airportObject;
    public Camera targetCamera;
    public float extraZoomOutFactor = 0.01f; // %1 boşluk payı (isteğe bağlı)

    void Start()
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

        if (screenAspect >= targetAspect)
        {
            // Ekran daha geniş, yüksekliğe göre zoom
            targetCamera.orthographicSize = bounds.size.y / 2f * (1f + extraZoomOutFactor);
        }
        else
        {
            // Ekran daha dar, genişliğe göre zoom
            float camSize = (bounds.size.x / screenAspect) / 2f;
            targetCamera.orthographicSize = camSize * (1f + extraZoomOutFactor);
        }
    }
}
