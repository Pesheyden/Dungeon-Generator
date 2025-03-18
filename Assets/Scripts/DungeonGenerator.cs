using System.Collections.Generic;
using System.Threading.Tasks;
using DelaunayTriangulation.Objects2D;
using DelaunayTriangulation;
using DelaunayTriangulation.Links;
using NaughtyAttributes;
using UnityEngine;
using System.Linq;
using Random = System.Random;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt _dungeon;
    [SerializeField] private Vector2Int _minimalRoomSize;
    [SerializeField] private int _wallWidth;
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _randomnessBoundaries;
    [SerializeField] private Vector2Int _doorSize;

    [Header("Debug")] [SerializeField] private bool _debugRooms;
    [SerializeField] private bool _debugWalls;
    [SerializeField] private bool _debugConnections;
    [SerializeField] private int _awaitTime = 10;

    public RoomNode RootNode;
    public List<RoomNode> Rooms = new();

    private DTriangulation _dTriangulation;
    private List<Edge> _edges = new();

    private List<RectInt> _debugRoomsList = new();
    private List<RectInt> _debugWallsList = new();

    private int _roomsAmount = 0;

    private Random _random;
    
    public Graph<GraphNode> Graph = new Graph<GraphNode>(); 

    private void Start()
    {
        GenerateDungeon();
    }

    [Button]
    private async void GenerateDungeon()
    {
        _random = new Random(_seed);
        RootNode = new RoomNode(_dungeon);
        await RecursionSplit(RootNode, false);
        await FillGraph();
    }



    private async Task RecursionSplit(RoomNode startRoom, bool doHorizontalSplit)
    {
        _roomsAmount++;


        RectInt newRoom1;
        RectInt newRoom2;

        //Create new raw rooms
        float randomNumber = (float)_random.NextDouble();
        if (doHorizontalSplit)
        {
            int newHeight = (int)Mathf.Lerp(startRoom.RoomValue.height * _randomnessBoundaries.x,
                startRoom.RoomValue.height * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width,
                newHeight + _wallWidth / 2);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y + newHeight - _wallWidth / 2,
                startRoom.RoomValue.width,
                startRoom.RoomValue.height - newHeight + _wallWidth / 2);
        }
        else
        {
            int newWidth = (int)Mathf.Lerp(startRoom.RoomValue.width * _randomnessBoundaries.x,
                startRoom.RoomValue.width * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x + newWidth - _wallWidth / 2,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width - newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);
        }

        //If new rooms are too small - break
        if (newRoom1.height <= _minimalRoomSize.y || newRoom2.height <= _minimalRoomSize.y)
        {
            int newWidth = (int)Mathf.Lerp(startRoom.RoomValue.width * _randomnessBoundaries.x,
                startRoom.RoomValue.width * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x + newWidth - _wallWidth / 2,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width - newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);

            if (newRoom1.width <= _minimalRoomSize.x || newRoom2.width <= _minimalRoomSize.x)
            {
                Rooms.Add(startRoom);
                return;
            }
        }

        if (newRoom1.width <= _minimalRoomSize.x || newRoom2.width <= _minimalRoomSize.x)
        {
            int newHeight = (int)Mathf.Lerp(startRoom.RoomValue.height * _randomnessBoundaries.x,
                startRoom.RoomValue.height * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width,
                newHeight + _wallWidth / 2);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y + newHeight - _wallWidth / 2,
                startRoom.RoomValue.width,
                startRoom.RoomValue.height - newHeight + _wallWidth / 2);

            if (newRoom1.height <= _minimalRoomSize.y || newRoom2.height <= _minimalRoomSize.y)
            {
                Rooms.Add(startRoom);
                return;
            }
        }

        //Extract wall
        /*RectInt newWall = AlgorithmsUtils.Intersect(newRoom1, newRoom2);

        //Remove wall from rooms
        if (doHorizontalSplit)
        {
            newRoom1.height -= _wallWidth;
            newRoom2.height -= _wallWidth;
            newRoom2.y += _wallWidth;
        }
        else
        {
            newRoom1.width -= _wallWidth;
            newRoom2.width -= _wallWidth;
            newRoom2.x += _wallWidth;
        }*/

        //Filling nodes
        RoomNode roomNode1 = new RoomNode(newRoom1);
        RoomNode roomNode2 = new RoomNode(newRoom2);

        startRoom.Room1 = roomNode1;
        startRoom.Room2 = roomNode2;
        //startRoom.WallValue = newWall;

        //Debug
        _debugRoomsList.Add(newRoom1);
        _debugRoomsList.Add(newRoom2);
        //_debugWallsList.Add(newWall);
        _debugRoomsList.Remove(startRoom.RoomValue);

        await Task.Delay(_awaitTime);
        await RecursionSplit(roomNode1, !doHorizontalSplit);
        await RecursionSplit(roomNode2, !doHorizontalSplit);
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

        foreach (var node in Graph.AdjacencyList)
        {
            Debug.Log($"Node{node.Key.Vertex.Position}: {string.Join(", ", node.Value)}");
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
        Debug.Log(doorSize + " " + intersect);

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

    public static List<Edge> MinimumSpanningTree(List<Edge> edges, Vertex start)
    {
        HashSet<Vertex> openSet = new HashSet<Vertex>();
        HashSet<Vertex> closedSet = new HashSet<Vertex>();

        foreach (var edge in edges)
        {
            openSet.Add(edge.A.Vertex);
            openSet.Add(edge.B.Vertex);
        }

        closedSet.Add(start);

        List<Edge> results = new List<Edge>();

        while (openSet.Count > 0)
        {
            bool chosen = false;
            Edge chosenEdge = null;
            float minWeight = float.PositiveInfinity;

            foreach (var edge in edges)
            {
                int closedVertices = 0;
                if (!closedSet.Contains(edge.A.Vertex)) closedVertices++;
                if (!closedSet.Contains(edge.B.Vertex)) closedVertices++;
                if (closedVertices != 1) continue;

                if (edge.Distance < minWeight)
                {
                    chosenEdge = edge;
                    chosen = true;
                    minWeight = edge.Distance;
                }
            }

            if (!chosen) break;
            results.Add(chosenEdge);
            openSet.Remove(chosenEdge.A.Vertex);
            openSet.Remove(chosenEdge.B.Vertex);
            closedSet.Add(chosenEdge.A.Vertex);
            closedSet.Add(chosenEdge.B.Vertex);
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