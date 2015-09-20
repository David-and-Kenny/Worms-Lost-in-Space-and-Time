using UnityEngine;
using System.Collections;

public class Oxygen : MonoBehaviour {
	public float air = 10;
	public float maxAir = 10;
	public float airBurnRate = 1f;
	public Texture2D bgTexture;
	public Texture2D airBarTexture;
	public int iconWidth = 32;
	public Vector2 airOffset = new Vector2(5, 5);
	
	private Player player;
	
	// Use this for initialization
	void Start () {
		player = GameObject.FindObjectOfType<Player> ();
	}
	
	void OnGUI(){
		var percent = air / maxAir;
		
		if (!player)
			percent = 0;
		
		DrawMeter (airOffset.x, airOffset.y, airBarTexture, bgTexture, percent);
	}
	
	void DrawMeter(float x, float y, Texture2D texture, Texture2D background, float percent){
		var bgW = background.width;
		var bgH = background.height;
		
		GUI.DrawTexture (new Rect (Screen.width-bgH*2f-x, y, bgW/2, bgH/2), background);
		
		var nW = ((bgW/2 - iconWidth) * percent)+iconWidth;
		
		GUI.BeginGroup (new Rect (Screen.width-bgH*2f-x, y, nW, bgH));
		GUI.DrawTexture (new Rect (0, 0, bgW/2, bgH/2), texture);
		GUI.EndGroup ();
		
		
	}
	public void addO2(){
		air = 10;
	}
	
	// Update is called once per frame
	void Update () {
		if (!player)
			return;
		
		if (air > 0) {
			air -= Time.deltaTime * airBurnRate;
			
		} else {
			Explode script = player.GetComponent<Explode>();
			script.OnExplode();
			Destroy (this);
		}
	}
}
