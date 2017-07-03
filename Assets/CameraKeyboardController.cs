using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class handles the camera changes caused by the keyboard.
/// </summary>
public class CameraKeyboardController : MonoBehaviour
{
    /// <summary>
    /// Camera move speed.
    /// </summary>
	private float moveSpeed = 3.5f;
	
    /// <summary>
    /// Update function, called every frame.
    /// </summary>
	public void Update() 
	{
		Vector3 translate = new Vector3(
			Input.GetAxis("Horizontal"),
			0,
			Input.GetAxis("Vertical")
		);

		this.transform.Translate(translate * moveSpeed * Time.deltaTime * (1 + this.transform.position.y / 2), Space.World);
	}
}
