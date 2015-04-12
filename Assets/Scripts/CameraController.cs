using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
	
	public float moveSpeed;
	public float lookSens;
	public float lookSmoothDamp;

	private float horizontal;
	private float vertical;
	private float pitch;
	private float yaw;
	private float currPitch;
	private float currYaw;
	private float pitchV;
	private float yawV;

	// Use this for initialization
	void Start () {
		moveSpeed = 150.0f;
		lookSens = 100.0f;
		lookSmoothDamp = 0.05f;
		pitch = transform.eulerAngles.x;
		yaw = transform.eulerAngles.y;
		currPitch = pitch;
		currYaw = yaw;
	}
	
	void Update () 
	{
		horizontal = Input.GetAxisRaw ("Horizontal");
		vertical = Input.GetAxisRaw ("Vertical");

		if(Input.GetButtonDown("Fire2"))
		{
			Screen.lockCursor = true;
			Screen.showCursor = false;
		}

		if(Input.GetButtonUp("Fire2"))
		{
			Screen.lockCursor = false;
			Screen.showCursor = true;
		}

		if(Screen.lockCursor)
		{
			pitch -= Input.GetAxis("Mouse Y") * lookSens * Time.deltaTime;
			yaw += Input.GetAxis("Mouse X") * lookSens * Time.deltaTime;

			pitch = Mathf.Clamp(pitch, -90, 90);
			
			currPitch = Mathf.SmoothDamp(currPitch, pitch,ref pitchV, lookSmoothDamp);
			currYaw = Mathf.SmoothDamp(currYaw, yaw,ref yawV,lookSmoothDamp);

			transform.rotation = Quaternion.Euler(new Vector3(currPitch, currYaw, 0));

		}
		
		Vector3 translation = new Vector3(horizontal,0,vertical) * moveSpeed * Time.deltaTime;
		transform.Translate(translation);
	}
}
