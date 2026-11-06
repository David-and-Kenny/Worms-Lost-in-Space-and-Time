using UnityEngine;
public class FinishZone : MonoBehaviour
{
    private Transform playerPos;
    public Transform endPos;
    private bool collided;
    public float speed = 5;
    private void Update()
    {
        if (!collided || !playerPos || !endPos) return;
        playerPos.position = Vector3.MoveTowards(playerPos.position, endPos.position, speed * Time.deltaTime);
        if (playerPos.position == endPos.position) targetReached();
    }
    private void OnTriggerEnter2D(Collider2D target)
    {
        if (!target.CompareTag("Player")) return;
        var body = target.GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity *= .5f;
        collided = true;
        playerPos = target.transform;
    }
    private void targetReached()
    {
        collided = false;
        var body = playerPos.GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity = Vector2.zero;
    }
}
