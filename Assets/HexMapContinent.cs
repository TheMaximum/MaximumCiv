using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Map generator for continents map.
/// </summary>
public class HexMapContinent : HexMap
{
    /// <summary>
    /// Generate map with continents.
    /// </summary>
	override public void GenerateMap()
	{
		// First: call base version to get all hexes we need.
		base.GenerateMap();

		int numContinents = 3;
		int continentSpacing = NumColumns / numContinents;

		Random.InitState(0);
		for(int c = 0; c < numContinents; c++)
		{
			// Make some kind of raised area
			int numSplats = Random.Range(4, 8);
			for(int i = 0; i < numSplats; i++)
			{
				int range = Random.Range(5, 8);
				int y = Random.Range(range, NumRows - range);
				int x = Random.Range(0, 10) - (y / 2) + (c * continentSpacing);

				elevateArea(x, y, range);
			}
		}

		// Add lumpiness Perlin Noise?
		float noiseResolution = 0.01f;
		Vector2 noiseOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)); 
		float noiseScale = 1.8f;  // Larger values makes more islands (and lakes, I guess)

		for(int column = 0; column < NumColumns; column++)
		{
			for(int row = 0; row < NumRows; row++)
			{
				Hex hex = GetHexAt(column, row);
				float noise = Mathf.PerlinNoise(
					((float)column / Mathf.Max(NumColumns, NumRows) / noiseResolution) + noiseOffset.x, 
					((float)row / Mathf.Max(NumColumns, NumRows) / noiseResolution) + noiseOffset.y 
				) - 0.5f;
				hex.Elevation += noise * noiseScale;
			}
		}

		// Set mesh to mountain/hill/flat/water based on height
		// Simulate rainfall/moisture and set plains/grasslands + forest
		noiseResolution = 0.02f;
		noiseOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)); 
		noiseScale = 2f;  // Larger values makes more islands (and lakes, I guess)

		for(int column = 0; column < NumColumns; column++)
		{
			for(int row = 0; row < NumRows; row++)
			{
				Hex hex = GetHexAt(column, row);
				float noise = Mathf.PerlinNoise(
					((float)column / Mathf.Max(NumColumns, NumRows) / noiseResolution) + noiseOffset.x, 
					((float)row / Mathf.Max(NumColumns, NumRows) / noiseResolution) + noiseOffset.y 
				) - 0.5f;
				hex.Moisture = noise * noiseScale;
			}
		}

		// Now make sure all hex visuals are updated to match data
		UpdateHexVisuals();

        Unit unit = new Unit();
        SpawnUnitAt(unit, UnitDwarfPrefab, 36, 15);

        Unit newUnit = new Unit(4);
        SpawnUnitAt(newUnit, UnitDwarfPrefab, 37, 15);
    }

    /// <summary>
    /// Elevate area on map around point.
    /// </summary>
    /// <param name="q">Location column</param>
    /// <param name="r">Location row</param>
    /// <param name="range">Range around point</param>
    /// <param name="centerHeight">Height of center point</param>
	private void elevateArea(int q, int r, int range, float centerHeight = 0.8f)
	{
		Hex centerHex = GetHexAt(q, r);
		Hex[] area = GetHexInRange(centerHex, range);

		foreach(Hex hex in area)
		{
			hex.Elevation = centerHeight * Mathf.Lerp(1f, 0.25f, Mathf.Pow(Hex.Distance(centerHex, hex) / range, 2f));
		}
	}
}
