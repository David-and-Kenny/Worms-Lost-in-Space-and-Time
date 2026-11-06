using UnityEngine;
using Worms.Navigation;
using Worms.UI;
public class LevelSelectScript : MonoBehaviour
{
    private void Start()
    {
        foreach (var button in FindObjectsByType<LevelButtonBinding>(FindObjectsSortMode.None)) button.Refresh();
    }
    public void Selectlevel(string worldLevel) => SceneNavigator.Instance.RouteLegacyScene(worldLevel);
}
