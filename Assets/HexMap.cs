using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QPath;

/// <summary>
/// Class handling the tile map.
/// </summary>
public class HexMap : MonoBehaviour, IQPathWorld 
{
    #region Meshes, Materials and GameObjects
    /// <summary>
    /// Mesh for water surface.
    /// </summary>
    public Mesh MeshWater;

    /// <summary>
    /// Mesh for flat surface.
    /// </summary>
	public Mesh MeshFlat;

    /// <summary>
    /// Mesh for hill surface.
    /// </summary>
	public Mesh MeshHill;

    /// <summary>
    /// Mesh for mountain surface.
    /// </summary>
	public Mesh MeshMountain;

    /// <summary>
    /// Material for oceans.
    /// </summary>
	public Material MatOcean;

    /// <summary>
    /// Material for plains.
    /// </summary>
	public Material MatPlains;

    /// <summary>
    /// Material for grasslands.
    /// </summary>
	public Material MatGrasslands;

    /// <summary>
    /// Material for mountains.
    /// </summary>
	public Material MatMountains;

    /// <summary>
    /// Material for desters.
    /// </summary>
	public Material MatDesert;

    /// <summary>
    /// Prefab hex tile.
    /// </summary>
    public GameObject HexPrefab;

    /// <summary>
    /// Prefab forest model.
    /// </summary>
    public GameObject ForestPrefab;

    /// <summary>
    /// Prefab jungle model.
    /// </summary>
	public GameObject JunglePrefab;

    /// <summary>
    /// Prefab unit dwarf model.
    /// </summary>
    public GameObject UnitDwarfPrefab;

    /// <summary>
    /// Prefab unit horse model.
    /// </summary>
    public GameObject UnitHorsePrefab;

    #endregion

    #region Height/Moisture settings

    /// <summary>
    /// Minimum height to spawn mountains on tile.
    /// </summary>
    [System.NonSerialized]
    public float HeightMountain = 1f;

    /// <summary>
    /// Minimum height to spawn hills on tile.
    /// </summary>
    [System.NonSerialized]
    public float HeightHill = 0.6f;

    /// <summary>
    /// Minimum height to spawn flat land on tile.
    /// </summary>
    [System.NonSerialized]
    public float HeightFlat = 0.0f;

    /// <summary>
    /// Minimum moisture to spawn jungle on tile.
    /// </summary>
    [System.NonSerialized]
    public float MoistureJungle = 0.50f;

    /// <summary>
    /// Minimum moisture to spawn forest on tile.
    /// </summary>
    [System.NonSerialized]
    public float MoistureForest = 0.20f;

    /// <summary>
    /// Minimum moisture to spawn grasslands on tile.
    /// </summary>
    [System.NonSerialized]
    public float MoistureGrasslands = 0f;

    /// <summary>
    /// Minimum moisture to spawn plains on tile.
    /// </summary>
    [System.NonSerialized]
    public float MoisturePlains = -0.5f;

    #endregion

    #region Generation settings

    /// <summary>
    /// Number of rows in map.
    /// </summary>
    [System.NonSerialized]
    public readonly int NumRows = 30;

    /// <summary>
    /// Number of columns in map.
    /// </summary>
    [System.NonSerialized]
    public readonly int NumColumns = 60;

    /// <summary>
    /// Allow east to west wrapping of the map?
    /// </summary>
    [System.NonSerialized]
    public bool AllowWrapEastWest = true;

    /// <summary>
    /// Allow north to south wrapping of the map?
    /// </summary>
    [System.NonSerialized]
    public bool AllowWrapNorthSouth = false;

    #endregion

    #region Tiles/Units/GameObjects lists

    /// <summary>
    /// List of hex tiles on the map.
    /// </summary>
    private Hex[,] hexes;

    /// <summary>
    /// Dictionary to retrieve gameobject of hex tile.
    /// </summary>
	private Dictionary<Hex, GameObject> hexToGameObjectMap;

    /// <summary>
    /// Dictionary to retrieve hex tile of game object.
    /// </summary>
	private Dictionary<GameObject, Hex> gameObjectToHexMap;

    /// <summary>
    /// List of units on the map.
    /// </summary>
    public HashSet<Unit> Units;

    /// <summary>
    /// Dictionary to retrieve gameobject of unit.
    /// </summary>
    public Dictionary<Unit, GameObject> UnitToGameObjectMap;

    /// <summary>
    /// Unit currently selected.
    /// </summary>
    public Unit SelectedUnit = null;

    #endregion

    /// <summary>
    /// Initialize: generate map.
    /// </summary>
    public void Start()
    {
        GenerateMap();
    }

