using UnityEngine;
using System.Collections;

public class Wheel : MonoBehaviour {
	public float speed=50f;
	public float direction = -1f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate (Vector3.forward, Time.deltaTime * direction * speed);
	}

}
