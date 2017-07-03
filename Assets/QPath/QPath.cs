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
            IQPathWorld world,
            IQPathUnit unit,
            T startTile,
            T destinationTile,
            CostEstimateDelegate costEstimateFunction
        ) where T : IQPathTile
        {
            if(world == null || unit == null || startTile == null || destinationTile == null)
            {
                Debug.LogError("Null value passed to QPath::FindPath.");
                return null;
            }

            QPathAStar<T> resolver = new QPathAStar<T>(world, unit, startTile, destinationTile, costEstimateFunction);
            resolver.DoWork();
            return resolver.GetList();
        }
    }

    public delegate float CostEstimateDelegate(IQPathTile a, IQPathTile b);
}
