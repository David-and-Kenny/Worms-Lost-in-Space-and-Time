using UnityEngine;
using System.Collections;

public class LevelSelect : MonoBehaviour {

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
	private float rotAngle=90;
	private Vector2 pivotPoint;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnGUI(){
		pivotPoint =new Vector2(Screen.width/2,Screen.height/2);
		GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
		
		GUI.Box (new Rect (0,0,800,600), boxbg);
		//first row add 90 to first parameter to space horizontally
		if (GUI.Button(new Rect(25,10,170,75),"<size=40>"+SceneOneName+"</size>"))
		{
			Application.LoadLevel(SceneOne);
		}
		if (GUI.Button(new Rect(215,10,170,75),"<size=40>"+SceneTwoName+"</size>"))
		{
			Application.LoadLevel(SceneTwo);
		}
		if (GUI.Button(new Rect(405,10,170,75),"<size=40>"+SceneThreeName+"</size>"))
		{
			Application.LoadLevel(SceneThree);     
		}
		if (GUI.Button(new Rect(595,10,170,75),"<size=40>"+SceneFourName+"</size>"))
		{
			Application.LoadLevel(SceneFour);     
		}
		if (GUI.Button(new Rect(25,100,170,75),"<size=40>"+SceneFiveName+"</size>"))
		{
			Application.LoadLevel(SceneFive);
		}
		if (GUI.Button(new Rect(215,100,170,75),"<size=40>"+SceneSixName+"</size>"))
		{
			Application.LoadLevel(SceneSix);
		}
		if (GUI.Button(new Rect(405,100,170,75),"<size=40>"+SceneSevenName+"</size>"))
		{
			Application.LoadLevel(SceneSeven);     
		}
		if (GUI.Button(new Rect(595,100,170,75),"<size=40>"+SceneEightName+"</size>"))
		{
			Application.LoadLevel(SceneEight);     
		}
		
		
		if (GUI.Button(new Rect(25,500,120,40),"<size=35>"+"Back"+"</size>"))
		{
			Application.LoadLevel("StorySelect");
		}
		GUI.EndGroup();
	
	}
}
