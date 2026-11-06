using UnityEngine;
public class Wheel : MonoBehaviour
{
    public float speed = 50f;
    public float direction = -1f;
    public Transform holder;
    private void Update() { transform.Rotate(Vector3.forward, Time.deltaTime * direction * speed); }
}
