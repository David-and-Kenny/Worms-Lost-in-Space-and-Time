using UnityEngine;

/// <summary>
/// Preserves scene references while the unsupported 2015 ads SDK is archived.
/// A maintained SDK and consent flow must be integrated before enabling ads.
/// </summary>
public class GoogleMobileAdsDemoScript : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Ads are disabled in the development build (legacy SDK archived).");
    }
}
