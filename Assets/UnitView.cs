using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class handles unit changes/updates.
/// </summary>
public class UnitView : MonoBehaviour
{
    /// <summary>
    /// New position for unit.
    /// </summary>
    private Vector3 newPosition;

    /// <summary>
    /// Current velocity of unit movement.
    /// </summary>
    private Vector3 currentVelocity;

    /// <summary>
    /// Smoothing time for unit movement.
    /// </summary>
    private float smoothTime = 0.5f;

    /// <summary>
    /// Initialize positions.
    /// </summary>
    public void Start()
    {
        newPosition = this.transform.position;
    }

    /// <summary>
    /// Execute animation on every frame.
    /// </summary>
    public void Update()
    {
        this.transform.position = Vector3.SmoothDamp(this.transform.position, newPosition, ref currentVelocity, smoothTime);
    }

    /// <summary>
    /// Animates the movement of a unit from a hex to another hex.
    /// </summary>
    /// <param name="oldHex">Old tile location</param>
    /// <param name="newHex">New tile location</param>
    public void OnUnitMoved(Hex oldHex, Hex newHex)
    {
        this.transform.position = oldHex.PositionFromCamera();
        newPosition = newHex.PositionFromCamera();
        currentVelocity = Vector3.zero;

        if(Vector3.Distance(this.transform.position, newPosition) > 2)
        {
            this.transform.position = newPosition;
        }
    }
}
