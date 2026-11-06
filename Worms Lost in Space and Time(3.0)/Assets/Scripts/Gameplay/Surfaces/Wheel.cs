using UnityEngine;
// Rotation must precede attachment-following; Unity does not order default-priority Updates.
[DefaultExecutionOrder(-100)]
public class Wheel : MonoBehaviour
{
    public float speed = 50f;
    public float direction = -1f;
    public Transform holder;
    private void Update() { transform.Rotate(Vector3.forward, Time.deltaTime * direction * speed); }
}
