using UnityEngine;
using System.Collections;

public class FinishZone : MonoBehaviour {
	private Transform playerPos;
	public Transform endPos;
	private bool collided;
	public float speed=5;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (collided) {
			playerPos.position=Vector3.MoveTowards(playerPos.position,endPos.position, speed*Time.deltaTime);
			if (playerPos.position == endPos.position) {
				targetReached();
			}
		}
//		if (playerPos.position == endPos.position) {
//			targetReached();
//		}
	}
	void OnTriggerEnter2D(Collider2D target){
		if (target.gameObject.tag == "Player") {
			target.GetComponent<Rigidbody2D>().velocity= target.GetComponent<Rigidbody2D>().velocity*.5f;
			collided=true;
			playerPos=target.transform;
//			target.transform.position=
				
		}
	}
	void targetReached(){
		collided = false;
		playerPos.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
	}

}
