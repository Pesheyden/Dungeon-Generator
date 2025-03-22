using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DelaunayTriangulation.Objects2D;
using DelaunayTriangulation;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;
using Random = System.Random;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt _dungeon;
    [SerializeField] private Vector2Int _minimalRoomSize;
    [SerializeField] private int _wallWidth;
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _randomnessBoundaries;
    [SerializeField] private Vector2Int _doorSize;
    [SerializeField] private float _roomsRemovePercentage;

    [Header("Debug")] [SerializeField] private bool _debugRooms;
    [SerializeField] private bool _debugWalls;
    [SerializeField] private bool _debugConnections;
    [SerializeField] private int _awaitTime = 10;
    
    public List<RectInt> Rooms;

    private DTriangulation _dTriangulation;
    private List<Edge> _edges;

    private List<RectInt> _debugRoomsList;
    private List<RectInt> _debugWallsList;
    

    private Random _random;
    
    public Graph Graph; 

    private void Start()
    {
        GenerateDungeon();
    }

    [Button]
    private async void GenerateDungeon()
    {
        ResetValues();
        await RecursionSplit(_dungeon, false);
        await FillGraph();
        Debug.Log(Time.realtimeSinceStartup);
    }

    private void ResetValues()
    {
        Rooms = new();
        _edges = new();
        _debugRoomsList = new();
        _debugWallsList = new();
        Graph = new Graph();
        _random = new Random(_seed);
        
        DebugDrawingBatcher.ClearCalls();
    }


    private async Task RecursionSplit(RectInt startRoom, bool doHorizontalSplit)
    {


        ValueTuple<RectInt, RectInt> newRooms; 
        //Create new raw rooms
        float randomNumber = (float)_random.NextDouble();
        if (doHorizontalSplit)
        {
            newRooms = HorizontalSplit(startRoom, randomNumber);
        }
        else
        {
            newRooms = VerticalSplit(startRoom, randomNumber);
        }

        //If new rooms are too small - break
        if (newRooms.Item1.height <= _minimalRoomSize.y || newRooms.Item2.height <= _minimalRoomSize.y)
        {
            newRooms = VerticalSplit(startRoom, randomNumber);

            if (newRooms.Item1.width <= _minimalRoomSize.x || newRooms.Item2.width <= _minimalRoomSize.x)
            {
                Rooms.Add(startRoom);
                return;
            }
        }

        if (newRooms.Item1.width <= _minimalRoomSize.x || newRooms.Item2.width <= _minimalRoomSize.x)
        {
            newRooms = HorizontalSplit(startRoom, randomNumber);

            if (newRooms.Item1.height <= _minimalRoomSize.y || newRooms.Item2.height <= _minimalRoomSize.y)
            {
                Rooms.Add(startRoom);
                return;
            }
        }

        //Debug
        _debugRoomsList.Add(newRooms.Item1);
        _debugRoomsList.Add(newRooms.Item2);
        _debugRoomsList.Remove(startRoom);

        await Task.Delay(_awaitTime);
        await RecursionSplit(newRooms.Item1, !doHorizontalSplit);
        await RecursionSplit(newRooms.Item2, !doHorizontalSplit);

        (RectInt,RectInt) HorizontalSplit(RectInt roomNode, float f)
        {
            int newHeight = (int)Mathf.Lerp(roomNode.height * _randomnessBoundaries.x,
                roomNode.height * _randomnessBoundaries.y, f);

            newRooms.Item1 = new RectInt(
                roomNode.x,
                roomNode.y,
                roomNode.width,
                newHeight + _wallWidth / 2);

            newRooms.Item2 = new RectInt(
                roomNode.x,
                roomNode.y + newHeight - _wallWidth / 2,
                roomNode.width,
                roomNode.height - newHeight + _wallWidth / 2);
            return (newRooms.Item1,newRooms.Item2);
        }

        (RectInt,RectInt) VerticalSplit(RectInt startRoom1, float randomNumber1)
        {
            int newWidth = (int)Mathf.Lerp(startRoom1.width * _randomnessBoundaries.x,
                startRoom1.width * _randomnessBoundaries.y, randomNumber1);

            newRooms.Item1 = new RectInt(
                startRoom1.x,
                startRoom1.y,
                newWidth + _wallWidth / 2,
                startRoom1.height);

            newRooms.Item2 = new RectInt(
                startRoom1.x + newWidth - _wallWidth / 2,
                startRoom1.y,
                startRoom1.width - newWidth + _wallWidth / 2,
                startRoom1.height);
            return (newRooms.Item1,newRooms.Item2);
        }
    }
    
    private async Task FillGraph()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in Rooms)
        {
            Vertex vertex = new Vertex((Vector2)room.RoomValue.position + (Vector2)room.RoomValue.size / 2);
            vertices.Add(vertex);

            RoomGraphNode roomGraphNode = new RoomGraphNode(vertex, room.RoomValue);
            Graph.AddNode(roomGraphNode);
        }

        #region Triangulate

        List<GraphNode> Nodes = Graph.GetNodes();
        List<Triangle> Triangles = new List<Triangle>();
        //Create a starting triangle so all other vertexes are inside of it
            
            //Determine min and max vertices' positions
            float minX = Nodes[0].Vertex.Position.x;
            float minY = Nodes[0].Vertex.Position.y;
            float maxX = minX;
            float maxY = minY;
            
            foreach (var node in Nodes)
            {
                if (node.Vertex.Position.x < minX) minX = node.Vertex.Position.x;
                if (node.Vertex.Position.x > maxX) maxX = node.Vertex.Position.x;
                if (node.Vertex.Position.y < minY) minY = node.Vertex.Position.y;
                if (node.Vertex.Position.y > maxY) maxY = node.Vertex.Position.y;
            }

            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            //Create new vertices
            Vertex p1 = new Vertex(new Vector2(minX - 1, minY - 1));
            Vertex p2 = new Vertex(new Vector2(minX - 1, maxY + deltaMax));
            Vertex p3 = new Vertex(new Vector2(maxX + deltaMax, minY - 1));
            
            //Create new rooms
            RoomGraphNode r1 = new RoomGraphNode(p1, new RectInt());
            RoomGraphNode r2 = new RoomGraphNode(p2, new RectInt());
            RoomGraphNode r3 = new RoomGraphNode(p3, new RectInt());

            //Add triangle
            Triangles.Add(new Triangle(r1, r2, r3));

            //Triangulate
            //Loop through all vertices
            foreach (var node in Nodes)
            {
                List<Edge> polygon = new List<Edge>();

                //Check if vertex is inside the existing triangle. If yes divide it into edges
                foreach (var t in Triangles)
                {
                    if (t.CircumCircleContains(node.Vertex.Position))
                    {
                        t.IsBad = true;
                        polygon.Add(new Edge(t.A, t.B));
                        polygon.Add(new Edge(t.B, t.C));
                        polygon.Add(new Edge(t.C, t.A));
                    }
                }

                Triangles.RemoveAll((Triangle t) => t.IsBad);

                //Remove almost equal edges
                for (int i = 0; i < polygon.Count; i++)
                {
                    for (int j = i + 1; j < polygon.Count; j++)
                    {
                        if (Edge.AlmostEqual(polygon[i], polygon[j]))
                        {
                            polygon[i].IsBad = true;
                            polygon[j].IsBad = true;
                        }
                    }
                }

                polygon.RemoveAll((Edge e) => e.IsBad);

                //Create new triangles
                foreach (var edge in polygon)
                {
                    Triangle newTriangle = new Triangle(edge.A, edge.B, node);
                    Triangles.Add(newTriangle);
                }
            }

            //Remove all triangles that were formed with vertices of the starting one
            Triangles.RemoveAll((Triangle t) =>
                t.ContainsVertex(p1.Position) || t.ContainsVertex(p2.Position) || t.ContainsVertex(p3.Position));

            //Fill Edges list with unique edges 
            HashSet<Edge> edgeSet = new HashSet<Edge>();

            foreach (var t in Triangles)
            {
                var ab = new Edge(t.A, t.B);
                
                var bc = new Edge(t.B, t.C);
                
                var ca = new Edge(t.C, t.A);

                if (edgeSet.Add(ab))
                {
                    _edges.Add(ab);
                    if(GenerateDoor(t.A.Size, t.B.Size, out var abDoor))
                        Graph.AddEdge(t.A, t.B, abDoor);
                }

                if (edgeSet.Add(bc))
                {
                    _edges.Add(bc);
                    if(GenerateDoor(t.B.Size, t.C.Size, out var bcDoor))
                        Graph.AddEdge(t.B, t.C, bcDoor);
                }

                if (edgeSet.Add(ca))
                {
                    _edges.Add(ca);
                    if(GenerateDoor(t.C.Size, t.A.Size, out var caDoor))
                        Graph.AddEdge(t.C, t.A, caDoor);
                  
                }
                await Task.Delay(_awaitTime);

            }
        #endregion
        
        DebugDrawingBatcher.BatchCall((() =>
        {
            
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();
            Dictionary<GraphNode, GraphNode> discoveredEdges = new Dictionary<GraphNode, GraphNode>();
            Queue<GraphNode> Q = new Queue<GraphNode>();

            var v = Graph.GetNodes()[0];
            Q.Enqueue(v);
            discovered.Add(v);

            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                DebugExtension.DebugWireSphere(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), Color.blue);
                foreach (GraphNode w in Graph.GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                        
                    }
                    Debug.DrawLine(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), new Vector3(w.Vertex.Position.x, 0 ,w.Vertex.Position.y), Color.blue);
                }
            }
        }));
        
        DebugDrawingBatcher.BatchCall((() =>
        {
            
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();
            Dictionary<GraphNode, GraphNode> discoveredEdges = new Dictionary<GraphNode, GraphNode>();
            Queue<GraphNode> Q = new Queue<GraphNode>();

            var v = Graph.GetNodes()[0];
            Q.Enqueue(v);
            discovered.Add(v);

            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                DebugExtension.DebugWireSphere(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), Color.green);
                foreach (GraphNode w in Graph.GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                        
                    }
                    Debug.DrawLine(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), new Vector3(w.Vertex.Position.x, 0 ,w.Vertex.Position.y), Color.green);
                }
            }
        }));

        var roomNodes = Graph.GetNodes().Where(node => node is RoomGraphNode).ToList();
        roomNodes.Sort((a, b) => (a.Size.width * a.Size.height).CompareTo(b.Size.width * b.Size.height));


        for (int i = 0; i < roomNodes.Count * _roomsRemovePercentage / 100; i++)
        {
            // if (Graph.GetNeighbors(roomNodes[i]).Count <= 2)
            //     continue;
            
            if(!Graph.IsConnectedWhenRemoving(roomNodes[i]))
                continue;
            
            Graph.RemoveNode(roomNodes[i]);

            await Task.Delay(_awaitTime);
        }

        DebugDrawingBatcher.BatchCall((() =>
        {
            
            HashSet<GraphNode> discovered = new HashSet<GraphNode>();
            Dictionary<GraphNode, GraphNode> discoveredEdges = new Dictionary<GraphNode, GraphNode>();
            Queue<GraphNode> Q = new Queue<GraphNode>();

            var v = Graph.GetNodes()[0];
            Q.Enqueue(v);
            discovered.Add(v);

            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                DebugExtension.DebugWireSphere(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), Color.magenta);
                foreach (GraphNode w in Graph.GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                        AlgorithmsUtils.DebugRectInt(w.Size, Color.green);
                    }
                    Debug.DrawLine(new Vector3(v.Vertex.Position.x, 0 ,v.Vertex.Position.y), new Vector3(w.Vertex.Position.x, 0 ,w.Vertex.Position.y), Color.magenta);
                }
            }
        }));

        var newEdges = MinimumSpanningTree(Graph);
        var edges = Graph.GetNodes().Where(node => node is DoorGraphNode ).ToList();

        foreach (var edge in newEdges)
        {
            edges.Remove(edge);
        }

        foreach (var edge in edges)
        {
            Graph.RemoveNode(edge);
            await Task.Delay(_awaitTime);
        }
        

    }

    private bool GenerateDoor(RectInt room1, RectInt room2, out DoorGraphNode doorGraphNode)
    {
        var intersect = AlgorithmsUtils.Intersect(room1, room2);


        DebugDrawingBatcher.BatchCall((() =>
        {
            AlgorithmsUtils.DebugRectInt(intersect, Color.magenta);
        }));

        float random = (float)_random.NextDouble();

        RectInt doorSize;
        if (intersect.width > intersect.height)
        {
            if (intersect.width - _wallWidth * 2 < _doorSize.x)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = new RectInt
            (
                (int)Mathf.Clamp(Mathf.Lerp(intersect.x + _wallWidth, intersect.x + intersect.width - _wallWidth * 2, random), intersect.x + _wallWidth, intersect.x + _wallWidth),
                intersect.y,
                _doorSize.x,
                _doorSize.y
            );
        }
        else
        {
            if (intersect.height - _wallWidth * 2 < _doorSize.y)
            {
                doorGraphNode = null;
                return false;
            }
            doorSize = new RectInt
            (
                intersect.x,
                (int)Mathf.Clamp(Mathf.Lerp(intersect.y + _wallWidth, intersect.y + intersect.height - _wallWidth * 2, random), intersect.y + _wallWidth, intersect.y + intersect.height - _wallWidth * 2),
                _doorSize.x,
                _doorSize.y
            );
        }
        Vertex vertex = new Vertex(new Vector2(doorSize.x + (float)doorSize.width / 2, doorSize.y + (float)doorSize.height / 2));
        DebugDrawingBatcher.BatchCall((() =>
        {
            AlgorithmsUtils.DebugRectInt(doorSize, Color.blue);
        }));

        doorGraphNode = new DoorGraphNode(vertex, doorSize);

        
        return true;
    }
    

    //Traverse
    private async Task Traverse(RoomNode inputNode)
    {
        if (inputNode.Room1 == null || inputNode.Room2 == null)
        {
            return;
        }

        await Task.Delay(100);

        await Traverse(inputNode.Room1);
        await Traverse(inputNode.Room2);
    }

    private void Update()
    {
        if (_debugRooms)
        {
            foreach (var room in _debugRoomsList)
            {
                AlgorithmsUtils.DebugRectInt(room, Color.yellow);
            }
        }

        if (_debugWalls)
        {
            foreach (var wall in _debugWallsList)
            {
                AlgorithmsUtils.DebugRectInt(wall, Color.blue);
            }
        }

        if (_debugConnections)
        {
            /*if (_dTriangulation != null && _dTriangulation.Edges != null)
            {
                foreach (var edge in _dTriangulation.Edges)
                {
                    Debug.DrawLine(
                        new Vector3(edge.A.Vertex.Position.x, -.1f, edge.A.Vertex.Position.y),
                        new Vector3(edge.B.Vertex.Position.x, -.1f, edge.B.Vertex.Position.y),
                        Color.gray
                    );
                }

                foreach (var vertex in _dTriangulation.Vertices)
                {
                    DebugExtension.DebugWireSphere(
                        new Vector3(vertex.Position.x, 0, vertex.Position.y),
                        Color.magenta
                    );
                }
            }*/

            foreach (var edge in _edges)
            {
                Debug.DrawLine(
                    new Vector3(edge.A.Vertex.Position.x, 0, edge.A.Vertex.Position.y),
                    new Vector3(edge.B.Vertex.Position.x, 0, edge.B.Vertex.Position.y),
                    Color.magenta
                );
            }
        }

        AlgorithmsUtils.DebugRectInt(_dungeon, Color.red);
    }

    public static List<DoorGraphNode> MinimumSpanningTree(Graph graph)
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

            foreach (var edge  in doorNodes )
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

public class RoomNode
{
    public RoomNode(RectInt roomValue)
    {
        RoomValue = roomValue;
        Room1 = Room2 = null;
    }

    public RectInt RoomValue;
    public RoomNode Room1;
    public RoomNode Room2;
}