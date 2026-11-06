using UnityEngine;
using Worms.UI;
/// <summary>Compatibility entry point for noncatalog explosion fixtures.</summary>
public class DisplayRestartText : MonoBehaviour
{
    public Texture2D boxbg;
    public float cumulativeTime;
    public float adjustment;
    public float xPosition;
    private void Start()
    {
        foreach (var view in FindObjectsByType<GameUiView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (view.ScreenKind == UiScreen.Death) return;
        var prefab = Resources.Load<GameObject>("UI/DeathPanel");
        if (prefab != null) Instantiate(prefab);
    }
}
