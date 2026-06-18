using System;
using System.Collections.Generic;
using System.Linq;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Services;

public sealed class RoutePlannerService
{
    public RouteSolveResult SolveRoute(
        Graph graph,
        int sourceId,
        int targetId,
        IReadOnlyList<int> hospitalIds,
        string solver,
        int yenK,
        bool emergencyMode,
        double robustLambda)
    {
        var alternativePaths = new List<PathResult>();
        PathResult activeResult;

        if (solver == "Dijkstra")
        {
            activeResult = new DijkstraSolver().Solve(graph, sourceId, targetId, emergencyMode);
        }
        else if (solver == "AStar")
        {
            activeResult = new AStarSolver().Solve(graph, sourceId, targetId, 100.0, emergencyMode);
        }
        else if (solver == "BidirectionalDijkstra")
        {
            activeResult = new BidirectionalDijkstraSolver().Solve(graph, sourceId, targetId, emergencyMode);
        }
        else if (solver == "Robust")
        {
            activeResult = new RobustRouteSolver().Solve(graph, sourceId, targetId, emergencyMode, robustLambda);
        }
        else if (solver == "Alternative")
        {
            alternativePaths = new AlternativeRouteSolver().FindAlternativeRoutes(graph, sourceId, targetId, yenK, emergencyMode);
            activeResult = alternativePaths.FirstOrDefault() ?? new PathResult { IsReachable = false, AlgorithmName = "Alternative" };
        }
        else if (solver == "Yen")
        {
            alternativePaths = new YenKShortestPathsSolver().FindKShortestPaths(graph, sourceId, targetId, yenK, emergencyMode);
            activeResult = alternativePaths.FirstOrDefault() ?? new PathResult { IsReachable = false, AlgorithmName = "Yen" };
        }
        else if (solver == "MultiHospital")
        {
            activeResult = new DijkstraMultiTargetSolver().Solve(graph, sourceId, hospitalIds.ToList());
        }
        else
        {
            throw new ArgumentException($"Unknown route solver '{solver}'.", nameof(solver));
        }

        return new RouteSolveResult(activeResult, alternativePaths);
    }

    public List<PathResult> CompareSolvers(
        Graph graph,
        int sourceId,
        int targetId,
        bool emergencyMode,
        double robustLambda)
    {
        return new List<PathResult>
        {
            new DijkstraSolver().Solve(graph, sourceId, targetId, emergencyMode),
            new AStarSolver().Solve(graph, sourceId, targetId, 100.0, emergencyMode),
            new BidirectionalDijkstraSolver().Solve(graph, sourceId, targetId, emergencyMode),
            new RobustRouteSolver().Solve(graph, sourceId, targetId, emergencyMode, robustLambda)
        };
    }

    public RouteValidationResult ValidateWithBellmanFord(
        Graph graph,
        PathResult activeResult,
        int sourceId,
        int targetId,
        bool emergencyMode)
    {
        var bellmanFordResult = new BellmanFordSolver().Solve(graph, sourceId, targetId, emergencyMode);

        if (bellmanFordResult.HasNegativeCycle)
        {
            return new RouteValidationResult("negative_cycle", "[VALIDATOR] Peringatan: Siklus berat negatif terdeteksi oleh Bellman-Ford!");
        }

        if (!bellmanFordResult.IsReachable)
        {
            return new RouteValidationResult(activeResult.IsReachable ? "invalid" : "valid");
        }

        if (!activeResult.IsReachable)
        {
            return new RouteValidationResult("invalid");
        }

        double diff = Math.Abs(bellmanFordResult.TotalTravelTimeMinutes - activeResult.TotalTravelTimeMinutes);
        if (diff < 1e-4)
        {
            return new RouteValidationResult("valid");
        }

        return new RouteValidationResult(
            "invalid",
            $"[VALIDATOR] Ketidakcocokan: Waktu optimal Bellman-Ford ({bellmanFordResult.TotalTravelTimeMinutes:F4}m) berbeda dengan {activeResult.AlgorithmName} ({activeResult.TotalTravelTimeMinutes:F4}m).");
    }

    public List<HospitalItem> GetHospitalDistances(
        Graph graph,
        int sourceId,
        IReadOnlyList<int> hospitalIds,
        bool emergencyMode)
    {
        var dijkstra = new DijkstraSolver();
        var items = new List<HospitalItem>();

        foreach (int hospitalId in hospitalIds)
        {
            var result = dijkstra.Solve(graph, sourceId, hospitalId, emergencyMode);
            var node = graph.GetVertex(hospitalId);
            items.Add(new HospitalItem
            {
                NodeId = hospitalId,
                Name = node.Name,
                IsReachable = result.IsReachable,
                TravelTimeMinutes = result.TotalTravelTimeMinutes
            });
        }

        return items.OrderBy(x => x.IsReachable ? x.TravelTimeMinutes : double.PositiveInfinity).ToList();
    }
}

public sealed record RouteSolveResult(PathResult ActivePathResult, List<PathResult> AlternativePaths);

public sealed record RouteValidationResult(string Status, string? WarningMessage = null);