    /// <summary>
    /// TESTING: Do Turn.
    /// </summary>
    public void Update()
    {
        // Hit spacebar to go to next turn.
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(Units != null)
            {
                foreach(Unit unit in Units)
                {
                    unit.DoTurn();
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.S))
        {
            if(SelectedUnit != null && SelectedUnit.Type == UnitType.Settler && SelectedUnit.MovementRemaining > 0)
            {
                // Settle a city.
                Hex[] cityTiles = GetHexInRange(SelectedUnit.Hex, 1);
                foreach(Hex cityTile in cityTiles)
                {
                    GameObject tileObject = GetGameObjectFromTile(cityTile);
                    tileObject.transform.GetChild(4).gameObject.SetActive(true);
                    tileObject.transform.GetChild(2).gameObject.SetActive(false);
                }

                DestroyUnit(SelectedUnit);
            }
        }
    }

    /// <summary>
    /// Get hex tile at provided position.
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <returns>Hex tile at provided position (null if not found)</returns>
	public Hex GetHexAt(int x, int y)
	{
		if(hexes == null)
		{
			Debug.LogError("Hexes array is not yet instantiated!");
			return null;
		}

		if(AllowWrapEastWest)
		{
			x = x % NumColumns;
			if(x < 0)
			{
				x += NumColumns;
			}
		}

		if(AllowWrapNorthSouth)
		{
			y = y % NumRows;
			if(y < 0)
			{
				y += NumRows;
			}
		}

		try
		{
            if(x < 0 || y < 0)
                return null;

			return hexes[x, y];
		}
		catch
		{
            Debug.LogError("Can't retrieve hex at (" + x + "," + y + ")");
			return null;
		}
	}

    /// <summary>
    /// Get position of hex tile (with column/row) relative to camera.
    /// </summary>
    /// <param name="q">Tile column</param>
    /// <param name="r">Tile row</param>
    /// <returns>Position of tile</returns>
    public Vector3 GetHexPosition(int q, int r)
    {
        return GetHexPosition(GetHexAt(q, r));
    }

    /// <summary>
    /// Get position of hex tile relative to camera.
    /// </summary>
    /// <param name="hex">Hex tile</param>
    /// <returns>Position of tile</returns>
    public Vector3 GetHexPosition(Hex hex)
    {
        return hex.PositionFromCamera(Camera.main.transform.position, NumRows, NumColumns);
    }

    /// <summary>
    /// Generate ocean filled map.
    /// </summary>
    virtual public void GenerateMap()
	{
		hexes = new Hex[NumColumns, NumRows];
		hexToGameObjectMap = new Dictionary<Hex, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, Hex>();

        for(int column = 0; column < NumColumns; column++) 
		{
			for(int row = 0; row < NumRows; row++) 
			{
				Hex hex = new Hex(this, column, row);
				hex.Elevation = -0.5f;
				hexes[column, row] = hex;

				Vector3 position = hex.PositionFromCamera(
					Camera.main.transform.position, 
					NumRows, 
					NumColumns
				);

				GameObject tile = (GameObject)Instantiate(
					HexPrefab,
					position,
					Quaternion.identity,
					this.transform
				);

				hexToGameObjectMap[hex] = tile;
                gameObjectToHexMap[tile] = hex;

				tile.name = string.Format("Tile {0},{1}", column, row);
				tile.GetComponent<HexComponent>().Hex = hex;
				tile.GetComponent<HexComponent>().HexMap = this;
			}
		}

		UpdateHexVisuals();
	}

