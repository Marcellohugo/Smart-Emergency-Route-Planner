using System;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    private void UpdateTrafficConditions()
    {
        if (ActiveGraph == null) return;

        if (selectedTrafficLevel == "Normal")
        {
            ActiveGraph.ResetTraffic();
        }
        else if (selectedTrafficLevel == "Severe")
        {
            foreach (var edge in ActiveGraph.AllEdges)
            {
                edge.Traffic = TrafficLevel.Severe;
                edge.TrafficMultiplier = 2.5;
            }
        }
        else if (selectedTrafficLevel == "Random")
        {
            ActiveGraph.ApplyRandomTraffic(RandomSeed);
        }

        UpdateTimeFactor();
    }

    private void UpdateTimeFactor()
    {
        if (ActiveGraph == null) return;

        foreach (var edge in ActiveGraph.AllEdges)
        {
            edge.TimePeriodMultiplier = TimePeriod;
        }

        SolveCurrentRoute();
        RunComparison();
    }

    private void CloseRandomRoads()
    {
        if (ActiveGraph == null) return;

        var rand = new Random();
        int seed = rand.Next(1, 999999);
        ActiveGraph.CloseRandomEdges(0.10, seed);
        SolveCurrentRoute();
        RunComparison();
        LogWarning("[SISTEM] 10% ruas jalan ditutup secara acak.");
    }

    private void ResetRoadClosures()
    {
        if (ActiveGraph == null) return;

        ActiveGraph.ResetClosures();
        SolveCurrentRoute();
        RunComparison();
        LogSuccess("[SISTEM] Semua blokade jalan dibuka kembali.");
    }
}
