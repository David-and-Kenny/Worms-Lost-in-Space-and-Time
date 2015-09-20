using UnityEngine;
using System.Collections;

public class LevelFinish : MonoBehaviour {
	public Texture2D boxbg;
	private float rotAngle=90;
	private Vector2 pivotPoint;
	public string nextLevel;
	public float adjustment=20f;
	private float time=0f;
	// Use this for initialization
	void Start () {
		//time = Mathf.Round(Time.timeSinceLevelLoad*100f)/100f;
		float aspect = (float)Screen.height / (float)Screen.width;
		time = PlayerPrefs.GetFloat ("FinishedTime");
		PlayerPrefs.DeleteKey ("FinishedTime");
		if (aspect > 1.7f)
			adjustment = 50;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnGUI(){
		var centeredStyle = GUI.skin.GetStyle("Label");
		centeredStyle.alignment = TextAnchor.UpperCenter;
		var guiWidth = Screen.height * .6f;
		var guiHeight = Screen.width * .7f;
	pivotPoint = new Vector2(Screen.width/2,Screen.height/2);
	GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (Screen.width/2-guiWidth/2, Screen.height/2-guiHeight/2, Screen.width, Screen.height));
		GUI.Box (new Rect (0,0,guiWidth,guiHeight), boxbg);
		GUI.Label(new Rect(10, 10, guiWidth-adjustment,guiHeight/4-2.5f), "<size=25>Good Job "+ time.ToString()+"s</size>", centeredStyle);
		if (GUI.Button(new Rect(10,guiHeight/4,guiWidth-adjustment,guiHeight/4-2.5f),"<size=45>Continue</size>"))
	{
		

		Application.LoadLevel(nextLevel);
		
		
	}
		if (GUI.Button(new Rect(10,guiHeight/2,guiWidth-adjustment,guiHeight/4-2.5f),"<size=45>Back</size>"))
	{
		Application.LoadLevel("World1");
	}
	
	
		if (GUI.Button(new Rect(10,guiHeight*.75f,guiWidth-adjustment,guiHeight/4-2.5f),"<size=45>Quit</size>"))
	{
		Application.Quit();
	}
	
	GUI.EndGroup();

}
}
