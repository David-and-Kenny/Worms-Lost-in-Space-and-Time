using UnityEngine;
using Worms.Navigation;

public class LevelSelect : MonoBehaviour
{
    public Texture2D boxbg;
    public string SceneOne;
    public string SceneOneName;
    public string SceneTwo;
    public string SceneTwoName;
    public string SceneThree;
    public string SceneThreeName;
    public string SceneFour;
    public string SceneFourName;
    public string SceneFive;
    public string SceneFiveName;
    public string SceneSix;
    public string SceneSixName;
    public string SceneSeven;
    public string SceneSevenName;
    public string SceneEight;
    public string SceneEightName;
    public void Selectlevel(string scene) => SceneNavigator.Instance.RouteLegacyScene(scene);
    public void Back() => SceneNavigator.Instance.MainMenu();
}
