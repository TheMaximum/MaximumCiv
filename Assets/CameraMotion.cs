using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera Motion.
/// </summary>
public class CameraMotion : MonoBehaviour 
{
    /// <summary>
    /// Old camera position.
    /// </summary>
	private Vector3 oldPosition;

    /// <summary>
    /// Initialization of class.
    /// </summary>
	public void Start() 
	{
		oldPosition = this.transform.position;
	}

    /// <summary>
    /// Update function, called every frame.
    /// </summary>
    public void Update() 
	{
		// TODO: Code to click-and-drag camera
		// TODO: WASD
		// TODO: Zoom in and out

		CheckForMovement();
	}

    /// <summary>
    /// Check if there was any movement.
    /// </summary>
	private void CheckForMovement()
	{
		if(oldPosition != this.transform.position)
		{
			oldPosition = this.transform.position;

			// TODO: Dictionary of components in HexMap
			HexComponent[] hexes = GameObject.FindObjectsOfType<HexComponent>();

			// TODO: Find better way to determine which hexes to update

			foreach(HexComponent hex in hexes)
			{
				hex.UpdatePosition();
			}
		}
	}
}
