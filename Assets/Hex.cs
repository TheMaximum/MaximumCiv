using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using QPath;

/// <summary>
/// Enumeration containing the possible tile bases.
/// </summary>
public enum TileBase
{
    Ocean,
    Water,
    Flat,
    Hills,
    Mountains
}

/// <summary>
/// Enumeration containing the possible tile vegetations.
/// </summary>
public enum TileVegetation
{
    Marsh,
    None,
    Grassland,
    Desert,
    Forest,
    Jungle
}

/// <summary>
/// Hex class defines the grid position, world-space position, size,
/// neighbours, etc... of a Hex Tile. However, it does NOT interact
/// with Unity directly in any way.
/// </summary>
public class Hex : IQPathTile 
{
    /// <summary>
    /// Column of the tile.
    /// </summary>
	public readonly int Q;

    /// <summary>
    /// Row of the tile.
    /// </summary>
	public readonly int R;

    /// <summary>
    /// Sum/Some value of tile.
    /// </summary>
	public readonly int S;

    /// <summary>
    /// Tile elevation, used in map generation and in-game effects.
    /// </summary>
	public float Elevation;

    /// <summary>
    /// Tile moisture, used in map gneeration and in-game effects.
    /// </summary>
	public float Moisture;

    /// <summary>
    /// Base of the tile.
    /// </summary>
    public TileBase Base;

    /// <summary>
    /// Vegetation on the tile.
    /// </summary>
    public TileVegetation Vegetation;

    /// <summary>
    /// Movement cost.
    /// </summary>
    public int MovementCost = 1;

    /// <summary>
    /// The map this tile is a part of.
    /// </summary>
    public readonly HexMap Map;

    /// <summary>
    /// Tile width multiplier.
    /// </summary>
    private static readonly float WIDTH_MULTIPLIER = (float)(Math.Sqrt(3) / 2);

    // TODO: Property to track hex type (plains, grasslands, etc...)
    // TODO: Property to track hex detail (mine, plantation, etc...)

    /// <summary>
    /// Tile radius size.
    /// </summary>
	private float radius = 1f;

    /// <summary>
    /// List of units on tile.
    /// </summary>
    private HashSet<Unit> units;

    /// <summary>
    /// Tile neighbours.
    /// </summary>
    private Hex[] neighbours;

    /// <summary>
    /// Initialize Hex tile.
    /// </summary>
    /// <param name="map">Map</param>
    /// <param name="column">Tile column</param>
    /// <param name="row">Tile row</param>
	public Hex(HexMap map, int column, int row)
	{
		this.Map = map;

		this.Q = column;
		this.R = row;
		this.S = -(column + row);
	}

    public override string ToString()
    {
        return String.Format("Hex({0},{1})", Q, R);
    }

    /// <summary>
    /// Returns the world-space position of this hex.
    /// </summary>
    public Vector3 Position()
	{
		return new Vector3(
			HexHorizontalSpacing() * (Q + (R / 2f)),
			0,
			HexVerticalSpacing() * R
		);
	}

    /// <summary>
    /// Get tile height.
    /// </summary>
    /// <returns>Tile height</returns>
	public float HexHeight()
	{
		return radius * 2;
	}

    /// <summary>
    /// Get tile width.
    /// </summary>
    /// <returns>Tile with</returns>
	public float HexWidth()
	{
		return WIDTH_MULTIPLIER * HexHeight();
	}

    /// <summary>
    /// Get vertial spacing.
    /// </summary>
    /// <returns>Vertical spacing</returns>
	public float HexVerticalSpacing()
	{
		return HexHeight() * 0.75f;
	}

    /// <summary>
    /// Get horizontal spacing.
    /// </summary>
    /// <returns>Horizontal spacing</returns>
	public float HexHorizontalSpacing()
	{
		return HexWidth();
	}

    /// <summary>
    /// Get position depending on the camera.
    /// </summary>
    /// <returns>Tile position relative to camera</returns>
    public Vector3 PositionFromCamera()
    {
        return Map.GetHexPosition(this);
    }

