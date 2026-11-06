using UnityEngine;
using Worms.Navigation;
public class LevelFinish : MonoBehaviour
{
    public Texture2D boxbg;
    public string nextLevel;
    public float adjustment = 20f;
    public void Continue() => SceneNavigator.Instance.OpenNext(Worms.LevelSessionHost.Current?.Session?.Completion);
    public void Back() => SceneNavigator.Instance.BackToWorld();
    public void Quit() => SceneNavigator.Instance.Quit();
}
