using System;
using System.Collections.Generic;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Tests
{
    public static class AlgorithmCorrectnessTests
    {
        public static bool RunSuite()
        {
            Console.WriteLine("\n==================================================");
            Console.WriteLine("             RUNNING CORRECTNESS TESTS            ");
            Console.WriteLine("==================================================");

            bool allPassed = true;

            allPassed &= TestSimplePath();
            allPassed &= TestDisconnectedGraph();
            allPassed &= TestMultipleEqualPaths();
            allPassed &= TestRoadClosures();
            allPassed &= TestTrafficMultiplier();
            allPassed &= TestNegativeCycleDetection();
            allPassed &= TestAStarEqualsDijkstra();

            Console.WriteLine("==================================================");
            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("        ALL CORRECTNESS TESTS PASSED SUCCESS!     ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("        SOME CORRECTNESS TESTS FAILED!            ");
                Console.ResetColor();
            }
            Console.WriteLine("==================================================");

            return allPassed;
        }

        private static bool TestSimplePath()
        {
            var graph = new Graph(5);
            // Coordinates for A* heuristic consistency (straight line layout)
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 2, 0, "1"));
            graph.AddVertex(new Vertex(2, 5, 0, "2"));
            graph.AddVertex(new Vertex(3, 6, 0, "3"));
            graph.AddVertex(new Vertex(4, 8, 0, "4"));

            // Edges
            graph.AddEdge(0, 1, 2.0, 60.0); // 2 mins
            graph.AddEdge(1, 2, 3.0, 60.0); // 3 mins
            graph.AddEdge(0, 2, 6.0, 60.0); // 6 mins
            graph.AddEdge(2, 3, 1.0, 60.0); // 1 min
            graph.AddEdge(3, 4, 2.0, 60.0); // 2 mins
            graph.AddEdge(0, 4, 15.0, 60.0); // 15 mins

            double expected = 2 + 3 + 1 + 2; // 8.0 mins (0 -> 1 -> 2 -> 3 -> 4)

            var dijkstra = new DijkstraSolver().Solve(graph, 0, 4);
            var astar = new AStarSolver().Solve(graph, 0, 4, 60.0);
            var bidijkstra = new BidirectionalDijkstraSolver().Solve(graph, 0, 4);

            bool passed = Math.Abs(dijkstra.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(astar.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(bidijkstra.TotalTravelTimeMinutes - expected) < 1e-5;

            PrintTestResult("Simple 5-Vertex Shortest Path", passed, expected, dijkstra.TotalTravelTimeMinutes);
            return passed;
        }

        private static bool TestDisconnectedGraph()
        {
            var graph = new Graph(5);
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 2, 0, "1"));
            graph.AddVertex(new Vertex(2, 4, 0, "2"));
            graph.AddVertex(new Vertex(3, 6, 0, "3"));
            graph.AddVertex(new Vertex(4, 8, 0, "4"));

            graph.AddEdge(0, 1, 1.0, 60.0);
            graph.AddEdge(3, 4, 1.0, 60.0);

            var dijkstra = new DijkstraSolver().Solve(graph, 0, 4);
            var astar = new AStarSolver().Solve(graph, 0, 4, 60.0);
            var bidijkstra = new BidirectionalDijkstraSolver().Solve(graph, 0, 4);

            bool passed = !dijkstra.IsReachable && !astar.IsReachable && !bidijkstra.IsReachable;
            PrintTestResult("Disconnected Graph (Unreachable)", passed, -1.0, dijkstra.TotalTravelTimeMinutes);
            return passed;
        }

        private static bool TestMultipleEqualPaths()
        {
            var graph = new Graph(4);
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 5, 2, "1"));
            graph.AddVertex(new Vertex(2, 5, -2, "2"));
            graph.AddVertex(new Vertex(3, 10, 0, "3"));

            graph.AddEdge(0, 1, 5.0, 60.0);
            graph.AddEdge(1, 3, 5.0, 60.0);
            graph.AddEdge(0, 2, 5.0, 60.0);
            graph.AddEdge(2, 3, 5.0, 60.0);

            double expected = 10.0;

            var dijkstra = new DijkstraSolver().Solve(graph, 0, 3);
            var astar = new AStarSolver().Solve(graph, 0, 3, 60.0);
            var bidijkstra = new BidirectionalDijkstraSolver().Solve(graph, 0, 3);

            bool passed = Math.Abs(dijkstra.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(astar.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(bidijkstra.TotalTravelTimeMinutes - expected) < 1e-5;

            PrintTestResult("Multiple Paths of Equal Length", passed, expected, dijkstra.TotalTravelTimeMinutes);
            return passed;
        }

        private static bool TestRoadClosures()
        {
            var graph = new Graph(5);
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 2, 0, "1"));
            graph.AddVertex(new Vertex(2, 5, 0, "2"));
            graph.AddVertex(new Vertex(3, 6, 0, "3"));
            graph.AddVertex(new Vertex(4, 8, 0, "4"));

            graph.AddEdge(0, 1, 2.0, 60.0);
            graph.AddEdge(1, 2, 3.0, 60.0);
            graph.AddEdge(0, 2, 6.0, 60.0);
            graph.AddEdge(2, 3, 1.0, 60.0);
            graph.AddEdge(3, 4, 2.0, 60.0);
            graph.AddEdge(0, 4, 15.0, 60.0);

            // Close edge 1 -> 2
            graph.CloseEdge(1, 2);

            // Cost without 1->2 should be 0->2(6) + 2->3(1) + 3->4(2) = 9 mins
            double expected = 9.0;

            var dijkstra = new DijkstraSolver().Solve(graph, 0, 4);
            var astar = new AStarSolver().Solve(graph, 0, 4, 60.0);
            var bidijkstra = new BidirectionalDijkstraSolver().Solve(graph, 0, 4);

            bool passed = Math.Abs(dijkstra.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(astar.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(bidijkstra.TotalTravelTimeMinutes - expected) < 1e-5;

            PrintTestResult("Road Closure Re-routing", passed, expected, dijkstra.TotalTravelTimeMinutes);
            return passed;
        }

        private static bool TestTrafficMultiplier()
        {
            var graph = new Graph(5);
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 2, 0, "1"));
            graph.AddVertex(new Vertex(2, 5, 0, "2"));
            graph.AddVertex(new Vertex(3, 6, 0, "3"));
            graph.AddVertex(new Vertex(4, 8, 0, "4"));

            var e01 = graph.AddEdge(0, 1, 2.0, 60.0);
            var e12 = graph.AddEdge(1, 2, 3.0, 60.0);
            var e02 = graph.AddEdge(0, 2, 6.0, 60.0);
            var e23 = graph.AddEdge(2, 3, 1.0, 60.0);
            var e34 = graph.AddEdge(3, 4, 2.0, 60.0);
            graph.AddEdge(0, 4, 15.0, 60.0);

            // Apply traffic multipliers
            // Route 0 -> 1 -> 2 -> 3 -> 4:
            // 0 -> 1 has normal traffic (1.0). Cost = 2.0
            // 1 -> 2 has High traffic (1.5). Cost = 3.0 * 1.5 = 4.5
            // 2 -> 3 has High traffic (1.5). Cost = 1.0 * 1.5 = 1.5
            // 3 -> 4 has normal traffic (1.0). Cost = 2.0
            // Total cost = 2.0 + 4.5 + 1.5 + 2.0 = 10.0 mins
            
            // Route 0 -> 2 -> 3 -> 4:
            // 0 -> 2 has normal traffic (1.0). Cost = 6.0
            // 2 -> 3 has High traffic (1.5). Cost = 1.0 * 1.5 = 1.5
            // 3 -> 4 has normal traffic (1.0). Cost = 2.0
            // Total cost = 6.0 + 1.5 + 2.0 = 9.5 mins
            
            e12.Traffic = TrafficLevel.High;
            e12.TrafficMultiplier = 1.5;
            
            e23.Traffic = TrafficLevel.High;
            e23.TrafficMultiplier = 1.5;

            double expected = 9.5;

            var dijkstra = new DijkstraSolver().Solve(graph, 0, 4);
            var astar = new AStarSolver().Solve(graph, 0, 4, 60.0);
            var bidijkstra = new BidirectionalDijkstraSolver().Solve(graph, 0, 4);

            bool passed = Math.Abs(dijkstra.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(astar.TotalTravelTimeMinutes - expected) < 1e-5 &&
                          Math.Abs(bidijkstra.TotalTravelTimeMinutes - expected) < 1e-5;

            PrintTestResult("Traffic Conditions Re-routing", passed, expected, dijkstra.TotalTravelTimeMinutes);
            return passed;
        }

        private static bool TestNegativeCycleDetection()
        {
            var graph = new Graph(3);
            graph.AddVertex(new Vertex(0, 0, 0, "0"));
            graph.AddVertex(new Vertex(1, 0, 0, "1"));
            graph.AddVertex(new Vertex(2, 0, 0, "2"));

            graph.AddEdge(0, 1, 2.0, 60.0);
            graph.AddEdge(1, 2, 3.0, 60.0);
            var eCycle = graph.AddEdge(2, 0, 1.0, 60.0);
            
            // Force negative weight on the cycle edge
            eCycle.TravelTimeMinutes = -10.0;

            var bf = new BellmanFordSolver().Solve(graph, 0, 2);

            bool passed = bf.HasNegativeCycle;
            PrintTestResult("Bellman-Ford Negative Cycle Detection", passed, 1.0, bf.HasNegativeCycle ? 1.0 : 0.0);
            return passed;
        }

        private static bool TestAStarEqualsDijkstra()
        {
            var graph = CityGraphGenerator.Generate(100, 500, 42, GraphFamily.RandomSparse);
            
            var dijkstra = new DijkstraSolver().Solve(graph, 0, 99);
            var astar = new AStarSolver().Solve(graph, 0, 99, 100.0);

            bool passed = Math.Abs(dijkstra.TotalTravelTimeMinutes - astar.TotalTravelTimeMinutes) < 1e-5;
            PrintTestResult("A* Consistency against Dijkstra", passed, dijkstra.TotalTravelTimeMinutes, astar.TotalTravelTimeMinutes);
            return passed;
        }

        private static void PrintTestResult(string name, bool passed, double expected, double actual)
        {
            string status = passed ? "PASSED" : "FAILED";
            Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"[{status}] ");
            Console.ResetColor();
            
            string expectedStr = expected == -1.0 ? "Unreachable" : $"{expected:F4} min";
            string actualStr = actual == -1.0 ? "Unreachable" : $"{actual:F4} min";

            if (name == "Bellman-Ford Negative Cycle Detection")
            {
                expectedStr = "Cycle Detected";
                actualStr = actual == 1.0 ? "Cycle Detected" : "No Cycle";
            }

            Console.WriteLine($"{name,-40} | Exp: {expectedStr,-15} | Act: {actualStr}");
        }
    }
}
