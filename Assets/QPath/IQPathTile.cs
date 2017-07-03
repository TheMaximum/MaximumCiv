using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPath
{
    /// <summary>
    /// Interface for pathable tiles.
    /// </summary>
    public interface IQPathTile
    {
        /// <summary>
        /// Get tiles around the current tile.
        /// </summary>
        /// <returns>Array of neighbouring tiles</returns>
        IQPathTile[] GetNeighbours();

        /// <summary>
        /// Get amount of turns needed to get to this tile.
        /// </summary>
        /// <param name="costSoFar">Movement cost used so far</param>
        /// <param name="sourceTile">Source tile</param>
        /// <param name="unit">Moving unit</param>
        /// <returns>Amount of turns needed to get to this tile</returns>
        float AggregateCostToEnter(float costSoFar, IQPathTile sourceTile, IQPathUnit unit);
    }
}
