﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QPath
{
    /// <summary>
    /// A* path finder.
    /// </summary>
    public class QPathAStar<T> where T : IQPathTile
    {
        /// <summary>
        /// Unit to be moved.
        /// </summary>
        private IQPathUnit unit;

        /// <summary>
        /// Start position of unit.
        /// </summary>
        private T startTile;

        /// <summary>
        /// Destination position of unit.
        /// </summary>
        private T destinationTile;

        /// <summary>
        /// Cost estimation function.
        /// </summary>
        private CostEstimateDelegate costEstimateFunction;

        /// <summary>
        /// Path for the unit to be taken.
        /// </summary>
        private Queue<T> path;

        /// <summary>
        /// Calculated costs for path.
        /// </summary>
        private Dictionary<T, float> gScore = new Dictionary<T, float>();

        /// <summary>
        /// Instantiate class and set parameters.
        /// </summary>
        /// <param name="world">World map</param>
        /// <param name="unit">Unit to be moved</param>
        /// <param name="startTile">Start position</param>
        /// <param name="destinationTile">Destination position</param>
        /// <param name="costEstimateFunction">Cost estimation function</param>
        public QPathAStar(
            IQPathUnit unit, 
            T startTile, 
            T destinationTile,
            CostEstimateDelegate costEstimateFunction)
        {
            this.unit = unit;
            this.startTile = startTile;
            this.destinationTile = destinationTile;
            this.costEstimateFunction = costEstimateFunction;
        }

        /// <summary>
        /// Execute path finding.
        /// </summary>
        public void DoWork()
        {
            path = new Queue<T>();

            HashSet<T> closedSet = new HashSet<T>();
            PathfindingPriorityQueue<T> openSet = new PathfindingPriorityQueue<T>();
            openSet.Enqueue(startTile, 0.0f);

            Dictionary<T, T> cameFrom = new Dictionary<T, T>();

            /*Dictionary<T, float> */gScore = new Dictionary<T, float>();
            gScore[startTile] = 0.0f;

            Dictionary<T, float> fScore = new Dictionary<T, float>();
            fScore[startTile] = costEstimateFunction(startTile, destinationTile);

            while(openSet.Count > 0)
            {
                T current = openSet.Dequeue();

                // Check to see if we are there.
                if(Object.ReferenceEquals(current, destinationTile))
                {
                    reconstructPath(cameFrom, current);
                    return;
                }

                closedSet.Add(current);

                foreach(T edgeNeighbour in current.GetNeighbours())
                {
                    T neighbour = edgeNeighbour;

                    if(closedSet.Contains(neighbour))
                    {
                        // Ignore this already completed neighbour
                        continue;
                    }

                    float totalPathfindingCostToNeighbour = neighbour.AggregateCostToEnter(gScore[current], current, unit);

                    if(totalPathfindingCostToNeighbour < 0.0f)
                    {
                        // Values less than zero represent an invalid/impassable tile
                        continue;
                    }

                    // Is the neighbour already in the open set?
                    //   If so, and if this new score is worse than the old score,
                    //   discard this new result.
                    if(openSet.Contains(neighbour) && totalPathfindingCostToNeighbour >= gScore[neighbour])
                    {
                        continue;
                    }

                    // This is either a new tile or we just found a cheaper route to it
                    cameFrom[neighbour] = current;
                    gScore[neighbour] = totalPathfindingCostToNeighbour;
                    fScore[neighbour] = gScore[neighbour] + costEstimateFunction(neighbour, destinationTile);

                    openSet.EnqueueOrUpdate(neighbour, gScore[neighbour]);
                }
            }
        }

        /// <summary>
        /// Reconstruct path from old position to current position.
        /// </summary>
        /// <param name="cameFrom">Old position</param>
        /// <param name="current">Current position</param>
        private void reconstructPath(Dictionary<T, T> cameFrom, T current)
        {
            // So at this point, current IS the goal.
            // So what we want to do is walk backwards through the Came_From
            // map, until we reach the "end" of that map...which will be
            // our starting node!
            Queue<T> totalPath = new Queue<T>();
            totalPath.Enqueue(current); // This "final" step is the path is the goal!

            while(cameFrom.ContainsKey(current))
            {
                // CameFrom is a map, where the
                // key => value relation is real saying
                // some_node => we_got_there_from_this_node

                current = cameFrom[current];
                totalPath.Enqueue(current);
            }

            // At this point, total_path is a queue that is running
            // backwards from the END tile to the START tile, so let's reverse it.
            path = new Queue<T>(totalPath.Reverse());
        }

        /// <summary>
        /// Get path to be taken.
        /// </summary>
        /// <returns>List of tiles to go through</returns>
        public ArrayList GetList()
        {
            ArrayList list = new ArrayList() { path.ToArray(), gScore };
            return list;
        }
    }
}