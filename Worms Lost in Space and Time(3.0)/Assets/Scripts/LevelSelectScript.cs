using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelSelectScript : MonoBehaviour {

	GameController controller;
	void  Start (){
		controller = new GameController ();
				CheckLockedLevels();
		CheckStars ();

	}
	
	//Level to load on button click. Will be used for Level button click event 
	public void Selectlevel(string worldLevel){
		SceneManager.LoadScene(worldLevel); //load the level
	}
	

	
	//function to check for the levels locked
	void  CheckLockedLevels (){
		//loop through the levels of a particular world

		int levelcount = controller.Load ();
		for(int j = 1; j < levelcount; j++){
			var lockedLevel = GameObject.Find("LockedLevel" + (j + 1));
			if (lockedLevel != null) lockedLevel.SetActive(false);
				
			}
		}
	void CheckStars (){

		bool[] starCount = controller.LoadArray();
		for(int i=1; i<starCount.Length; i++){
			if(starCount[i]==false){
		var conch = GameObject.Find("Conch" + i);
		if (conch != null) conch.SetActive(false);
			}
		}}}
	
