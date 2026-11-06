using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class AlienC : MonoBehaviour
{
    public float attackDelay = 3f;
    public Transform projectile;
    private Animator animator;
    private void Awake() { animator = GetComponent<Animator>(); }
    private void Start() { if (attackDelay > 0f) StartCoroutine(OnAttack()); }
    private void Update() { animator.SetInteger("AnimState", 0); }
    private IEnumerator OnAttack()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackDelay);
            Fire();
        }
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
