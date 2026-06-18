using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Services;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    // State variables
    private Graph? ActiveGraph { get; set; }
    private GraphFamily GraphLayout { get; set; } = GraphFamily.GridCity;
    private int NodeCount { get; set; } = 80;
    private int EdgeCount { get; set; } = 180;
    private int RandomSeed { get; set; } = 42;

    private string ActiveSolver { get; set; } = "Dijkstra";
    private int YenK { get; set; } = 3;
    private double RobustLambda { get; set; } = 15.0;

    private bool EmergencyMode { get; set; } = true;
    private string selectedTrafficLevel { get; set; } = "Normal";
    private double TimePeriod { get; set; } = 1.0;
    private ElementReference svgRef;

    // Zoom and pan variables
    private double ZoomScale { get; set; } = 1.0;
    private double PanX { get; set; } = 0.0;
    private double PanY { get; set; } = 0.0;
    private bool isPanning = false;
    private double lastMouseX = 0;
    private double lastMouseY = 0;

    private string SvgViewBox => $"{(int)PanX} {(int)PanY} {(int)(1000.0 / ZoomScale)} {(int)(1000.0 / ZoomScale)}";

    // Source and destination
    private int SourceNodeId { get; set; } = 0;
    private int TargetNodeId { get; set; } = 79;
    private List<int> HospitalNodeIds { get; set; } = new List<int>();

    // Selection/Hover values
    private int? hoveredVertexId = null;
    private Edge? hoveredEdge = null;
    private double tooltipX = 0;
    private double tooltipY = 0;

    // Active Result Values
    private PathResult? ActivePathResult { get; set; }
    private List<PathResult> AlternativePaths { get; set; } = new List<PathResult>();
    private List<PathResult> ComparisonResults { get; set; } = new List<PathResult>();
    private List<HospitalItem> HospitalDistances { get; set; } = new List<HospitalItem>();

    // Tab state
    private string activeTab = "compare";

    // Console logs
    private List<LogEntry> ConsoleLogs { get; set; } = new List<LogEntry>();

    // Validation Status
    private string ValidationStatus { get; set; } = "none";

    // Animation status
    private bool isAnimating = false;
    private int AnimationSpeed { get; set; } = 50;
    private List<int> animatedVisitedNodes = new List<int>();
    private List<int> animatedPathNodes = new List<int>();
    private List<System.ValueTuple<int, int>> animatedPathEdges = new List<System.ValueTuple<int, int>>();

    private int? activeDraggedVertexId = null;

    protected override void OnInitialized()
    {
        GenerateNewGraph();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Recreate icons if Lucide is available in browser context
        await JSRuntime.InvokeVoidAsync("window.recreateLucideIcons");
    }

    private void GenerateNewGraph()
    {
        try
        {
            if (NodeCount < 5) NodeCount = 5;
            if (EdgeCount < NodeCount - 1) EdgeCount = NodeCount - 1;

            ActiveGraph = CityGraphGenerator.Generate(NodeCount, EdgeCount, RandomSeed, GraphLayout);

            // Default dispatch center and default main hospital node
            SourceNodeId = 0;
            TargetNodeId = NodeCount - 1;

            HospitalNodeIds.Clear();
            HospitalNodeIds.Add(TargetNodeId);

            // Add other mock hospitals based on node count
            if (NodeCount >= 20)
            {
                HospitalNodeIds.Add(NodeCount / 4);
            }
            if (NodeCount >= 50)
            {
                HospitalNodeIds.Add(NodeCount / 2);
            }

            // Apply selected traffic model
            UpdateTrafficConditions();

            // Resolve routes
            SolveCurrentRoute();
            RunComparison();

            LogSuccess($"[SISTEM] Peta kota baru dibuat dengan layout {(GraphLayout == GraphFamily.GridCity ? "Kota Grid" : "Peta Acak")}, {NodeCount} persimpangan dan {EdgeCount} ruas jalan.");
        }
        catch (Exception ex)
        {
            LogError("[ERROR] Gagal memuat peta: " + ex.Message);
        }
    }

    private void RollRandomSeed()
    {
        var rand = new Random();
        RandomSeed = rand.Next(1, 999999);
        GenerateNewGraph();
    }

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
            Console.WriteLine("Error running comparison: " + ex.Message);
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
                edge.Traffic = SmartEmergencyRoutePlanner.Models.TrafficLevel.Severe;
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

    // SVG node and edge interaction
    private async Task OnSvgMouseMove(MouseEventArgs e)
    {
        if (isPanning)
        {
            double dx = e.ClientX - lastMouseX;
            double dy = e.ClientY - lastMouseY;
            lastMouseX = e.ClientX;
            lastMouseY = e.ClientY;

            PanX -= dx / ZoomScale;
            PanY -= dy / ZoomScale;

            // Limit boundary to prevent getting completely lost
            PanX = Math.Max(-1000.0, Math.Min(1500.0, PanX));
            PanY = Math.Max(-1000.0, Math.Min(1500.0, PanY));
            StateHasChanged();
        }
        else if (activeDraggedVertexId.HasValue && ActiveGraph != null)
        {
            var coords = await JSRuntime.InvokeAsync<SvgCoords>("window.getSvgMouseCoords", svgRef, e.ClientX, e.ClientY);

            double svgX = Math.Max(0.0, Math.Min(1000.0, coords.X));
            double svgY = Math.Max(0.0, Math.Min(1000.0, coords.Y));

            var vertex = ActiveGraph.GetVertex(activeDraggedVertexId.Value);
            vertex.X = svgX / 10.0;
            vertex.Y = svgY / 10.0;

            // Update weights dynamically
            foreach (var edge in ActiveGraph.AllEdges)
            {
                if (edge.From == vertex.Id || edge.To == vertex.Id)
                {
                    double dist = Geometry.CalculateEuclideanDistance(ActiveGraph.GetVertex(edge.From), ActiveGraph.GetVertex(edge.To));
                    edge.DistanceKm = dist;
                    edge.TravelTimeMinutes = (dist / edge.SpeedKmh) * 60.0;
                }
            }

            SolveCurrentRoute();
        }
    }

    private void OnNodeMouseDown(MouseEventArgs e, int vertexId)
    {
        // 0: Left Click (Drag or Set Source if ctrl clicked, or toggle source on direct click)
        if (e.Button == 0)
        {
            if (e.CtrlKey)
            {
                SetSourceNode(vertexId);
            }
            else
            {
                activeDraggedVertexId = vertexId;
            }
        }
    }

    private void OnSvgMouseDown(MouseEventArgs e)
    {
        // Start panning if it is a left-click and we aren't dragging a node
        if (e.Button == 0 && !activeDraggedVertexId.HasValue)
        {
            isPanning = true;
            lastMouseX = e.ClientX;
            lastMouseY = e.ClientY;
        }
    }

    private void OnSvgMouseUp()
    {
        if (isPanning)
        {
            isPanning = false;
            StateHasChanged();
        }
        if (activeDraggedVertexId.HasValue)
        {
            activeDraggedVertexId = null;
            RunComparison();
            LogSystem("[SISTEM] Koordinat persimpangan diperbarui.");
        }
    }

    private void ZoomIn()
    {
        double oldScale = ZoomScale;
        ZoomScale = Math.Min(5.0, ZoomScale + 0.1);

        double zoomFactor = ZoomScale / oldScale;
        PanX = 500.0 - (500.0 - PanX) / zoomFactor;
        PanY = 500.0 - (500.0 - PanY) / zoomFactor;
    }

    private void ZoomOut()
    {
        double oldScale = ZoomScale;
        ZoomScale = Math.Max(0.5, ZoomScale - 0.1);

        double zoomFactor = ZoomScale / oldScale;
        PanX = 500.0 - (500.0 - PanX) / zoomFactor;
        PanY = 500.0 - (500.0 - PanY) / zoomFactor;
    }

    private void ResetZoom()
    {
        ZoomScale = 1.0;
        PanX = 0.0;
        PanY = 0.0;
        isPanning = false;
        LogSystem("[SISTEM] Tampilan peta direset.");
    }

    private void SetSvgReference(ElementReference reference)
    {
        svgRef = reference;
    }

    private async Task OnSvgWheel(WheelEventArgs e)
    {
        double zoomFactor = e.DeltaY < 0 ? 1.15 : 1.0 / 1.15;
        double newScale = Math.Max(0.5, Math.Min(5.0, ZoomScale * zoomFactor));

        if (newScale != ZoomScale)
        {
            var coords = await JSRuntime.InvokeAsync<SvgCoords>("window.getSvgMouseCoords", svgRef, e.ClientX, e.ClientY);
            double mouseX = coords.X;
            double mouseY = coords.Y;

            PanX = mouseX - (mouseX - PanX) / (newScale / ZoomScale);
            PanY = mouseY - (mouseY - PanY) / (newScale / ZoomScale);

            ZoomScale = newScale;
            StateHasChanged();
        }
    }

    private void SetSourceNode(int id)
    {
        if (id == TargetNodeId)
        {
            LogWarning("[SISTEM] Titik asal tidak boleh sama dengan tujuan.");
            return;
        }
        SourceNodeId = id;
        SolveCurrentRoute();
        RunComparison();
        LogSystem($"[SISTEM] Titik Asal dipindah ke persimpangan {id}.");
    }

    private void SetTargetNode(int id)
    {
        if (id == SourceNodeId)
        {
            LogWarning("[SISTEM] Titik tujuan tidak boleh sama dengan asal.");
            return;
        }
        TargetNodeId = id;
        SolveCurrentRoute();
        RunComparison();
        LogSystem($"[SISTEM] Titik Tujuan dipindah ke persimpangan {id}.");
    }

    private void ToggleHospitalStatus(int id)
    {
        if (HospitalNodeIds.Contains(id))
        {
            if (HospitalNodeIds.Count <= 1)
            {
                LogWarning("[SISTEM] Harus menyisakan minimal 1 Rumah Sakit.");
                return;
            }
            HospitalNodeIds.Remove(id);
            LogSystem($"[SISTEM] Status RS dicabut dari persimpangan {id}.");
        }
        else
        {
            HospitalNodeIds.Add(id);
            LogSuccess($"[SISTEM] Persimpangan {id} ditandai sebagai Rumah Sakit Baru.");
        }
        SolveCurrentRoute();
        RunComparison();
    }

    private void ToggleEdgeClosure(Edge edge)
    {
        edge.IsClosed = !edge.IsClosed;
        SolveCurrentRoute();
        RunComparison();
        LogSystem($"[SISTEM] Ruas jalan {edge.From} - {edge.To} sekarang {(edge.IsClosed ? "DITUTUP" : "DIBUKA")}.");
    }

    // Animations logic
    private async Task StartAnimation()
    {
        if (ActivePathResult == null || ActiveGraph == null) return;

        StopAnimation();

        isAnimating = true;
        animatedVisitedNodes.Clear();
        animatedPathNodes.Clear();
        animatedPathEdges.Clear();

        // Calculate dynamic delay based on speed slider
        int delay = Math.Max(5, 120 - AnimationSpeed);

        // Phase 1: Animate node expansion sequence
        if (ActivePathResult.ExpandedNodesList != null && ActivePathResult.ExpandedNodesList.Any())
        {
            foreach (var nodeId in ActivePathResult.ExpandedNodesList)
            {
                if (!isAnimating) break;
                animatedVisitedNodes.Add(nodeId);
                StateHasChanged();
                await Task.Delay(delay);
            }
        }

        // Phase 2: Animate path drawing from source to target
        if (isAnimating && ActivePathResult.IsReachable && ActivePathResult.Path != null)
        {
            var path = ActivePathResult.Path;
            for (int i = 0; i < path.Count; i++)
            {
                if (!isAnimating) break;
                animatedPathNodes.Add(path[i]);
                if (i > 0)
                {
                    animatedPathEdges.Add((path[i - 1], path[i]));
                }
                StateHasChanged();
                await Task.Delay(delay * 2);
            }
        }

        isAnimating = false;
        StateHasChanged();
    }

    private void StopAnimation()
    {
        isAnimating = false;
        animatedVisitedNodes.Clear();
        animatedPathNodes.Clear();
        animatedPathEdges.Clear();
    }

    // Helper functions for layouts
    private void SetActiveTab(string tab)
    {
        activeTab = tab;
    }

    private bool IsNodeInActivePath(int id)
    {
        if (isAnimating)
        {
            return animatedPathNodes.Contains(id);
        }
        return ActivePathResult?.Path != null && ActivePathResult.Path.Contains(id);
    }

    private bool IsEdgeInPath(int from, int to)
    {
        if (isAnimating)
        {
            return animatedPathEdges.Any(e => e.Item1 == from && e.Item2 == to);
        }

        if (ActiveSolver == "Alternative" || ActiveSolver == "Yen")
        {
            // Just draw the primary route as active path, others will show as secondary alternative paths
            var firstPath = AlternativePaths.FirstOrDefault();
            return firstPath != null && IsEdgeInSinglePath(firstPath.Path, from, to);
        }

        return ActivePathResult?.Path != null && IsEdgeInSinglePath(ActivePathResult.Path, from, to);
    }

    private bool IsEdgeInSinglePath(List<int>? path, int from, int to)
    {
        if (path == null || path.Count < 2) return false;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] == from && path[i + 1] == to)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsEdgeInAlternativePath(List<int>? path, int from, int to)
    {
        return IsEdgeInSinglePath(path, from, to);
    }

    private bool IsEdgeAnimated(int from, int to)
    {
        if (!isAnimating || ActivePathResult?.ExpandedNodesList == null) return false;

        // Edge is animated if we have visited the 'from' node and 'to' node in expanded sequence
        return animatedVisitedNodes.Contains(from) && animatedVisitedNodes.Contains(to);
    }

    private void OnNodeMouseOver(int id)
    {
        hoveredVertexId = id;
    }

    private void OnNodeMouseOut()
    {
        hoveredVertexId = null;
    }

    private void OnEdgeMouseOver(MouseEventArgs e, Edge edge)
    {
        hoveredEdge = edge;
        tooltipX = e.OffsetX + 15;
        tooltipY = e.OffsetY + 15;
    }

    private void OnEdgeMouseOut()
    {
        hoveredEdge = null;
    }

    // Logger operations
    private void ClearConsole()
    {
        ConsoleLogs.Clear();
    }

    private void LogSystem(string message) => Log(message, "system");
    private void LogSuccess(string message) => Log(message, "success");
    private void LogWarning(string message) => Log(message, "warning");
    private void LogError(string message) => Log(message, "danger");

    private void Log(string message, string cssClass)
    {
        ConsoleLogs.Insert(0, new LogEntry { Text = $"[{DateTime.Now:HH:mm:ss}] {message}", Class = cssClass });
        if (ConsoleLogs.Count > 100)
        {
            ConsoleLogs.RemoveAt(ConsoleLogs.Count - 1);
        }
    }

    private void RunCorrectnessTests()
    {
        LogSystem("[SISTEM] Memulai pengetesan keakuratan algoritma (Correctness Test Suite)...");
        var sw = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(sw);
            bool passed = SmartEmergencyRoutePlanner.Tests.AlgorithmCorrectnessTests.RunSuite();

            string testOutput = sw.ToString();
            var lines = testOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("[PASSED]") || line.Contains("SUCCESS"))
                {
                    LogSuccess(line);
                }
                else if (line.Contains("[FAILED]") || line.Contains("FAILED!"))
                {
                    LogError(line);
                }
                else
                {
                    LogSystem(line);
                }
            }
        }
        catch (Exception ex)
        {
            LogError("[ERROR] Gagal menjalankan suite tes: " + ex.Message);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // Output formatting
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
