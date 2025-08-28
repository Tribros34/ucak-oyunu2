using UnityEngine;

public class FixedBoardCamera : MonoBehaviour
{
    public Camera cam;                     // boş bırakırsan otomatik alır
    public SpriteRenderer board;           // PlayfieldBoard
    [Range(0f,0.3f)] public float extra = 0.06f;

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!cam || !board) return;

        var b = board.bounds;
        // Kamerayı board merkezine koy
        cam.transform.position = new Vector3(b.center.x, b.center.y, cam.transform.position.z);
        cam.orthographic = true;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        // Genişliğe ve yüksekliğe göre gereken ortho size
        float sizeByWidth  = (b.size.x / aspect) * 0.5f;
        float sizeByHeight =  b.size.y * 0.5f;
        // Güvenli olanı seç + küçük pay
        cam.orthographicSize = Mathf.Max(sizeByWidth, sizeByHeight) * (1f + extra);
    }
}
