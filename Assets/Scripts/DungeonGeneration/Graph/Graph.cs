using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class Graph
    {
        public Dictionary<GraphNode, HashSet<GraphNode>> AdjacencyList;

        public Graph()
        {
            AdjacencyList = new Dictionary<GraphNode, HashSet<GraphNode>>();
        }

        public void Clear()
        {
            AdjacencyList.Clear();
        }

        public void RemoveNode(GraphNode node)
        {
            if (AdjacencyList.ContainsKey(node))
            {
                AdjacencyList.Remove(node);
            }

            List<GraphNode> nodesToRemove = new List<GraphNode>();
            foreach (var key in AdjacencyList.Keys)
            {
                if (key is DoorGraphNode && AdjacencyList[key].Contains(node))
                {
                    nodesToRemove.Add(key);
                    continue;
                }

                AdjacencyList[key].Remove(node);
            }

            foreach (var ntr in nodesToRemove)
            {
                if (AdjacencyList.ContainsKey(ntr))
                {
                    AdjacencyList.Remove(ntr);
                }


                foreach (var key in AdjacencyList.Keys)
                {
                    AdjacencyList[key].Remove(ntr);
                }
            }
        }


        public List<GraphNode> GetNodes()
        {
            return new List<GraphNode>(AdjacencyList.Keys);
        }

        public GraphNode GetNodeByIndex(int index)
        {
            return GetNodes()[index];
        }

        public void AddNode(GraphNode node)
        {
            if (!AdjacencyList.ContainsKey(node))
            {
                AdjacencyList[node] = new HashSet<GraphNode>();
            }
        }

        public void RemoveEdge(GraphNode fromNode, GraphNode toNode)
        {
            if (AdjacencyList.ContainsKey(fromNode))
            {
                AdjacencyList[fromNode].Remove(toNode);
            }

            if (AdjacencyList.ContainsKey(toNode))
            {
                AdjacencyList[toNode].Remove(fromNode);
            }
        }

        public void AddEdge(GraphNode fromNode, GraphNode toNode, GraphNode edgeNode)
        {
            if (!AdjacencyList.ContainsKey(fromNode))
            {
                AddNode(fromNode);
            }

            if (!AdjacencyList.ContainsKey(toNode))
            {
                AddNode(toNode);
            }

            if (!AdjacencyList.ContainsKey(edgeNode))
            {
                AddNode(edgeNode);
            }

            AdjacencyList[fromNode].Add(edgeNode);

            AdjacencyList[edgeNode].Add(fromNode);
            AdjacencyList[edgeNode].Add(toNode);

            AdjacencyList[toNode].Add(edgeNode);
        }

        public List<GraphNode> GetNeighbors(GraphNode node)
        {
            return AdjacencyList[node].ToList();
        }

        public int GetNodeCount()
        {
            return AdjacencyList.Count;
        }

        public void PrintGraph()
        {
            foreach (var node in AdjacencyList)
            {
                Debug.Log($"{node.Key}: {string.Join(", ", node.Value)}");
            }
        }

        public float DoorEdgeDistance(DoorGraphNode doorGraphNode)
        {
            return Vector2.Distance(GetNeighbors(doorGraphNode)[0].Vertex.Position,
                GetNeighbors(doorGraphNode)[1].Vertex.Position);
        }

        // Breadth-First Search (BFS)
        public void BFS(GraphNode v)
        {
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();
            Queue<GraphNode> Q = new Queue<GraphNode>();

            Q.Enqueue(v);
            discovered.Add(v);

            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                Debug.Log(v);
                foreach (GraphNode w in GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                    }
                }
            }
        }

        // Depth-First Search (DFS)
        public void DFS(GraphNode v)
        {
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();

            Stack<GraphNode> S = new Stack<GraphNode>();
            S.Push(v);
            while (S.Count > 0)
            {
                v = S.Pop();
                if (!discovered.Contains(v))
                {
                    discovered.Add(v);
                    Debug.Log(v);
                    foreach (GraphNode w in GetNeighbors(v))
                    {
                        S.Push(w);
                    }
                }
            }
        }

        public bool IsConnectedWhenRemoving(GraphNode v)
        {
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();
            Queue<GraphNode> Q = new Queue<GraphNode>();

            var startNode = GetNodeByIndex(0) == v ? GetNodeByIndex(1) : GetNodeByIndex(0);
            Q.Enqueue(startNode);
            discovered.Add(startNode);
            discovered.Add(v);

            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                foreach (GraphNode w in GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                    }
                }
            }

            return discovered.Count == GetNodeCount();
        }


    }
}