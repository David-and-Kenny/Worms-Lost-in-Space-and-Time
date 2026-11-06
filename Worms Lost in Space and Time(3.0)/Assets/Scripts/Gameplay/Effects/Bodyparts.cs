using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class Bodyparts : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color start;
    private float elapsed;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        start = spriteRenderer.color;
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        spriteRenderer.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, elapsed / 2f));
        if (spriteRenderer.color.a <= 0f) Destroy(gameObject);
    }
}
