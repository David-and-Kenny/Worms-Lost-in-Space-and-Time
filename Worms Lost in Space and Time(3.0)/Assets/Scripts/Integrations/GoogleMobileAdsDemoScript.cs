using UnityEngine;

/// <summary>
/// Preserves legacy scene references. MonetizationHost owns the modern integration;
/// this component must never initialize another SDK instance.
/// </summary>
public class GoogleMobileAdsDemoScript : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Legacy ad component is inactive; monetization is managed by MonetizationHost.");
    }
}
