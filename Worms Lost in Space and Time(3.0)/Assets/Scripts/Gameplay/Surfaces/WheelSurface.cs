using UnityEngine;

/// <summary>Explicitly marks a rotating object as an attachable surface.</summary>
[RequireComponent(typeof(Wheel))]
public sealed class WheelSurface : MonoBehaviour
{
    public Wheel wheel;
    private void OnValidate()
    {
        if (!wheel || !wheel.holder) Debug.LogWarning("Wheel surface requires its wheel and authored holder references.", this);
    }
}