    /// <summary>
    /// Get position depending on the camera.
    /// </summary>
    /// <param name="cameraPosition">Current camera position</param>
    /// <param name="numRows">Number of rows in map</param>
    /// <param name="numColumns">Number of columns in map</param>
    /// <returns>Tile position relative to camera</returns>
	public Vector3 PositionFromCamera(Vector3 cameraPosition, float numRows, float numColumns)
	{
		float mapHeight = numRows * HexVerticalSpacing();
		float mapWidth = numColumns * HexHorizontalSpacing();

		Vector3 position = Position();

		if(Map.AllowWrapEastWest) 
		{
            float widthsFromCamera = Mathf.Round((position.x - cameraPosition.x) / mapWidth);
            int widthsToFix = (int)widthsFromCamera;
			position.x -= widthsToFix * mapWidth;
		}

		if(Map.AllowWrapNorthSouth) 
		{
            float heightsFromCamera = Mathf.Round((position.z - cameraPosition.z) / mapHeight);
            int heightsToFix = (int)heightsFromCamera;
			position.z -= heightsToFix * mapHeight;
		}

		return position;
	}

    /// <summary>
    /// Calculate distance between two tiles.
    /// </summary>
    /// <param name="a">Tile A</param>
    /// <param name="b">Tile B</param>
    /// <returns>Distance between tiles</returns>
    public static float CostEstimate(IQPathTile a, IQPathTile b)
    {
        return Distance((Hex)a, (Hex)b);
    }

    /// <summary>
    /// Calcuate distance between two tiles.
    /// </summary>
    /// <param name="a">Tile A</param>
    /// <param name="b">Tile B</param>
    /// <returns>Distance between tiles</returns>
	public static float Distance(Hex a, Hex b)
	{
		//FIXME: probably wrong for wrapping

		int dQ = Mathf.Abs(a.Q - b.Q);
		if(a.Map.AllowWrapEastWest)
		{
			if(dQ > a.Map.NumColumns / 2)
				dQ = a.Map.NumColumns - dQ;
		}

		int dR = Mathf.Abs(a.R - b.R);
		if(a.Map.AllowWrapNorthSouth)
		{
			if(dR > a.Map.NumRows / 2)
				dR = a.Map.NumRows - dR;
		}
		
		return Mathf.Max(
			dQ, 
			dR, 
			Mathf.Abs(a.S - b.S)
		);
	}

    /// <summary>
    /// Add unit to unitlist of tile.
    /// </summary>
    /// <param name="unit">Unit</param>
    public void AddUnit(Unit unit)
    {
        if(units == null)
            units = new HashSet<Unit>();

        units.Add(unit);
    }

    /// <summary>
    /// Remove unit from unitlist of tile.
    /// </summary>
    /// <param name="unit">Unit</param>
    public void RemoveUnit(Unit unit)
    {
        if(units != null)
            units.Remove(unit);
    }

    /// <summary>
    /// Get array of units for this tile.
    /// </summary>
    /// <returns>Array of units</returns>
    public Unit[] Units()
    {
        if(units == null)
            return null;

        return units.ToArray();
    }

    /// <summary>
    /// Get movement cost to enter this tile.
    /// </summary>
    /// <returns>Movement cost to enter tile</returns>
    public int BaseMovementCost()
    {
        return MovementCost;
    }

    /// <summary>
    /// Get tiles around the current tile.
    /// </summary>
    /// <returns>Array of neighbouring tiles</returns>
    public IQPathTile[] GetNeighbours()
    {
        if(neighbours != null)
            return neighbours;

        List<Hex> tempNeighbours = new List<Hex>();

        tempNeighbours.Add(Map.GetHexAt(Q + 1, R));
        tempNeighbours.Add(Map.GetHexAt(Q - 1, R));
        tempNeighbours.Add(Map.GetHexAt(Q, R + 1));
        tempNeighbours.Add(Map.GetHexAt(Q, R - 1));
        tempNeighbours.Add(Map.GetHexAt(Q + 1, R - 1));
        tempNeighbours.Add(Map.GetHexAt(Q - 1, R + 1));

        List<Hex> neighbourList = new List<Hex>();
        foreach(Hex neighbour in tempNeighbours)
        {
            if(neighbour != null)
            {
                neighbourList.Add(neighbour);
            }
        }

        neighbours = neighbourList.ToArray();
        return neighbours;
    }

    /// <summary>
    /// Get amount of turns needed to get to this tile.
    /// </summary>
    /// <param name="costSoFar">Movement cost used so far</param>
    /// <param name="sourceTile">Source tile</param>
    /// <param name="unit">Moving unit</param>
    /// <returns>Amount of turns needed to get to this tile</returns>
    public float AggregateCostToEnter(float costSoFar, IQPathTile sourceTile, IQPathUnit unit)
    {
        // TODO: Ignoring source tile, change when there are rivers.
        return ((Unit)unit).AggregateTurnsToEnterHex(this, costSoFar);
    }
}
