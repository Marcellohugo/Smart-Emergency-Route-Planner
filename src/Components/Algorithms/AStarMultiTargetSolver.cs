using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class AStarMultiTargetSolver
    {
        /// <summary>
        /// Solves the multi-hospital routing query by executing A* search to each target hospital,
        /// then identifying the target hospital with the minimum travel time.
        /// Runtime and expansions are aggregated to show the total cost of multiple runs.
        /// </summary>
        public PathResult Solve(Graph graph, int source, List<int> targetHospitals, double maxSpeedKmh = 100.0, bool emergencyMode = false)
        {
            var stopwatch = Stopwatch.StartNew();

            var astar = new AStarSolver();
            PathResult? bestResult = null;
            int bestHospital = -1;

            int totalExpandedNodes = 0;
            long totalRelaxationCount = 0;

            foreach (int hospital in targetHospitals)
            {
                var res = astar.Solve(graph, source, hospital, maxSpeedKmh, emergencyMode);

                totalExpandedNodes += res.ExpandedNodes;
                totalRelaxationCount += res.RelaxationCount;

                if (res.IsReachable)
                {
                    if (bestResult == null || res.TotalTravelTimeMinutes < bestResult.TotalTravelTimeMinutes)
                    {
                        bestResult = res;
                        bestHospital = hospital;
                    }
                }
            }

            stopwatch.Stop();

            bool isReachable = bestResult != null;

            var finalResult = new PathResult
            {
                AlgorithmName = "A* Multi-Target",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = totalExpandedNodes,
                RelaxationCount = totalRelaxationCount,
                HasNegativeCycle = false,
                Notes = isReachable ? $"Closest Hospital: Vertex {bestHospital}" : "No hospital is reachable."
            };

            if (isReachable && bestResult != null)
            {
                finalResult.Path = bestResult.Path;
                finalResult.TotalTravelTimeMinutes = bestResult.TotalTravelTimeMinutes;
            }
            else
            {
                finalResult.TotalTravelTimeMinutes = -1;
                finalResult.Path = new List<int>();
            }

            return finalResult;
        }
    }
}
