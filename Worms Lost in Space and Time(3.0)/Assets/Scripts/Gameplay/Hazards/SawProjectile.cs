using UnityEngine;
public class SawProjectile : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D target) { Destroy(gameObject); }
}
