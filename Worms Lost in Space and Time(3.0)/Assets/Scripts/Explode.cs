using UnityEngine;
using System.Collections;

public class Explode : MonoBehaviour {

	public Transform arm1;
	public Transform arm2;
	public Transform head;
	public Transform torso;
	public Transform leg1;
	public Transform leg2;
	public Transform jetpack;
	private Transform[] parts;

	public int totalParts;

	// Use this for initialization
	void Start () {
		 parts = new Transform[7]{arm1,arm2,head,torso,leg1,leg2,jetpack};
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D(Collider2D target){
		if (target.gameObject.tag == "Deadly") {
			OnExplode();
		}

	}

	void OnCollisionEnter2D(Collision2D target){
		if (target.gameObject.tag == "Deadly") {
			OnExplode();
		}
		
	}

	public void OnExplode(){
		Destroy (gameObject);

		var t = transform;

		for (int i = 0; i < 7; i++) {
						Transform clone = Instantiate (parts[i], t.position, Quaternion.identity) as Transform;
						clone.GetComponent<Rigidbody2D>().AddForce (Vector3.right * (Random.Range (-50, 50)));
						clone.GetComponent<Rigidbody2D>().AddForce (Vector3.up * Random.Range (100, 400));

				}
		GameObject go = new GameObject ("ClickToContinue");
		go.AddComponent<DisplayRestartText> ();
		//script.scene = Application.loadedLevelName;
		//go.AddComponent<ClickToContinue> ();
		/*GameObject go = new GameObject ("ClickToContinue");
		ClickToContinue script = go.AddComponent<ClickToContinue> ();
		script.scene = Application.loadedLevelName;
		go.AddComponent<DisplayRestartText> ();*/

	}

}
