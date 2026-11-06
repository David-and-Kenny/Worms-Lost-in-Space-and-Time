using UnityEngine;
using Worms.Navigation;
public class StorySelect : MonoBehaviour
{
    public Texture2D boxbg;
    public string StoryOne;
    public string StoryOneName = "Area One";
    public string StoryTwo;
    public string StoryTwoName = "Area Two";
    public string StoryThree;
    public string StoryThreeName = "Area Three";
    public int currentlevel;
    public void Continue() => SceneNavigator.Instance.Continue();
    public void OpenWorldOne() => SceneNavigator.Instance.OpenWorld("world-1");
    public void OpenWorldTwo() => SceneNavigator.Instance.OpenWorld("world-2");
    public void Quit() => SceneNavigator.Instance.Quit();
}
