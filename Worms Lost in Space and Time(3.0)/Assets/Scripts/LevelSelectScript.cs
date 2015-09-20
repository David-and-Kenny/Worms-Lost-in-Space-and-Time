using UnityEngine;
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
		Application.LoadLevel(worldLevel); //load the level
	}
	

	
	//function to check for the levels locked
	void  CheckLockedLevels (){
		//loop through the levels of a particular world

		int levelcount = controller.Load ();
		for(int j = 1; j < levelcount; j++){
			GameObject.Find("LockedLevel"+(j+1)).SetActive(false);// = false;
				
			}
		}
	void CheckStars (){

		bool[] starCount = controller.LoadArray();
		for(int i=1; i<starCount.Length; i++){
			if(starCount[i]==false){
		GameObject.Find ("Conch" + (i)).SetActive (false);
			}
		}}}
	
