using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public Transform cameraTransform;
public Transform wheelTransform;
	public Transform holdSlot;
	public bool playerAttached=false;
	public Wheel wheel;
	public int direction;
	public Transform back;
	private CountdownExplode ce;
	private Transform conveyor;


	void Start(){

		this.gameObject.GetComponent<Rigidbody2D>().velocity= Vector3.down* 3;
	}

	// Update is called once per frame
	void Update () {
		if (playerAttached) {
			if(holdSlot){
			transform.transform.position= holdSlot.transform.position;
			transform.rotation= holdSlot.transform.rotation;
			transform.Rotate (Vector3.forward * -90);
			}
			if(conveyor){transform.rotation=conveyor.rotation;}
		}
		if ((Input.touchCount >=1 || Input.GetKey ("space")) && playerAttached==true) {
			if(ce){ce.playerLeft();}
			playerAttached=false;
			this.gameObject.GetComponent<Rigidbody2D>().velocity= transform.up *10;	
		}

	}

	
	void OnCollisionEnter2D(Collision2D target){
		
				if (wheelTransform == null || wheelTransform != target.transform) {
						if (target.gameObject.tag == "Wheel") {
								conveyor=null;
								wheelTransform = target.transform;
								wheel = wheelTransform.gameObject.GetComponent<Wheel> ();
								transform.localScale = new Vector3 (wheel.direction == -1 ? -1 : 1, 1, 1);
								playerAttached = true;
								holdSlot = target.transform.Find ("Holder");
								holdSlot.transform.position = target.contacts [0].point;
								//holdSlot.transform.Rotate(Vector3.up);
			
								var angle = Mathf.Atan2 (holdSlot.transform.position.y - wheelTransform.transform.position.y, holdSlot.transform.position.x - wheelTransform.transform.position.x);
								holdSlot.rotation = Quaternion.AngleAxis ((180 * angle) / Mathf.PI, Vector3.forward);
								if (target.gameObject.name == "destructable") {
					if(ce){ce.playerLeft();}
					 ce = target.gameObject.GetComponent <CountdownExplode> ();
					ce.count();
										
								}
			}else if(target.gameObject.name=="conveyor"){
				print ("conveyor");
				holdSlot=null;
				wheelTransform=null;
				conveyor=target.transform;
				transform.rotation= target.transform.rotation;
				Conveyor con= target.gameObject.GetComponent<Conveyor>();
				this.gameObject.GetComponent<Rigidbody2D>().velocity= transform.right *con.speed;
				playerAttached = true;
			}
				}
		}
	void OnTriggerEnter2D(Collider2D target){

		if (target.gameObject.name == "CameraChange") {
			Destroy(cameraTransform.GetComponent("CameraFollow"));
			CameraFollow cam= cameraTransform.gameObject.AddComponent<CameraFollow>();
			cam.centerFloat= target.transform.Find("Center");
			cam.followingX= (direction== 0? false: true);
			cam.followingY= (direction== 1? false: true);
			cam.background= back;
			cam.target=this.transform;

			direction= (direction== 0? 1: 0);

		}
	}
	public void detatch(){
		playerAttached = false;
		this.gameObject.GetComponent<Rigidbody2D>().velocity= transform.up *5;
	}

}
