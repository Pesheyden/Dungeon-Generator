using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DelaunayTriangulation.Objects2D;
using DelaunayTriangulation;
using DungeonGeneration.Data;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;
using Random = System.Random;

public class DungeonGenerator : MonoBehaviour
{
    [Popup]
    [Tooltip("Settings config of the dungeon. To edit use ctrl + left click")]
    [SerializeField] private DungeonGenerationSettingsSo _settings;

    public List<RectInt> Rooms;
    public Graph Graph;

    private List<RectInt> _debugRoomsList;
    private Random _random;

    #region Debug

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugRooms() => DebugDrawingBatcher.ReversePauseGroup("Rooms");
    
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugGraph() => DebugDrawingBatcher.ReversePauseGroup("Graph");
    
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugDoors() => DebugDrawingBatcher.ReversePauseGroup("Doors");
    
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugFinalDungeon() => DebugDrawingBatcher.ReversePauseGroup("FinalDungeon");

    #endregion

    private void Start()
    {
        GenerateDungeon();
    }

    [Button]
    private async void GenerateDungeon()
    {
        ResetValues();
        DebugDrawingBatcher.BatchCall("Rooms", () =>
        {
            foreach (var room in _debugRoomsList)
            {
                AlgorithmsUtils.DebugRectInt(room, Color.yellow);
            }
        });
        await RecursionSplit(_settings.DungeonParameters, false);
        await FillGraph();
        
        DebugDrawingBatcher.BatchCall("FinalDungeon", () =>
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
                DebugExtension.DebugWireSphere(new Vector3(v.Vertex.Position.x, 0, v.Vertex.Position.y), Color.red);
                if(v is RoomGraphNode)
                    AlgorithmsUtils.DebugRectInt(v.Size, Color.red);
                else
                    AlgorithmsUtils.DebugRectInt(v.Size, Color.blue);
                foreach (GraphNode w in Graph.GetNeighbors(v))
                {
                    if (discovered.Add(w))
                        Q.Enqueue(w);
                    Debug.DrawLine(new Vector3(v.Vertex.Position.x, 0, v.Vertex.Position.y),
                        new Vector3(w.Vertex.Position.x, 0, w.Vertex.Position.y), Color.red);
                }
            }
        });
        Debug.Log(Time.realtimeSinceStartup);
    }

    private void ResetValues()
    {
        Rooms = new();
        _debugRoomsList = new();
        Graph = new Graph();
        _random = new Random(_settings.Seed);

        DebugDrawingBatcher.ClearCalls();
    }


    private async Task RecursionSplit(RectInt startRoom, bool doHorizontalSplit)
    {
        Stack<RectInt> roomsToSplit = new Stack<RectInt>();
        roomsToSplit.Push(startRoom);
        while (roomsToSplit.Count > 0)
        {
            //Create new  rooms
            var room = roomsToSplit.Pop();
            ValueTuple<RectInt, RectInt> newRooms;
            float randomNumber = (float)_random.NextDouble();
            if (doHorizontalSplit)
            {
                newRooms = HorizontalSplit(room, randomNumber);
            }
            else
            {
                newRooms = VerticalSplit(room, randomNumber);
            }

            //Check room sizes
            if (newRooms.Item1.height <= _settings.MinimalRoomSize.y || newRooms.Item2.height <= _settings.MinimalRoomSize.y)
            {
                newRooms = VerticalSplit(room, randomNumber);

                if (newRooms.Item1.width <= _settings.MinimalRoomSize.x || newRooms.Item2.width <= _settings.MinimalRoomSize.x)
                {
                    Rooms.Add(room);
                    continue;
                }
            }

            if (newRooms.Item1.width <= _settings.MinimalRoomSize.x || newRooms.Item2.width <= _settings.MinimalRoomSize.x)
            {
                newRooms = HorizontalSplit(room, randomNumber);

                if (newRooms.Item1.height <= _settings.MinimalRoomSize.y || newRooms.Item2.height <= _settings.MinimalRoomSize.y)
                {
                    Rooms.Add(room);
                    continue;
                }
            }

            //Debug
            _debugRoomsList.Add(newRooms.Item1);
            _debugRoomsList.Add(newRooms.Item2);
            _debugRoomsList.Remove(room);

            roomsToSplit.Push(newRooms.Item1);
            roomsToSplit.Push(newRooms.Item2);
            doHorizontalSplit = !doHorizontalSplit;

            await Task.Delay(_settings.RoomGenerationAwait);
        }

        (RectInt, RectInt) HorizontalSplit(RectInt roomNode, float f)
        {
            int newHeight = (int)Mathf.Lerp(roomNode.height * _settings.RandomnessBoundaries.x,
                roomNode.height * _settings.RandomnessBoundaries.y, f);

            var newRoom1 = new RectInt(
                roomNode.x,
                roomNode.y,
                roomNode.width,
                newHeight + _settings.WallWidth / 2);

            var newRoom2 = new RectInt(
                roomNode.x,
                roomNode.y + newHeight - _settings.WallWidth / 2,
                roomNode.width,
                roomNode.height - newHeight + _settings.WallWidth / 2);
            return (newRoom1, newRoom2);
        }

        (RectInt, RectInt) VerticalSplit(RectInt startRoom1, float randomNumber1)
        {
            int newWidth = (int)Mathf.Lerp(startRoom1.width * _settings.RandomnessBoundaries.x,
                startRoom1.width * _settings.RandomnessBoundaries.y, randomNumber1);

            var newRoom1 = new RectInt(
                startRoom1.x,
                startRoom1.y,
                newWidth + _settings.WallWidth / 2,
                startRoom1.height);

            var newRoom2 = new RectInt(
                startRoom1.x + newWidth - _settings.WallWidth / 2,
                startRoom1.y,
                startRoom1.width - newWidth + _settings.WallWidth / 2,
                startRoom1.height);
            return (newRoom1, newRoom2);
        }
    }

    private async Task FillGraph()
    {
        DebugDrawingBatcher.BatchCall("Graph", () =>
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
                DebugExtension.DebugWireSphere(new Vector3(v.Vertex.Position.x, 0, v.Vertex.Position.y), Color.blue);
                foreach (GraphNode w in Graph.GetNeighbors(v))
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                    }

                    Debug.DrawLine(new Vector3(v.Vertex.Position.x, 0, v.Vertex.Position.y),
                        new Vector3(w.Vertex.Position.x, 0, w.Vertex.Position.y), Color.blue);
                }
            }
        });
        
        //Fill graph with all created rooms
        List<Vertex> vertices = new List<Vertex>();
        foreach (var room in Rooms)
        {
            Vertex vertex = new Vertex((Vector2)room.position + (Vector2)room.size / 2);
            vertices.Add(vertex);

            RoomGraphNode roomGraphNode = new RoomGraphNode(vertex, room);
            Graph.AddNode(roomGraphNode);
            await Task.Delay(_settings.GraphGenerationAwait);
        }

        //Create graph edges(doors)
        var edgesTriangulation = DTriangulation.Triangulate(Graph);
        foreach (var edge in edgesTriangulation)
        {
            if(GenerateDoor(edge.A.Size, edge.B.Size, out var door))
                Graph.AddEdge(edge.A, edge.B, door);
            await Task.Delay(_settings.DoorsGenerationAwait);
        }

        //Remove some small rooms
        var roomNodes = Graph.GetNodes().Where(node => node is RoomGraphNode).ToList();
        roomNodes.Sort((a, b) => (a.Size.width * a.Size.height).CompareTo(b.Size.width * b.Size.height));


        for (int i = 0; i < roomNodes.Count * _settings.RoomsRemovePercentage / 100; i++)
        {
            if (!Graph.IsConnectedWhenRemoving(roomNodes[i]))
                continue;

            Graph.RemoveNode(roomNodes[i]);

            await Task.Delay(_settings.GraphFilteringAwait);
        }

        //Use MinimumSpanningTree
        var newEdges = MinimumSpanningTree.GetMinimumSpanningTree(Graph);
        var edges = Graph.GetNodes().Where(node => node is DoorGraphNode).ToList();

        foreach (var edge in newEdges)
        {
            edges.Remove(edge);
        }

        foreach (var edge in edges)
        {
            Graph.RemoveNode(edge);
            await Task.Delay(_settings.GraphFilteringAwait);
        }
    }

    private bool GenerateDoor(RectInt room1, RectInt room2, out DoorGraphNode doorGraphNode)
    {
        var intersect = AlgorithmsUtils.Intersect(room1, room2);

        float random = (float)_random.NextDouble();

        RectInt doorSize;
        if (intersect.width > intersect.height)
        {
            if (intersect.width - _settings.WallWidth * 2 < _settings.DoorSize.x)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = new RectInt
            (
                (int)Mathf.Clamp(
                    Mathf.Lerp(intersect.x + _settings.WallWidth, intersect.x + intersect.width - _settings.WallWidth * 2, random),
                    intersect.x + _settings.WallWidth, intersect.x + _settings.WallWidth),
                intersect.y,
                _settings.DoorSize.x,
                _settings.DoorSize.y
            );
        }
        else
        {
            if (intersect.height - _settings.WallWidth * 2 < _settings.DoorSize.y)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = new RectInt
            (
                intersect.x,
                (int)Mathf.Clamp(
                    Mathf.Lerp(intersect.y + _settings.WallWidth, intersect.y + intersect.height - _settings.WallWidth * 2, random),
                    intersect.y + _settings.WallWidth, intersect.y + intersect.height - _settings.WallWidth * 2),
                _settings.DoorSize.x,
                _settings.DoorSize.y
            );
        }

        Vertex vertex = new Vertex(new Vector2(doorSize.x + (float)doorSize.width / 2,
            doorSize.y + (float)doorSize.height / 2));
        DebugDrawingBatcher.BatchCall("Doors",() => { AlgorithmsUtils.DebugRectInt(doorSize, Color.blue); });

        doorGraphNode = new DoorGraphNode(vertex, doorSize);


        return true;
    }
    
}