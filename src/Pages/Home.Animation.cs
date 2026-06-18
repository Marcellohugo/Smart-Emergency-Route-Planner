using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    private async Task StartAnimation()
    {
        if (ActivePathResult == null || ActiveGraph == null) return;

        StopAnimation();

        isAnimating = true;
        animatedVisitedNodes.Clear();
        animatedPathNodes.Clear();
        animatedPathEdges.Clear();

        int delay = System.Math.Max(5, 120 - AnimationSpeed);

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
            return animatedPathEdges.Any(e => e.From == from && e.To == to);
        }

        if (ActiveSolver == "Alternative" || ActiveSolver == "Yen")
        {
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

        return animatedVisitedNodes.Contains(from) && animatedVisitedNodes.Contains(to);
    }
}
