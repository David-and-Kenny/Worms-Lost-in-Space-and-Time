using UnityEngine;
using Worms.Gameplay;
[RequireComponent(typeof(Animator))]
public class AlienC : MonoBehaviour
{
    public float attackDelay = 3f;
    public Transform projectile;
    private Animator animator;
    private float remaining;
    private GameSimulation simulation;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.Fixed;
        remaining = attackDelay;
    }
    private void OnEnable() { simulation = GameSimulation.For(this); simulation.Register(this); }
    private void OnDisable() { if (simulation) simulation.Unregister(this); }
    public void Advance(float seconds)
    {
        animator.SetInteger("AnimState", 0);
        if (attackDelay <= 0) return;
        remaining -= seconds;
        if (remaining > 0) return;
        remaining += attackDelay;
        Fire();
    }
    private void Fire() { animator.SetInteger("AnimState", 1); }
    // Animation event entry point.
    public void OnShoot()
    {
        if (!projectile) return;
        var clone = Instantiate(projectile, transform.position, Quaternion.identity);
        var body = clone.GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity = transform.up * -10f;
    }
}
