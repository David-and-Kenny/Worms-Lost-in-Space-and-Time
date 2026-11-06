using UnityEngine;
[RequireComponent(typeof(Animator))]
public class AlienA : MonoBehaviour
{
    private Animator animator;
    private bool readyToAttack;
    private void Awake() { animator = GetComponent<Animator>(); }
    private void OnTriggerStay2D(Collider2D target)
    {
        if (!target.CompareTag("Player")) return;
        if (readyToAttack)
        {
            var explosion = target.GetComponent<Explode>();
            if (explosion) explosion.OnExplode();
            readyToAttack = false;
            animator.SetInteger("AnimState", 0);
        }
        else animator.SetInteger("AnimState", 1);
    }
    private void OnTriggerExit2D(Collider2D target)
    {
        readyToAttack = false;
        animator.SetInteger("AnimState", 0);
    }
    // Animation event entry point.
    public void Attack() { readyToAttack = true; }
}
