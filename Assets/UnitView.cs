using System;
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
    /// Amount of time one movement takes ((smoothTime * 1000) + 500).
    /// </summary>
    private float movementTime;

    /// <summary>
    /// Previous milliseconds on tile addition.
    /// </summary>
    private long lastMillis;

    private Queue<Hex> movementQueue;

    /// <summary>
    /// Initialize positions.
    /// </summary>
    public void Start()
    {
        newPosition = this.transform.position;
        lastMillis = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        movementQueue = new Queue<Hex>();
        movementTime = (smoothTime * 1000) + 500;
    }

    /// <summary>
    /// Execute animation on every frame.
    /// </summary>
    public void Update()
    {
        if(Vector3.Distance(this.transform.position, newPosition) > 2)
            newPosition = this.transform.position;

        this.transform.position = Vector3.SmoothDamp(this.transform.position, newPosition, ref currentVelocity, smoothTime);

        if(movementQueue != null && movementQueue.Count > 0)
        {
            long currentMillis = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            if(currentMillis > (lastMillis + movementTime))
            {
                Hex newHex = movementQueue.Dequeue();
                changePosition(this.transform.position, newHex.PositionFromCamera());
                lastMillis = currentMillis;
            }
        }
    }

    /// <summary>
    /// Animates the movement of a unit from a hex to another hex.
    /// </summary>
    /// <param name="oldHex">Old tile location</param>
    /// <param name="newHex">New tile location</param>
    public void OnUnitMoved(Hex oldHex, Hex newHex)
    {
        long currentMillis = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        if(currentMillis > (lastMillis + (smoothTime * 1000)) &&
           movementQueue.Count == 0)
        {
            changePosition(oldHex.PositionFromCamera(), newHex.PositionFromCamera());
            lastMillis = currentMillis;
        }
        else
        {
            movementQueue.Enqueue(newHex);
        }
    }

    /// <summary>
    /// Changes the in-scene position of the unit.
    /// </summary>
    /// <param name="previous">Previous position</param>
    /// <param name="current">New position</param>
    private void changePosition(Vector3 previous, Vector3 current)
    {
        this.transform.position = previous;
        newPosition = current;
        currentVelocity = Vector3.zero;

        if(Vector3.Distance(this.transform.position, current) > 2)
        {
            this.transform.position = current;
        }
    }

    /// <summary>
    /// Is the unit finished moving?
    /// </summary>
    /// <returns>All moves finished?</returns>
    public bool UnitFinishedMoving()
    {
        if(movementQueue.Count > 0)
            return false;

        long currentMillis = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        if(currentMillis > (lastMillis + movementTime))
            return true;

        return false;
    }
}
