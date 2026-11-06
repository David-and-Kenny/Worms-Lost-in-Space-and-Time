using UnityEngine;
using Worms;
public class Explode : MonoBehaviour
{
    public Transform arm1;
    public Transform arm2;
    public Transform head;
    public Transform torso;
    public Transform leg1;
    public Transform leg2;
    public Transform jetpack;
    public int totalParts;
    private bool exploded;
    private void OnTriggerEnter2D(Collider2D target)
    {
        if (target.CompareTag("Deadly")) OnExplode();
    }
    private void OnCollisionEnter2D(Collision2D target)
    {
        if (target.gameObject.CompareTag("Deadly")) OnExplode();
    }
    public void OnExplode()
    {
        if (exploded) return;
        var host = LevelSessionHost.GetOrCreate();
        if (host && !host.Session.TryDie()) return;
        exploded = true;
        Transform[] parts = { arm1, arm2, head, torso, leg1, leg2, jetpack };
        foreach (var part in parts)
        {
            if (!part) continue;
            var clone = Instantiate(part, transform.position, Quaternion.identity);
            var body = clone.GetComponent<Rigidbody2D>();
            if (body) body.AddForce(new Vector2(Random.Range(-50, 50), Random.Range(100, 400)));
        }
        Destroy(gameObject);
        // Procedural legacy fixtures have no catalog session or scene UI presenter.
        if (!host)
            new GameObject("ClickToContinue").AddComponent<DisplayRestartText>();
    }
}
