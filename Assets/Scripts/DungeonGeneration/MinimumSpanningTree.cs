using System.Collections.Generic;
using System.Linq;
using DelaunayTriangulation.Objects2D;

namespace DungeonGeneration.Graph
{
    public class MinimumSpanningTree
    {
        public static List<DoorGraphNode> GetMinimumSpanningTree(Graph graph)
        {
            HashSet<Vertex> openSet = new HashSet<Vertex>();
            HashSet<Vertex> closedSet = new HashSet<Vertex>();

            var doorNodes = graph.GetNodes().Where(node => node is DoorGraphNode).ToList();

            foreach (var edge in doorNodes)
            {
                foreach (var node in graph.GetNeighbors(edge))
                {
                    openSet.Add(node.Vertex);
                    openSet.Add(node.Vertex);
                }
            }

            closedSet.Add(graph.GetNodeByIndex(0).Vertex);

            List<DoorGraphNode> results = new List<DoorGraphNode>();

            while (openSet.Count > 0)
            {
                bool chosen = false;
                DoorGraphNode chosenEdge = null;
                float minWeight = float.PositiveInfinity;

                foreach (var edge in doorNodes)
                {
                    int closedVertices = 0;
                    foreach (var node in graph.GetNeighbors(edge))
                    {
                        if (!closedSet.Contains(node.Vertex)) closedVertices++;
                    }

                    if (closedVertices != 1) continue;

                    if (graph.DoorEdgeDistance(edge as DoorGraphNode) < minWeight)
                    {
                        chosenEdge = edge as DoorGraphNode;
                        chosen = true;
                        minWeight = graph.DoorEdgeDistance(edge as DoorGraphNode);
                    }
                }

                if (!chosen) break;
                results.Add(chosenEdge);
                foreach (var node in graph.GetNeighbors(chosenEdge))
                {
                    openSet.Remove(node.Vertex);
                    closedSet.Add(node.Vertex);
                }
            }

            return results;
        }
    }
}