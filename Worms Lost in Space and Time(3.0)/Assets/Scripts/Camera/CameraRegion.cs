using UnityEngine;

/// <summary>Authored framing metadata, independent of object and child names.</summary>
public sealed class CameraRegion : MonoBehaviour
{
    public Transform center;
    private void OnValidate()
    {
        if (!center) Debug.LogWarning("Camera region requires a center reference.", this);
    }
}
