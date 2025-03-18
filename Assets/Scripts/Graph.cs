using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph<T>
{
    public Dictionary<T, HashSet<T>> AdjacencyList;

    public Graph()
    {
        AdjacencyList = new Dictionary<T, HashSet<T>>();
    }
    
    public void Clear() 
    { 
        AdjacencyList.Clear(); 
    }
    
    public void RemoveNode(T node)
    {
        if (AdjacencyList.ContainsKey(node))
        {
            AdjacencyList.Remove(node);
        }
        
        foreach (var key in AdjacencyList.Keys)
        {
            AdjacencyList[key].Remove(node);
        }
    }
    
    public List<T> GetNodes()
    {
        return new List<T>(AdjacencyList.Keys);
    }
    
    public void AddNode(T node)
    {
        if (!AdjacencyList.ContainsKey(node))
        {
            AdjacencyList[node] = new HashSet<T>();
        }
    }

    public void RemoveEdge(T fromNode, T toNode)
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

    public void AddEdge(T fromNode, T toNode, T edgeNode) { 
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
    
    public List<T> GetNeighbors(T node) 
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
    
    // Breadth-First Search (BFS)
    public void BFS(T v)
    {
        HashSet<T> discovered = new HashSet<T>();
        Queue<T> Q = new Queue<T>();
        
        Q.Enqueue(v);
        discovered.Add(v);

        while (Q.Count > 0)
        {
            v = Q.Dequeue();
            Debug.Log(v);
            foreach (T w in GetNeighbors(v))
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
    public void DFS(T v)
    {
        HashSet<T> discovered = new HashSet<T>();
        
        Stack<T> S = new Stack<T>();
        S.Push(v);
        while (S.Count > 0)
        {
            v = S.Pop();
            if (!discovered.Contains(v))
            {
                discovered.Add(v);
                Debug.Log(v);
                foreach (T w in GetNeighbors(v))
                {
                    S.Push(w);
                }
            }
        }
    }
}