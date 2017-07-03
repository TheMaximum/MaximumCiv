using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Hex tile component.
/// </summary>
public class HexComponent : MonoBehaviour 
{
    /// <summary>
    /// The hex tile.
    /// </summary>
	public Hex Hex;

    /// <summary>
    /// The tile map.
    /// </summary>
	public HexMap HexMap;

    /// <summary>
    /// Update position of component.
    /// </summary>
	public void UpdatePosition()
	{
        if(Hex != null)
        {
            this.transform.position = Hex.PositionFromCamera(
                Camera.main.transform.position,
                HexMap.NumRows,
                HexMap.NumColumns
            );
        }
	}
}
