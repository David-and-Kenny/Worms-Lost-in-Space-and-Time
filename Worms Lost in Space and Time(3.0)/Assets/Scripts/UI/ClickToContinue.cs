using UnityEngine;
using Worms.Navigation;
using Worms.UI;
public class ClickToContinue : MonoBehaviour
{
    public string scene;
    private void Update()
    {
        if (UiPointer.PressBegan && !UiPointer.OverControl) LoadScene();
    }
    public void LoadScene() => SceneNavigator.Instance.RouteLegacyScene(scene);
}
