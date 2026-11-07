using UnityEngine;
using Worms.Gameplay;
public class CountdownExplode : MonoBehaviour
{
    public Transform piece1;
    public float timer = 1f;
    public bool countdown = false;
    private Player occupant;
    private bool exploded;
    public Player Occupant => occupant;
    public void RegisterPlayer(Player player) { occupant = player; }
    public void playerLeft() { occupant = null; }
    public void count() { countdown = true; }
    private GameSimulation simulation;
    private void OnEnable() { simulation = GameSimulation.For(this); simulation.Register(this); }
    private void OnDisable() { if (simulation) simulation.Unregister(this); }
    public void Advance(float seconds)
    {
        if (!countdown || exploded) return;
        timer -= seconds;
        if (timer < 0f) Explode();
    }
    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        if (occupant) occupant.detatch();
        occupant = null;
        if (piece1)
        {
            for (int i = 0; i < 8; i++)
            {
                var clone = Instantiate(piece1, transform.position, Quaternion.identity);
                var body = clone.GetComponent<Rigidbody2D>();
                if (body) body.AddForce(new Vector2(Random.Range(-15, 15), Random.Range(-15, 15)));
                clone.rotation = Random.rotation;
            }
        }
        Destroy(gameObject);
    }
}
