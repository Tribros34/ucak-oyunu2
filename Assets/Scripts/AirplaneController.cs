using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AirplaneData))]
public class AirplaneController : MonoBehaviour
{
    private AirplaneData data;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;

    private float movedDistance = 0f;
    private bool isMoving = false;

    private void Start()
    {
        data = GetComponent<AirplaneData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;
    }

    public void StartRolling() // bu fonksiyon dışarıdan tetiklenir
    {
        Debug.Log($"{gameObject.name} StartRolling çağrıldı");

        if (!isMoving)
        {
            isMoving = true;
            StartCoroutine(RollAndTakeOff());
        }
    }

    private IEnumerator RollAndTakeOff()
    {
        Debug.Log($"{gameObject.name} hareket etmeye başladı");

        while (movedDistance < data.requiredDistance)
        {
            float moveStep = data.moveSpeed * Time.deltaTime;
            transform.position += data.moveDirection.normalized * moveStep;
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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.2f, t);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - t);

            yield return null;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Airplane") && other.gameObject != this.gameObject)
    {
        Debug.Log($"{gameObject.name} çarpıştı → {other.gameObject.name}");

        Destroy(other.gameObject); // diğer uçağı sil
        Destroy(this.gameObject);  // kendini sil
    }
}

}
