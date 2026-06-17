using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class GraphVizExporter
    {
        /// <summary>
        /// Exports a graph and an optional path to a DOT file for Graphviz visualization.
        /// </summary>
        public static void ExportToDot(Graph graph, List<int> path, string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();
            sb.AppendLine("digraph SmartEmergencyRoute {");
            sb.AppendLine("    rankdir=LR;");
            sb.AppendLine("    node [style=filled, fontname=\"Arial\"];");
            sb.AppendLine("    edge [fontname=\"Arial\", fontsize=10];");

            int source = path != null && path.Count > 0 ? path[0] : 0;
            int target = path != null && path.Count > 0 ? path[path.Count - 1] : graph.VertexCount - 1;

            var pathNodes = new HashSet<int>(path ?? new List<int>());
            var pathEdges = new HashSet<(int, int)>();
            if (path != null && path.Count > 1)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    pathEdges.Add((path[i], path[i + 1]));
                }
            }

            // 1. Export Nodes
            foreach (var vertex in graph.Vertices)
            {
                string fillColor = "lightblue";
                string shape = "ellipse";
                string label = $"{vertex.Id}\\n({vertex.X:F0},{vertex.Y:F0})";

                if (vertex.Id == source)
                {
                    fillColor = "chartreuse3";
                    shape = "doublecircle";
                    label = $"SOURCE (Hub {vertex.Id})";
                }
                else if (vertex.Id == target)
                {
                    fillColor = "crimson";
                    shape = "doublecircle";
                    label = $"TARGET (Hosp {vertex.Id})";
                }
                else if (pathNodes.Contains(vertex.Id))
                {
                    fillColor = "gold";
                }

                sb.AppendLine($"    {vertex.Id} [fillcolor=\"{fillColor}\", shape={shape}, label=\"{label}\"];");
            }

            // 2. Export Edges
            foreach (var edge in graph.AllEdges)
            {
                string color = "gray60";
                string style = "solid";
                double penwidth = 1.0;
                string label = $"{edge.EffectiveTravelTimeMinutes:F1}m";

                if (edge.IsClosed)
                {
                    color = "red";
                    style = "dashed";
                    label = "CLOSED";
                }
                else if (pathEdges.Contains((edge.From, edge.To)))
                {
                    color = "darkgreen";
                    penwidth = 3.0;
                }

                sb.AppendLine($"    {edge.From} -> {edge.To} [color=\"{color}\", style=\"{style}\", penwidth={penwidth:F1}, label=\"{label}\"];");
            }

            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
