using UnityEngine;
using Worms.Navigation;
using Worms.UI;
public class StoryScript : MonoBehaviour
{
    public Texture2D boxbg;
    public float WAIT_TIME = 0.04f;
    public string wholeText;
    public string nextLevel;
    public GameUiView view;
    private StoryFlow flow;
    private void Start() => flow = new StoryFlow(wholeText, WAIT_TIME);
    private void Update()
    {
        if (flow == null) return;
        if (flow.Update(Time.unscaledDeltaTime, UiPointer.Pressed)) SceneNavigator.Instance.RouteLegacyScene(nextLevel);
        if (view != null && view.Body != null) view.Body.text = flow.VisibleText;
    }
}
