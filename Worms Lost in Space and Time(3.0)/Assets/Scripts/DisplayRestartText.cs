using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class DisplayRestartText : MonoBehaviour {
	public Texture2D boxbg;
	private Texture2D t2D;
	private float rotAngle=90;
	private Vector2 pivotPoint;
//	private float startX;
//	private float endX;
//	public float duration= 1f;
//	private float startTime;
	// Use this for initialization
	public float cumulativeTime = 0.0f;
	public float adjustment;
	public float xPosition;
	void Start () {
		t2D = Resources.Load<Texture2D>("press_anywhere_to_try_again");
		float aspect = (float)Screen.height / (float)Screen.width;

		if (aspect > 1.7f)
						adjustment = 1f;
				else if (aspect >= 1.6)
						adjustment = .48f;
				else if (aspect >= 1.3)
						adjustment = .28f;
		xPosition= ((-Screen.width * .4f + 10f)-(Screen.height * .2f - 20))*adjustment;

	}
	
	// Update is called once per frame
	void Update () {
		cumulativeTime += Time.deltaTime;
		if (xPosition < (-Screen.width * .4f + 10f)) {
			xPosition+= (Screen.height * .2f - 20)* cumulativeTime/4;
		}
		if (xPosition >= (-Screen.width * .4f + 10f)*adjustment) {
			xPosition=	-Screen.width * .4f + 10f;
		}
	}

	void OnGUI(){
		//cumulativeTime += Time.deltaTime;
		pivotPoint = new Vector2(Screen.width/2,Screen.height/2);
		GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (xPosition, Screen.height / 2 - (Screen.width / 2) + 10, Screen.height/**.2f*/, Screen.height));

						if (GUI.Button (new Rect (10, 0, Screen.height * .2f - 20, Screen.width * .9f / 3f - 10f), "<size=45>Main\n Menu</size>")) {
				Application.LoadLevel ("StorySelect");

						}
						if (GUI.Button (new Rect (10, Screen.width * .9f / 3f, Screen.height * .2f - 20, Screen.width * .9f / 3f - 10f), "<size=45>Back</size>")) {
				Application.LoadLevel ("World1");
						}
		if (GUI.Button (new Rect (10, Screen.width * .9f * (2f / 3f), Screen.height * .2f - 20, Screen.width * .9f / 3f - 10f), "<size=45>Quit</size>")) {
										
								Application.Quit ();   
						}
		if (GUI.Button (new Rect ( Screen.height * .2f ,0, Screen.height -(Screen.height * .2f - 20),Screen.width),"",GUIStyle.none)) {
			
			Application.LoadLevel(Application.loadedLevel); 
		}
		GUI.EndGroup();
		var y = (Screen.height - t2D.height) / 2;
		var x = (Screen.width*1.2f - t2D.width) / 2;

		if(Time.time % 2 > 1)
			GUI.DrawTexture (new Rect (x,y, t2D.width, t2D.height), t2D);

	}

}
