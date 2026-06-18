using System.Collections.Generic;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    private string FormatTravelTime()
    {
        if (ActivePathResult == null || !ActivePathResult.IsReachable) return "--";
        return $"{ActivePathResult.TotalTravelTimeMinutes:F2} m";
    }

    private string FormatDistance()
    {
        if (ActivePathResult == null || !ActivePathResult.IsReachable) return "--";
        double dist = CalculatePathDistance(ActivePathResult.Path);
        return $"{dist:F2} km";
    }

    private string FormatExpanded()
    {
        if (ActivePathResult == null) return "--";
        return $"{ActivePathResult.ExpandedNodes}";
    }

    private string FormatRuntime()
    {
        if (ActivePathResult == null) return "--";
        return $"{ActivePathResult.RuntimeMilliseconds:F3} ms";
    }

    private string TranslateTraffic(TrafficLevel level)
    {
        return level switch
        {
            TrafficLevel.Low => "Lancar (Low)",
            TrafficLevel.Normal => "Normal",
            TrafficLevel.High => "Padat (High)",
            TrafficLevel.Severe => "Macet Parah (Severe)",
            _ => "Tidak Diketahui"
        };
    }

    private double CalculatePathDistance(List<int> path)
    {
        if (path == null || path.Count < 2 || ActiveGraph == null) return 0.0;

        double dist = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int u = path[i];
            int v = path[i + 1];
            foreach (var edge in ActiveGraph.GetNeighbors(u))
            {
                if (edge.To == v)
                {
                    dist += edge.DistanceKm;
                    break;
                }
            }
        }

        return dist;
    }
}
