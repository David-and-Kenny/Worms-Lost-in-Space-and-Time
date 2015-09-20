using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {
	public Transform target;
	public Transform background;
	public float smoothTime= 0.3f;
	private Transform thisTransform;
	private Vector3 velocity;
	public bool followingX;
	public bool followingY;
	public Transform centerFloat;

	void Start(){
		thisTransform = transform;
		//followingX = true;
		 float aspect = (float)Screen.height / (float)Screen.width;

    if (aspect > 1.7f)
        Camera.main.orthographicSize = 9.9f;
    else if(aspect>=1.5)
        Camera.main.orthographicSize = 9f;
	else if(aspect>=1.3)
			Camera.main.orthographicSize = 7.5f;

		/*
    float vertRatio = Screen.height / 320.0f;
    fontSize = (int)(12 * vertRatio);*/
	}
	// Update is called once per frame
	void Update () 
	{
		if (target)
		{
			if(followingX){
				if(centerFloat){
					float yPos=Mathf.SmoothDamp(thisTransform.position.y, centerFloat.position.y, ref velocity.y, smoothTime);
					float newPosition= Mathf.SmoothDamp(thisTransform.position.x, target.position.x, ref velocity.x, smoothTime);
					thisTransform.position= new Vector3(newPosition, yPos, thisTransform.position.z);
					background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
				}else{
			float newPosition= Mathf.SmoothDamp(thisTransform.position.x, target.position.x, ref velocity.x, smoothTime);
			thisTransform.position= new Vector3(newPosition, thisTransform.position.y, thisTransform.position.z);
			background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
				}}
			if(followingY){

				if(centerFloat){
					float xPos=Mathf.SmoothDamp(thisTransform.position.x, centerFloat.position.x, ref velocity.x, smoothTime);
					float newPosition= Mathf.SmoothDamp(thisTransform.position.y, target.position.y, ref velocity.y, smoothTime);
					thisTransform.position= new Vector3(xPos /*thisTransform.position.x*/, newPosition, thisTransform.position.z);
					background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
				}else{
				float newPosition= Mathf.SmoothDamp(thisTransform.position.y, target.position.y, ref velocity.y, smoothTime);
				thisTransform.position= new Vector3(thisTransform.position.x, newPosition, thisTransform.position.z);
				background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
				}}
			/*if(followingX){
				if(centerFloat){
				
				}
			float newPosition= Mathf.SmoothDamp(thisTransform.position.x, target.position.x, ref velocity.x, smoothTime);
			thisTransform.position= new Vector3(newPosition, thisTransform.position.y, thisTransform.position.z);
			background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
			}
			if(followingY){
				if(centerFloat){
					
				}
				float newPosition= Mathf.SmoothDamp(thisTransform.position.y, target.position.y, ref velocity.y, smoothTime);
				thisTransform.position= new Vector3(thisTransform.position.x, newPosition, thisTransform.position.z);
				background.position= new Vector3(thisTransform.position.x, thisTransform.position.y, background.position.z);
			}*/
		}
		
	}

}