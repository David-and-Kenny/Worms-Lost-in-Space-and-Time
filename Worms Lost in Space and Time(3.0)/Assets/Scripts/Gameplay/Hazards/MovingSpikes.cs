using UnityEngine;
public class MovingSpikes : MonoBehaviour
{
    public float speed = .5f;
    public Transform startPos;
    public Transform endPos;
    public System.Func<float> ElapsedTimeSource { get; set; }
    private void Update()
    {
        if (!startPos || !endPos) return;
        float elapsed = ElapsedTimeSource != null ? ElapsedTimeSource() : Time.timeSinceLevelLoad;
        float distance = Mathf.PingPong(elapsed * speed, 1f);
        transform.position = Vector3.Lerp(startPos.position, endPos.position, distance);
    }
    private void OnValidate()
    {
        if (!startPos || !endPos) Debug.LogWarning("Moving spikes require both path endpoints.", this);
    }
}
