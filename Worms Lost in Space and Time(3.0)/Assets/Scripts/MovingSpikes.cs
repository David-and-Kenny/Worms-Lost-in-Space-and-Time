using UnityEngine;
using System.Collections;

public class MovingSpikes : MonoBehaviour {
	public float speed = .5f;
	private float i;
	public Transform startPos;
	public Transform endPos;

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {
		/*currentPos = this.transform.position;
	if (currentPos < endPos) {
			rigidbody2D.velocity= new Vector2(transform.localScale.x, transform.localScale.y) * speed;		
		}*/

		i = Mathf.PingPong (Time.timeSinceLevelLoad * speed, 1);
		transform.position= Vector3.Lerp(startPos.position,endPos.position, i);
			
	}
}
