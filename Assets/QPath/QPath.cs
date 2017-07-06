using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPath
{
    /// <summary>
    /// Class handles the pathfinding for the game.
    /// </summary>
    public static class QPath
    {
        /// <summary>
        /// Find path from current tile to destination.
        /// </summary>
        /// <param name="world">World map</param>
        /// <param name="unit">Unit to be moved</param>
        /// <param name="startTile">Current position</param>
        /// <param name="destinationTile">Destintation position</param>
        /// <param name="costEstimateFunction">Cost estimation function</param>
        /// <returns>Path (list of tiles)</returns>
        public static ArrayList FindPath<T>(
            IQPathUnit unit,
            T startTile,
            T destinationTile,
            CostEstimateDelegate costEstimateFunction
        ) where T : IQPathTile
        {
            if(unit == null || startTile == null || destinationTile == null)
            {
                Debug.LogError("Null value passed to QPath::FindPath.");
                return null;
            }

            QPathAStar<T> resolver = new QPathAStar<T>(unit, startTile, destinationTile, costEstimateFunction);
            resolver.DoWork();
            return resolver.GetList();
        }
    }

    /// <summary>
    /// Function to estimate cost of movement from tile A to tile B.
    /// </summary>
    /// <param name="a">Tile A</param>
    /// <param name="b">Tile B</param>
    /// <returns>Estimated cost to move from A to B</returns>
    public delegate float CostEstimateDelegate(IQPathTile a, IQPathTile b);
}
