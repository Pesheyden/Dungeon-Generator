using System.Collections.Generic;
using System.Threading.Tasks;
using DelaunayTriangulation.Objects2D;
using DelaunayTriangulation;
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

    [Header("Debug")] 
    [SerializeField] private bool _debugRooms;
    [SerializeField] private bool _debugWalls;
    [SerializeField] private bool _debugConnections;

    public RoomNode RootNode;
    public List<RoomNode> Rooms = new();

    private DTriangulation _dTriangulation;
    private List<Edge> _edges = new();

    private List<RectInt> _debugRoomsList = new();
    private List<RectInt> _debugWallsList = new();

    private int _roomsAmount = 0;

    private Random _random;

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
        Triangulate();
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
            int newHeight = (int)Mathf.Lerp(startRoom.RoomValue.height * _randomnessBoundaries.x, startRoom.RoomValue.height * _randomnessBoundaries.y, randomNumber);

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
            int newWidth = (int)Mathf.Lerp(startRoom.RoomValue.width * _randomnessBoundaries.x, startRoom.RoomValue.width * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                newWidth  + _wallWidth / 2,
                startRoom.RoomValue.height);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x + newWidth - _wallWidth / 2,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width - newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);
        }
        
        //If new rooms are too small - break
        if(newRoom1.height <= _minimalRoomSize.y || newRoom2.height <= _minimalRoomSize.y)
        {
            int newWidth = (int)Mathf.Lerp(startRoom.RoomValue.width * _randomnessBoundaries.x, startRoom.RoomValue.width * _randomnessBoundaries.y, randomNumber);

            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                newWidth  + _wallWidth / 2,
                startRoom.RoomValue.height);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x + newWidth - _wallWidth / 2,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width - newWidth + _wallWidth / 2,
                startRoom.RoomValue.height);
            
            if (newRoom1.width <= _minimalRoomSize.x ||newRoom2.width <= _minimalRoomSize.x )
            {
                Rooms.Add(startRoom);
                return;
            }
            
        }
        
        if (newRoom1.width <= _minimalRoomSize.x ||newRoom2.width <= _minimalRoomSize.x )
        {
            int newHeight = (int)Mathf.Lerp(startRoom.RoomValue.height * _randomnessBoundaries.x, startRoom.RoomValue.height * _randomnessBoundaries.y, randomNumber);

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
        
        await Task.Delay(10);
        await RecursionSplit(roomNode1, !doHorizontalSplit);
        await RecursionSplit(roomNode2, !doHorizontalSplit);
    }

    private void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in Rooms) {
            vertices.Add(new Vertex((Vector2)room.RoomValue.position + (Vector2)room.RoomValue.size / 2));
        }

        _dTriangulation = DTriangulation.Triangulate(vertices);

        _edges = MinimumSpanningTree(_dTriangulation.Edges, _dTriangulation.Edges[0].A);
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
            if (_dTriangulation != null && _dTriangulation.Edges != null)
            {
                foreach (var edge in _dTriangulation.Edges)
                {
                    Debug.DrawLine(
                        new Vector3(edge.A.Position.x, -.1f, edge.A.Position.y),
                        new Vector3(edge.B.Position.x, -.1f, edge.B.Position.y), 
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
            }

            foreach (var edge in _edges)
            {
                Debug.DrawLine(
                    new Vector3(edge.A.Position.x, 0, edge.A.Position.y),
                    new Vector3(edge.B.Position.x, 0, edge.B.Position.y), 
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
            openSet.Add(edge.A);
            openSet.Add(edge.B);
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
                if (!closedSet.Contains(edge.A)) closedVertices++;
                if (!closedSet.Contains(edge.B)) closedVertices++;
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
            openSet.Remove(chosenEdge.A);
            openSet.Remove(chosenEdge.B);
            closedSet.Add(chosenEdge.A);
            closedSet.Add(chosenEdge.B);
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
    public RectInt WallValue;
    public RoomNode Room1;
    public RoomNode Room2;
}
