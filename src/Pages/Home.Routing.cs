using System;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    private void OnSolverChanged()
    {
        StopAnimation();
        SolveCurrentRoute();
    }

    private void SolveCurrentRoute()
    {
        if (ActiveGraph == null) return;

        StopAnimation();

        try
        {
            var route = RoutePlanner.SolveRoute(ActiveGraph, SourceNodeId, TargetNodeId, HospitalNodeIds, ActiveSolver, YenK, EmergencyMode, RobustLambda);
            ActivePathResult = route.ActivePathResult;
            AlternativePaths = route.AlternativePaths;

            if (ActivePathResult != null)
            {
                if (ActivePathResult.IsReachable)
                {
                    LogSystem($"[ALGORITMA] {ActivePathResult.AlgorithmName} selesai. Waktu tempuh: {ActivePathResult.TotalTravelTimeMinutes:F2} menit, node dikunjungi: {ActivePathResult.ExpandedNodes}.");
                }
                else
                {
                    LogWarning($"[ALGORITMA] {ActiveSolver} selesai, namun target tidak dapat dijangkau.");
                }
            }

            VerifyRouteWithBellmanFord();
            UpdateHospitalsList();
        }
        catch (Exception ex)
        {
            LogError("[ALGORITMA ERROR] " + ex.Message);
        }
    }

    private void RunComparison()
    {
        if (ActiveGraph == null) return;

        try
        {
            ComparisonResults = RoutePlanner.CompareSolvers(ActiveGraph, SourceNodeId, TargetNodeId, EmergencyMode, RobustLambda);
        }
        catch (Exception ex)
        {
            LogError("[PERBANDINGAN ERROR] " + ex.Message);
        }
    }

    private void VerifyRouteWithBellmanFord()
    {
        if (ActiveGraph == null || ActivePathResult == null)
        {
            ValidationStatus = "none";
            return;
        }

        try
        {
            var validation = RoutePlanner.ValidateWithBellmanFord(ActiveGraph, ActivePathResult, SourceNodeId, TargetNodeId, EmergencyMode);
            ValidationStatus = validation.Status;
            if (!string.IsNullOrWhiteSpace(validation.WarningMessage))
            {
                LogWarning(validation.WarningMessage);
            }
        }
        catch
        {
            ValidationStatus = "none";
        }
    }

    private void UpdateHospitalsList()
    {
        if (ActiveGraph == null) return;

        HospitalDistances = RoutePlanner.GetHospitalDistances(ActiveGraph, SourceNodeId, HospitalNodeIds, EmergencyMode);
    }
}
