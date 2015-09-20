using UnityEngine;
using System.Collections;

public class StorySelect : MonoBehaviour {  
	public Texture2D boxbg;
	public string StoryOne;
	public string StoryOneName ="Area One";
	public string StoryTwo;
	public string StoryTwoName ="Area Two";
	public string StoryThree;
	public string StoryThreeName ="Area Three";
	private float rotAngle=90;
	private Vector2 pivotPoint;
	public int currentlevel;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnGUI(){
		pivotPoint = new Vector2(Screen.width/2,Screen.height/2);
		GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (Screen.width/2-250, Screen.height/2-330, Screen.width, Screen.height));
		GUI.Box (new Rect (0,0,520,660), boxbg);
		
		if (GUI.Button (new Rect (10, 10, 500, 120), "<size=45>Continue</size>")) {

						GameController controller = new GameController ();
						int levelcount = controller.Load ();
						Application.LoadLevel ("Level" + levelcount);
				}
		if (GUI.Button(new Rect(10,140,500,120),"<size=45>"+StoryOneName+"</size>"))
		{
			Application.LoadLevel(StoryOne);
		}
		if (GUI.Button(new Rect(10,270,500,120),"<size=45>"+StoryTwoName+"</size>"))
		{
			Application.LoadLevel(StoryTwo);
		}
		if (GUI.Button(new Rect(10,400,500,120),"<size=45>"+StoryThreeName+"</size>"))
		{
			Application.LoadLevel(StoryThree);     
		}
		
		if (GUI.Button(new Rect(10,530,500,120),"<size=45>Quit</size>"))
		{
			Application.Quit();
		}
		
		GUI.EndGroup();
	}
}
