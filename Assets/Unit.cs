using QPath;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class contains properties about units.
/// </summary>
public class Unit : IQPathUnit
{
    /// <summary>
    /// Unit name.
    /// </summary>
    public string Name = "Dwarf";

    /// <summary>
    /// Unit hit points (min = 0, max = 100).
    /// </summary>
    public int HitPoints = 100;

    /// <summary>
    /// Unit strength.
    /// </summary>
    public int Strength = 8;

    /// <summary>
    /// Amount of moves the unit can take in each turn.
    /// </summary>
    public int Movement = 2;

    /// <summary>
    /// Amount of moves left for unit in this turn.
    /// </summary>
    public int MovementRemaining = 2;

    /// <summary>
    /// Current tile on which the unit is placed.
    /// </summary>
    public Hex Hex { get; protected set; }

    /// <summary>
    /// Event for change in position of unit.
    /// </summary>
    public event UnitMove OnUnitMoved;

    /// <summary>
    /// Delegate for OnUnitMoved event.
    /// </summary>
    /// <param name="oldHex">Old tile</param>
    /// <param name="newHex">New tile</param>
    public delegate void UnitMove(Hex oldHex, Hex newHex);

    /// <summary>
    /// Current queued path for this unit.
    /// </summary>
    private Queue<Hex> path;

    private Dictionary<Hex, float> pathCost;

    /// <summary>
    /// Use Civilization 6 movement rules (only enter tile if all needed movement is available).
    /// If not: use Civilization 5 movement rules (enter tile if some movement is still available).
    /// 
    /// TODO: move to configuration
    /// </summary>
    private const bool MOVEMENT_RULES_LIKE_CIV6 = true;

    /// <summary>
    /// Change location of unit.
    /// </summary>
    /// <param name="newHex">New location</param>
    public void SetHex(Hex newHex)
    {
        Hex oldHex = Hex;

        if(Hex != null)
        {
            Hex.RemoveUnit(this);
        }

        Hex = newHex;
        Hex.AddUnit(this);

        if(OnUnitMoved != null)
        {
            OnUnitMoved(oldHex, newHex);
        }
    }

    /// <summary>
    /// Set unit path.
    /// </summary>
    /// <param name="tiles">Array of tiles</param>
    public void SetPath(ArrayList tiles)
    {
        Hex[] pathTiles = (Hex[])tiles[0];
        pathCost = (Dictionary<Hex, float>)tiles[1];
        path = new Queue<Hex>(pathTiles);

        if(path.Count > 0)
            path.Dequeue(); // Dequeue current tile (first in queue).
    }

    /// <summary>
    /// Get current path for unit.
    /// </summary>
    /// <returns>Current path</returns>
    public ArrayList GetPath()
    {
        if(path == null)
            return null;

        List<Hex> pathList = new List<Hex>();
        pathList.Add(Hex);
        foreach(Hex hex in path)
        {
            pathList.Add(hex);
        }

        return new ArrayList() { pathList.ToArray(), pathCost };
    }

    /// <summary>
    /// Clear the current path of the unit.
    /// </summary>
    public void ClearPath()
    {
        SetPath(new ArrayList() { new Hex[0], new Dictionary<Hex, float>() });
    }

    /// <summary>
    /// Execute turn.
    /// </summary>
    public void DoTurn()
    {
        // TODO: Heal up?

        if(path == null || path.Count == 0)
            return;

        // Move to next tile in queue.
        Hex newHex = path.Dequeue();
        SetHex(newHex);
    }

    /// <summary>
    /// Determine the movement cost for entering a specific tile.
    /// </summary>
    /// <param name="tile">Tile to be entered</param>
    /// <returns>Movement cost to enter tile</returns>
    public int MovementCostToEnterHex(Hex tile)
    {
        // TODO: override movement cost depending on unit.
        return tile.BaseMovementCost();
    }

    /// <summary>
    /// Determine the amount of turns needed to enter a specific tile.
    /// </summary>
    /// <param name="tile">Tile to be entered</param>
    /// <param name="turnsToDate">Turns used so far</param>
    /// <returns>Turns to date + turns needed to enter this tile</returns>
    public float AggregateTurnsToEnterHex(Hex tile, float turnsToDate)
    {
        float baseTurnsToEnter = (float)MovementCostToEnterHex(tile) / Movement;

        if(baseTurnsToEnter < 0.0f)
        {
            return -99999.0f;
        }

        // If enter cost is bigger than unit movement, use unit movement as enter cost.
        if(baseTurnsToEnter > 1.0f)
        {
            baseTurnsToEnter = 1.0f;
        }

        float turnsRemaining = (float)MovementRemaining / Movement;

        float turnsToDateWhole = Mathf.Floor(turnsToDate);
        float turnsToDateFraction = turnsToDate - turnsToDateWhole;

        // Resolve floating-point drift if it occurs.
        if((turnsToDateFraction < 0.01f && turnsToDateFraction > 0.0f) || 
           (turnsToDateFraction > 0.99f && turnsToDateFraction < 1.0f))
        {
            Debug.LogError("Looks like we have floating-point drift.");

            if(turnsToDateFraction < 0.01f)
                turnsToDateFraction = 0.0f;

            if(turnsToDateFraction > 0.99f)
            {
                turnsToDateWhole += 1.0f;
                turnsToDateFraction = 0.0f;
            }
        }

        float turnsUsedAfterThisMove = turnsToDateFraction + baseTurnsToEnter;
        if(turnsUsedAfterThisMove > 1.0f)
        {
            // Not enough movement to complete move.

            if(MOVEMENT_RULES_LIKE_CIV6)
            {
                // Not allowed to enter tile.
                if(turnsToDateFraction == 0.0f)
                {
                    // Full movement left (fresh turn), not enough to enter tile - enter anyway.
                }
                else
                {
                    // Not a fresh turn, remain idle for remainder of the turn.
                    turnsToDateWhole += 1.0f;
                    turnsToDateFraction = 0.0f;
                }

                turnsUsedAfterThisMove = baseTurnsToEnter;
            }
            else
            {
                // Civilization 5 style movement, can always enter the tile, even if we don't have enough movement left.
                turnsUsedAfterThisMove = 1.0f;
            }
        }

        return (turnsToDateWhole + turnsUsedAfterThisMove);
    }

    /// <summary>
    /// Get turn cost to enter a tile (f.e. 0.5 turns if movement cost is 1 and have maximum 2 movement).
    /// </summary>
    /// <param name="sourceTile">Source tile</param>
    /// <param name="destinationTile">Destintation tile</param>
    /// <returns>Turn cost to enter a tile</returns>
    public float CostToEnterTile(IQPathTile sourceTile, IQPathTile destinationTile)
    {
        return 1;
    }
}