    /// <summary>
    /// Update tile visuals according to tile properties.
    /// </summary>
	public void UpdateHexVisuals()
	{
		for(int column = 0; column < NumColumns; column++)
		{
			for(int row = 0; row < NumRows; row++)
			{
				Hex hex = hexes[column, row];
				GameObject hexGameObject = hexToGameObjectMap[hex];

				MeshRenderer mr = hexGameObject.GetComponentInChildren<MeshRenderer>();
				MeshFilter mf = hexGameObject.GetComponentInChildren<MeshFilter>();

                hex.MovementCost = 1;

                if(hex.Elevation >= HeightFlat && hex.Elevation < HeightMountain)
				{
					if(hex.Moisture >= MoistureJungle)
					{
						mr.material = MatGrasslands;
						Vector3 position = hexGameObject.transform.position;
						if(hex.Elevation >= HeightHill)
							position.y += 0.25f;
						
						GameObject.Instantiate(
							JunglePrefab, 
							position, 
							Quaternion.identity, 
							hexGameObject.transform
						);

                        hex.Vegetation = TileVegetation.Jungle;
                        hex.MovementCost = 2;
					} 
					else if(hex.Moisture >= MoistureForest)
					{
						mr.material = MatGrasslands;
						Vector3 position = hexGameObject.transform.position;
						if(hex.Elevation >= HeightHill)
							position.y += 0.25f;

						GameObject.Instantiate(
							ForestPrefab, 
							position, 
							Quaternion.identity, 
							hexGameObject.transform
						);

                        hex.Vegetation = TileVegetation.Forest;
                        hex.MovementCost = 2;
                    } 
					else if(hex.Moisture >= MoistureGrasslands)
					{
                        hex.Vegetation = TileVegetation.Grassland;
                        mr.material = MatGrasslands;
					} 
					else if(hex.Moisture >= MoisturePlains)
					{
                        hex.Vegetation = TileVegetation.None;
                        mr.material = MatPlains;
					}
					else
					{
                        hex.Vegetation = TileVegetation.Desert;
						mr.material = MatDesert;
					}
				}

				if(hex.Elevation >= HeightMountain)
				{
					mr.material = MatMountains;
					mf.mesh = MeshMountain;
                    hex.Base = TileBase.Mountains;
                    hex.MovementCost = -1;
                }
				else if(hex.Elevation >= HeightHill)
				{
					mf.mesh = MeshHill;
                    hex.Base = TileBase.Hills;
                    hex.MovementCost = 2;
                }
				else if(hex.Elevation >= HeightFlat)
				{
                    hex.Base = TileBase.Flat;
                    mf.mesh = MeshFlat;
				}
				else
				{
                    hex.Base = TileBase.Ocean;
                    mr.material = MatOcean;
					mf.mesh = MeshWater;
                    hex.MovementCost = -1;
                }

                TextMesh tileText = hexGameObject.GetComponentInChildren<TextMesh>();
                if(tileText != null)
                    tileText.text = string.Format("{0},{1}\n{2}", column, row, hex.BaseMovementCost());
            }
		}
	}

    /// <summary>
    /// Get array of tiles within range from central point.
    /// </summary>
    /// <param name="centerHex">Center tile</param>
    /// <param name="range">Range around center tile</param>
    /// <returns>Array of tiles within range around center tile</returns>
	public Hex[] GetHexInRange(Hex centerHex, int range)
	{
        if(centerHex == null)
            return null;
		List<Hex> results = new List<Hex>();

		for(int dx = -range; dx <= range; dx++)
		{
			for(int dy = Mathf.Max(-range, (-dx - range)); dy <= Mathf.Min(range, (-dx + range)); dy++)
			{
				results.Add(GetHexAt((centerHex.Q + dx), (centerHex.R + dy)));
			}
		}

		return results.ToArray();
	}

    /// <summary>
    /// Spawns a unit at designated location.
    /// </summary>
    /// <param name="prefab">Unit prefab</param>
    /// <param name="q">Column location</param>
    /// <param name="r">Row location</param>
    public void SpawnUnitAt(Unit unit, GameObject prefab, int q, int r)
    {
        if(Units == null)
        {
            Units = new HashSet<Unit>();
            UnitToGameObjectMap = new Dictionary<Unit, GameObject>();
        }

        Hex hex = GetHexAt(q, r);
        unit.SetHex(hex);

        GameObject hexGameObject = hexToGameObjectMap[hex];
        GameObject unitGameObject = (GameObject)Instantiate(
            prefab,
            hexGameObject.transform.position,
            Quaternion.identity,
            hexGameObject.transform
        );
        unit.OnUnitMoved += unitGameObject.GetComponent<UnitView>().OnUnitMoved;

        Units.Add(unit);
        UnitToGameObjectMap.Add(unit, unitGameObject);
    }

    /// <summary>
    /// Removes the unit from the map and destroys the object.
    /// </summary>
    /// <param name="unit">Unit to be destroyed</param>
    public void DestroyUnit(Unit unit)
    {
        GameObject unitObject = UnitToGameObjectMap[SelectedUnit];
        Units.Remove(SelectedUnit);
        UnitToGameObjectMap.Remove(SelectedUnit);
        Destroy(unitObject);

        if(unit == SelectedUnit)
            SelectedUnit = null;
    }

    /// <summary>
    /// Get game object from tile.
    /// </summary>
    /// <param name="tile">Tile</param>
    /// <returns>Game object linked to tile</returns>
    public GameObject GetGameObjectFromTile(Hex tile)
    {
        if(tile == null)
            return null;

        if(hexToGameObjectMap.ContainsKey(tile))
            return hexToGameObjectMap[tile];

        return null;
    }

    /// <summary>
    /// Get tile from game object.
    /// </summary>
    /// <param name="gameObject">Game object</param>
    /// <returns>Tile linked to game object</returns>
    public Hex GetTileFromGameObject(GameObject gameObject)
    {
        if(gameObjectToHexMap != null && gameObjectToHexMap.ContainsKey(gameObject))
        {
            return gameObjectToHexMap[gameObject];
        }

        return null;
    }
}
