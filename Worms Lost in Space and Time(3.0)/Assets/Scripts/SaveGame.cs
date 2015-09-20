using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class SaveGame : MonoBehaviour
{
	private bool starSuccess=false;
	public float starTime=0f;
	public int currentLevel;
	private float time;
	void Start () {
			
		}
	
	void OnTriggerEnter2D( Collider2D other)
	{
		if(other.gameObject.tag == "Player")
		{
			time = Mathf.Round (Time.timeSinceLevelLoad * 100f) / 100f;

			Debug.Log ("Time is "+time);
			if (time < starTime) {
				starSuccess=true;
			}
			GameController cont= new GameController();
			int levelcount=currentLevel;



			cont.Save (levelcount, starSuccess);

			PlayerPrefs.SetFloat("FinishedTime", time);

			/*GameController.control.levelcount += 1;
			GameController.control.Save();*/
		}
	}
}