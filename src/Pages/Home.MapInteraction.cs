using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Utilities;
using SmartEmergencyRoutePlanner.ViewModels;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
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
        if (e.Button != 0) return;

        if (e.CtrlKey)
        {
            SetSourceNode(vertexId);
        }
        else
        {
            activeDraggedVertexId = vertexId;
        }
    }

    private void OnSvgMouseDown(MouseEventArgs e)
    {
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

        if (newScale == ZoomScale) return;

        var coords = await JSRuntime.InvokeAsync<SvgCoords>("window.getSvgMouseCoords", svgRef, e.ClientX, e.ClientY);
        double mouseX = coords.X;
        double mouseY = coords.Y;

        PanX = mouseX - (mouseX - PanX) / (newScale / ZoomScale);
        PanY = mouseY - (mouseY - PanY) / (newScale / ZoomScale);

        ZoomScale = newScale;
        StateHasChanged();
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
}
