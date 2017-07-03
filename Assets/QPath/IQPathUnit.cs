using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPath
{
    /// <summary>
    /// Interface for pathable units.
    /// </summary>
    public interface IQPathUnit
    {
        /// <summary>
        /// Get turn cost to enter a tile (f.e. 0.5 turns if movement cost is 1 and have maximum 2 movement).
        /// </summary>
        /// <param name="sourceTile">Source tile</param>
        /// <param name="destinationTile">Destination tile</param>
        /// <returns>Turn cost to enter tile</returns>
        float CostToEnterTile(IQPathTile sourceTile, IQPathTile destinationTile);
    }
}
