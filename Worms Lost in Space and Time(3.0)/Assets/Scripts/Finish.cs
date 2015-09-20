using UnityEngine;
using System.Collections;

public class Finish : MonoBehaviour {
	protected string currentLevel;
	public string nextLevel;

	// Use this for initialization
	void Start () {
		currentLevel = Application.loadedLevelName;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnTriggerEnter2D(Collider2D target){
		if (target.gameObject.tag == "Player") {
			Destroy (target.gameObject);
			GameObject go = new GameObject ("LevelFinish");
			LevelFinish script = go.AddComponent<LevelFinish> ();
			script.nextLevel = nextLevel;

			//Application.loadedLevelName;
			//go.AddComponent<DisplayRestartText> ();
		}

}

}
