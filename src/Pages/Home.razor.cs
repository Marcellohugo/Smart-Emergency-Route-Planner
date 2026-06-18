using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.ViewModels;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
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

    private double ZoomScale { get; set; } = 1.0;
    private double PanX { get; set; } = 0.0;
    private double PanY { get; set; } = 0.0;
    private bool isPanning;
    private double lastMouseX;
    private double lastMouseY;

    private string SvgViewBox => $"{(int)PanX} {(int)PanY} {(int)(1000.0 / ZoomScale)} {(int)(1000.0 / ZoomScale)}";

    private int SourceNodeId { get; set; }
    private int TargetNodeId { get; set; } = 79;
    private List<int> HospitalNodeIds { get; set; } = new List<int>();

    private int? hoveredVertexId;
    private Edge? hoveredEdge;
    private double tooltipX;
    private double tooltipY;

    private PathResult? ActivePathResult { get; set; }
    private List<PathResult> AlternativePaths { get; set; } = new List<PathResult>();
    private List<PathResult> ComparisonResults { get; set; } = new List<PathResult>();
    private List<HospitalItem> HospitalDistances { get; set; } = new List<HospitalItem>();

    private string activeTab = "compare";
    private List<LogEntry> ConsoleLogs { get; set; } = new List<LogEntry>();
    private string ValidationStatus { get; set; } = "none";

    private bool isAnimating;
    private int AnimationSpeed { get; set; } = 50;
    private List<int> animatedVisitedNodes = new List<int>();
    private List<int> animatedPathNodes = new List<int>();
    private List<(int From, int To)> animatedPathEdges = new List<(int From, int To)>();

    private int? activeDraggedVertexId;

    protected override void OnInitialized()
    {
        GenerateNewGraph();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync("window.recreateLucideIcons");
    }

    private void GenerateNewGraph()
    {
        try
        {
            if (NodeCount < 5) NodeCount = 5;
            if (EdgeCount < NodeCount - 1) EdgeCount = NodeCount - 1;

            ActiveGraph = CityGraphGenerator.Generate(NodeCount, EdgeCount, RandomSeed, GraphLayout);
            SourceNodeId = 0;
            TargetNodeId = NodeCount - 1;

            HospitalNodeIds.Clear();
            HospitalNodeIds.Add(TargetNodeId);

            if (NodeCount >= 20)
            {
                HospitalNodeIds.Add(NodeCount / 4);
            }

            if (NodeCount >= 50)
            {
                HospitalNodeIds.Add(NodeCount / 2);
            }

            UpdateTrafficConditions();
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

    private void SetActiveTab(string tab)
    {
        activeTab = tab;
    }
}
