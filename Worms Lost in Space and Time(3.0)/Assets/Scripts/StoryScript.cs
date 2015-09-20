using UnityEngine;
using System.Collections;

public class StoryScript : MonoBehaviour {
	/*public Texture2D boxbg;

	private float rotAngle=90;
	private Vector2 pivotPoint;
	public string text;
	public float letterPause=0.2f;
	private string displayText;

	void OnStart(){
		TypeText ();
	}

	void Update()
	{

	}
	
	public IEnumerator TypeText(){
		foreach (char letter in text.ToCharArray()) {
			print ("typing");
			displayText+= letter;
			yield return new WaitForSeconds(letterPause);
		}
	}
		
		
	void OnGUI(){
		print ("happening");
		pivotPoint = new Vector2(Screen.width/2,Screen.height/2);
		GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (Screen.width/2-250, Screen.height/2-330, Screen.width, Screen.height));
		GUI.Box (new Rect (0,0,520,660), boxbg);
		GUI.Label(new Rect(10, 10, 510, 650), displayText);
		GUI.EndGroup();
	}
}*/
	public Texture2D boxbg;
	public float WAIT_TIME = 0.04f;
	private float rotAngle=90;
	private Vector2 pivotPoint;
	private float waitTimer = 0.0f;
	public string wholeText;
	private string typewriterText = "";
	private int currentIndex = 0;
	private bool firstTouch;
	public string nextLevel;
	private int frameWait=0;
	private float textScale;
	// Use this for initialization
	void Start () {
		/*float aspect = (float)Screen.height / (float)Screen.width;
		if (aspect > 1.7f)
			textScale = 20f;
		else if (aspect >= 1.6)
			textScale = 20f;
		else if (aspect >= 1.3)
			textScale = 20f;*/
	}

	
	void Update ()
	{
		if (((Input.touchCount >= 1 || Input.GetMouseButtonDown (0))) && firstTouch && frameWait==0) {

			Application.LoadLevel (nextLevel);
		}
		if (((Input.touchCount >=1 ||Input.GetMouseButtonDown (0))) && !firstTouch){
			typewriterText=wholeText;
			firstTouch=true;
			frameWait=20;
		}

		waitTimer += Time.deltaTime;
		
		if (waitTimer > WAIT_TIME && currentIndex < wholeText.Length && !firstTouch)
		{            
			typewriterText += wholeText[currentIndex];
			waitTimer = 0.0f;
			++currentIndex;
		}   
		if(frameWait>0)
		frameWait--;
	}
	void OnGUI(){
		var centeredStyle = GUI.skin.GetStyle("Label");
		centeredStyle.alignment = TextAnchor.UpperLeft;
		var guiWidth = Screen.height * .6f;
		var guiHeight = Screen.width * .7f;
		pivotPoint = new Vector2(Screen.width/2,Screen.height/2);
		GUIUtility.RotateAroundPivot (rotAngle, pivotPoint); 
		GUI.BeginGroup (new Rect (Screen.width/2-guiWidth/2, Screen.height/2-guiHeight/2, Screen.width, Screen.height));
		GUI.Box (new Rect (0,0,guiWidth,guiHeight), boxbg);
		GUI.Label(new Rect(10, 10, guiWidth-60,guiHeight-20), "<size=24>"+typewriterText+"</size>");
		GUI.EndGroup();
	}
}
