using UnityEngine;
using Worms.Gameplay;
public class Wheel : MonoBehaviour
{
    public float speed = 50f;
    public float direction = -1f;
    public Transform holder;
    private GameSimulation simulation;
    private void OnEnable() { simulation = GameSimulation.For(this); simulation.Register(this); }
    private void OnDisable() { if (simulation) simulation.Unregister(this); }
    public void Advance(float seconds) { transform.Rotate(Vector3.forward, seconds * direction * speed); }
}
