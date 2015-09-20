using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GameController{
	private int maxLevels=30;
	public int levelcount;
	public bool[] tempStarArray;

	 public  GameController () {
		levelcount=Load ();
		tempStarArray = LoadArray ();
	}
	
	public void Save(int level, bool success)
	{
		//Debug.Log ("savingasdf");
		tempStarArray = LoadArray ();
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + "/playerInfo.dat");
		if (tempStarArray [level] == false) {
						tempStarArray [level] = success;
				}
		PlayerData data = new PlayerData();
		Debug.Log ("LevelCount:" + levelcount + ", Level" + level);
		if (level >= levelcount) {
						data.levelcount = (level+1);
				} else {
			data.levelcount=levelcount;
		}
		data.starArray = tempStarArray;
		bf.Serialize(file, data);
		file.Close();
		int i = 0;
		foreach(bool thing in tempStarArray){
			Debug.Log(i+"."+thing+", ");
			i++;
		}
	}
	public int Load()
	{

		if (File.Exists (Application.persistentDataPath + "/playerInfo.dat")) {

						BinaryFormatter bf = new BinaryFormatter ();
						FileStream file = File.Open (Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);
						PlayerData data = (PlayerData)bf.Deserialize (file);
						file.Close ();

						//levelcount = data.levelcount;
			return data.levelcount;
			
				} else {
				return 1;
		}
	}
	public bool[] LoadArray(){
		//Debug.Log("loadingstararray");
		if (File.Exists (Application.persistentDataPath + "/playerInfo.dat")) {
						BinaryFormatter bf = new BinaryFormatter ();
						FileStream file = File.Open (Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);
						PlayerData data = (PlayerData)bf.Deserialize (file);
						file.Close ();
						return data.starArray;
				} else {
			return new bool[maxLevels+1];
		}
	}
}
[Serializable]
class PlayerData
{
	public int levelcount;
	public bool[] starArray;
}