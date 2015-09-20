using UnityEngine;
using System.Collections;

public class CountdownExplode : MonoBehaviour {
	public Transform piece1;
	public float timer=1f;
	public bool countdown=false;
	private bool playerAttatched;
	// Use this for initialization
	void Start () {
		playerAttatched = true;
	/*	parts = new Transform[6]{piece1,piece2,piece3,piece4,piece5,piece6};*/
	}
	
	// Update is called once per frame
	void Update () {
		if (countdown) {
						timer -= Time.deltaTime;
						print (timer);
						if (timer < 0) {
								Explode ();		
						}
				}
	}
	void Explode(){
		GameObject p = GameObject.Find ("Player 1");
		if (p) {
						Player player = p.GetComponent <Player> ();
						if (playerAttatched) {
								player.detatch ();
						}
				}
		print ("exploding");
		Destroy (gameObject);

		var t = this.transform;

		for (int i = 0; i < 8; i++) {
			print ("yo");
			Transform clone = Instantiate (piece1, t.position, Quaternion.identity)as Transform;
			clone.GetComponent<Rigidbody2D>().AddForce (Vector3.right * (Random.Range (-15, 15)));
						clone.GetComponent<Rigidbody2D>().AddForce (Vector3.up * Random.Range (-15, 15));
			clone.rotation = Random.rotation;

				}
	}public void playerLeft(){
		playerAttatched = false;
	}
	public void count(){
		countdown = true;
	}
}
