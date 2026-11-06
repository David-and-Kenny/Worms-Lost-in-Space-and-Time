using UnityEngine;
using Worms.Gameplay;
public class MovingSpikes : MonoBehaviour
{
    public float speed = .5f;
    public Transform startPos;
    public Transform endPos;
    private GameSimulation simulation;
    private void OnEnable() { simulation = GameSimulation.For(this); simulation.Register(this); }
    private void OnDisable() { if (simulation) simulation.Unregister(this); }
    public void Advance(float elapsed)
    {
        if (!startPos || !endPos) return;
        float distance = Mathf.PingPong(elapsed * speed, 1f);
        transform.position = Vector3.Lerp(startPos.position, endPos.position, distance);
    }
    private void OnValidate()
    {
        if (!startPos || !endPos) Debug.LogWarning("Moving spikes require both path endpoints.", this);
    }
}
