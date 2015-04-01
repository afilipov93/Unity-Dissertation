using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	public int moveSpeed;
	private float moveHorizontal;
	private float moveVertical;

	// Use this for initialization
	void Start () {
		moveSpeed = 100;
		moveVertical = 0.0f;
		moveHorizontal = 0.0f;
	}

	void FixedUpdate () 
	{
		moveHorizontal = Input.GetAxisRaw ("Horizontal");
		moveVertical = Input.GetAxisRaw ("Vertical");
		
		Vector3 translation = new Vector3 (moveHorizontal, 0.0f, moveVertical) * moveSpeed * Time.deltaTime;
		transform.Translate(translation);
	}
}
