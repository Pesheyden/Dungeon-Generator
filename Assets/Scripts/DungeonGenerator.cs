using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonGeneration.Data;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Utilities;
using Random = System.Random;

/// <summary>
/// Main class responsible for procedural dungeon generation using BSP (Binary Space Partitioning).
/// Handles room splitting, door placement, and graph construction for pathfinding and connectivity.
/// </summary>
public class DungeonGenerator : Singleton<DungeonGenerator>
{
    /// <summary>
    /// Configuration settings for dungeon generation.
    /// </summary>
    [Popup] [Tooltip("Settings config of the dungeon. To edit use ctrl + left click")]
    public DungeonGenerationSettingsSo Settings;

    /// <summary>
    /// List of all rooms currently in the dungeon.
    /// </summary>
    public List<RoomNode> Rooms;

    /// <summary>
    /// Graph structure representing room connections via doors.
    /// </summary>
    public Graph Graph;

    /// <summary>
    /// Unity event triggered once dungeon generation is completed.
    /// </summary>
    public UnityEvent OnDungeonCreated;

    private Random _random;
    private float _starTime;

    #region DebugButtons

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

    /// <summary>
    /// Begins the asynchronous process of dungeon generation.
    /// </summary>
    [Button]
    private async void GenerateDungeon()
    {
        _starTime = Time.realtimeSinceStartup;
        ResetValues();

        BatchRoomsDebug();
        BatchDoorsDebug();

        await SplitDungeon(Settings.DungeonParameters, false);
        Debug.Log($"Room split: {Time.realtimeSinceStartup - _starTime}");
        
        await CreateGraph();
        BatchFinalDungeonDebug();

        OnDungeonCreated.Invoke();
        Debug.Log($"Full dungeon generation: {Time.realtimeSinceStartup - _starTime}");
    }

    #region BSP

    /// <summary>
    /// Splits the dungeon recursively into rooms using BSP logic.
    /// </summary>
    private async Task SplitDungeon(RectInt startRoomDimensions, bool doHorizontalSplit)
    {
        Stack<RoomNode> roomsToSplit = new();
        RoomNode startRoom = new(startRoomDimensions);
        roomsToSplit.Push(startRoom);
        Rooms.Add(startRoom);

        while (roomsToSplit.Count > 0)
        {
            var room = roomsToSplit.Pop();
            if (!DoSplit(doHorizontalSplit, room, out var newRooms))
                continue;

            RoomNode newRoom1 = new(newRooms.Item1);
            RoomNode newRoom2 = new(newRooms.Item2);

            // Attempt to place a connecting door between the two new rooms
            if (GenerateDoor(newRoom1, newRoom2, out DoorNode doorNode))
            {
                newRoom1.DoorNodes.Add(doorNode);
                newRoom2.DoorNodes.Add(doorNode);
            }

            DetermineConnections(room, newRoom1, newRoom2);

            Rooms.Remove(room);
            roomsToSplit.Push(newRoom1);
            roomsToSplit.Push(newRoom2);
            Rooms.Add(newRoom1);
            Rooms.Add(newRoom2);
            doHorizontalSplit = !doHorizontalSplit;

            await Task.Delay(Settings.RoomGenerationAwait, Application.exitCancellationToken);
        }
    }

    /// <summary>
    /// Attempts to split a room either horizontally or vertically based on a random value.
    /// </summary>
    private bool DoSplit(bool doHorizontalSplit, RoomNode room, out (RectInt, RectInt) newRooms)
    {
        float randomNumber = (float)_random.NextDouble();
        if (doHorizontalSplit)
        {
            newRooms = HorizontalSplit(room.Dimensions, randomNumber);
        }
        else
        {
            newRooms = VerticalSplit(room.Dimensions, randomNumber);
        }

        // Ensure rooms meet the minimum size criteria, fallback to alternate splits as needed
        if (newRooms.Item1.height <= Settings.MinimalRoomSize.y || newRooms.Item2.height <= Settings.MinimalRoomSize.y)
        {
            newRooms = VerticalSplit(room.Dimensions, randomNumber);
            if (newRooms.Item1.width <= Settings.MinimalRoomSize.x || newRooms.Item2.width <= Settings.MinimalRoomSize.x)
                return false;
        }

        if (newRooms.Item1.width <= Settings.MinimalRoomSize.x || newRooms.Item2.width <= Settings.MinimalRoomSize.x)
        {
            newRooms = HorizontalSplit(room.Dimensions, randomNumber);
            if (newRooms.Item1.height <= Settings.MinimalRoomSize.y || newRooms.Item2.height <= Settings.MinimalRoomSize.y)
                return false;
        }

        return true;

        // Perform horizontal split on the room
        (RectInt, RectInt) HorizontalSplit(RectInt roomNode, float f)
        {
            int newHeight = (int)Mathf.Lerp(roomNode.height * Settings.RandomnessBoundaries.x,
                                            roomNode.height * Settings.RandomnessBoundaries.y, f);

            var newRoom1 = new RectInt(roomNode.x, roomNode.y, roomNode.width, newHeight + Settings.WallWidth / 2);
            var newRoom2 = new RectInt(roomNode.x, roomNode.y + newHeight - Settings.WallWidth / 2,
                                       roomNode.width, roomNode.height - newHeight + Settings.WallWidth / 2);

            if (Settings.WallWidth % 2 != 0)
                newRoom1.height += 1;

            return (newRoom1, newRoom2);
        }

        // Perform vertical split on the room
        (RectInt, RectInt) VerticalSplit(RectInt startRoom1, float randomNumber1)
        {
            int newWidth = (int)Mathf.Lerp(startRoom1.width * Settings.RandomnessBoundaries.x,
                                           startRoom1.width * Settings.RandomnessBoundaries.y, randomNumber1);

            var newRoom1 = new RectInt(startRoom1.x, startRoom1.y, newWidth + Settings.WallWidth / 2, startRoom1.height);
            var newRoom2 = new RectInt(startRoom1.x + newWidth - Settings.WallWidth / 2, startRoom1.y,
                                       startRoom1.width - newWidth + Settings.WallWidth / 2, startRoom1.height);

            if (Settings.WallWidth % 2 != 0)
                newRoom1.width += 1;

            return (newRoom1, newRoom2);
        }
    }

    /// <summary>
    /// Transfers and recalculates connections from a parent room to its split children.
    /// </summary>
    private void DetermineConnections(RoomNode room, RoomNode newRoom1, RoomNode newRoom2)
    {
        foreach (var door in room.DoorNodes)
        {
            RoomNode connectedRoom = door.GetOtherRoom(room);

            if (AlgorithmsUtils.Intersects(connectedRoom.Dimensions, newRoom1.Dimensions))
            {
                if (GenerateDoor(connectedRoom, newRoom1, out var newDoor))
                {
                    connectedRoom.DoorNodes.Add(newDoor);
                    newRoom1.DoorNodes.Add(newDoor);
                }
            }

            if (AlgorithmsUtils.Intersects(connectedRoom.Dimensions, newRoom2.Dimensions))
            {
                if (GenerateDoor(connectedRoom, newRoom2, out var newDoor))
                {
                    connectedRoom.DoorNodes.Add(newDoor);
                    newRoom2.DoorNodes.Add(newDoor);
                }
            }

            connectedRoom.DoorNodes.Remove(door);
        }
    }

    #endregion

    /// <summary>
    /// Creates a connectivity graph from the current room and door layout.
    /// </summary>
    private async Task CreateGraph()
    {
        BatchDrawGraph();
        await RemoveSmallRooms();
        await FillGraphWithAllRooms();
    }

    /// <summary>
    /// Removes the smallest percentage of rooms based on settings to improve dungeon flow.
    /// </summary>
    private async Task RemoveSmallRooms()
    {
        var roomNodes = new List<RoomNode>(Rooms);
        roomNodes.Sort((a, b) =>
            (a.Dimensions.width * a.Dimensions.height).CompareTo(b.Dimensions.width * b.Dimensions.height));

        for (int i = 0; i < roomNodes.Count * Settings.RoomsRemovePercentage / 100; i++)
        {
            if (!roomNodes[i].CanBeRemovedWithoutConnectionsSeparation(roomNodes))
                continue;

            roomNodes[i].ClearConnections();
            roomNodes.RemoveAt(i);

            await Task.Delay(Settings.GraphFilteringAwait, Application.exitCancellationToken);
        }

        Rooms = roomNodes;
    }

    /// <summary>
    /// Connects all rooms in the graph using their door relationships.
    /// </summary>
    private async Task FillGraphWithAllRooms()
    {
        List<RoomNode> roomsToConnect = new(Rooms);
        HashSet<RoomNode> discovered = new();
        List<RoomNode> discoveredWithChildren = new();

        var room = roomsToConnect[0];
        discovered.Add(room);
        discoveredWithChildren.Add(room);

        while (roomsToConnect.Count != discovered.Count)
        {
            Start:
            foreach (var door in room.DoorNodes)
            {
                var connectedRoom = door.GetOtherRoom(room);
                if (discovered.Add(connectedRoom))
                {
                    discoveredWithChildren.Add(connectedRoom);
                    Graph.AddEdge(room, connectedRoom, door);
                    room = connectedRoom;
                    goto Start;
                }
            }

            await Task.Delay(Settings.GraphGenerationAwait, Application.exitCancellationToken);
            discoveredWithChildren.Remove(room);
            room = discoveredWithChildren[0];
        }
    }

    /// <summary>
    /// Tries to generate a door between two rooms if they intersect.
    /// </summary>
    private bool GenerateDoor(RoomNode room1, RoomNode room2, out DoorNode doorGraphNode)
    {
        var intersect = AlgorithmsUtils.Intersect(room1.Dimensions, room2.Dimensions);
        float random = (float)_random.NextDouble();
        RectInt doorSize;

        if (intersect.width > intersect.height)
        {
            if (intersect.width - Settings.WallWidth * 2 < Settings.DoorSize.x)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = CreateHorizontalDoor(intersect, random);
        }
        else
        {
            if (intersect.height - Settings.WallWidth * 2 < Settings.DoorSize.y)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = CreateVerticalDoor(intersect, random);
        }

        doorGraphNode = new DoorNode(doorSize);
        doorGraphNode.ConnectedRooms[0] = room1;
        doorGraphNode.ConnectedRooms[1] = room2;

        return true;

        RectInt CreateHorizontalDoor(RectInt rectInt, float f)
        {
            return new RectInt(
                (int)Mathf.Clamp(
                    Mathf.Lerp(rectInt.x + Settings.WallWidth, rectInt.x + rectInt.width - Settings.WallWidth * 2, f),
                    rectInt.x + Settings.WallWidth, rectInt.x + rectInt.width - Settings.WallWidth * 2),
                rectInt.y,
                Settings.DoorSize.x,
                Settings.DoorSize.y
            );
        }

        RectInt CreateVerticalDoor(RectInt i, float random1)
        {
            return new RectInt(
                i.x,
                (int)Mathf.Clamp(
                    Mathf.Lerp(i.y + Settings.WallWidth, i.y + i.height - Settings.WallWidth * 2, random1),
                    i.y + Settings.WallWidth, i.y + i.height - Settings.WallWidth * 2),
                Settings.DoorSize.x,
                Settings.DoorSize.y
            );
        }
    }

    /// <summary>
    /// Resets the internal state before generation.
    /// </summary>
    private void ResetValues()
    {
        Rooms = new();
        Graph = new Graph();
        _random = new Random(Settings.Seed);
        DebugDrawingBatcher.ClearCalls();
    }

    #region Debugging

    private void BatchDoorsDebug()
    {
        DebugDrawingBatcher.BatchCall("Doors", () =>
        {
            foreach (var room in Rooms)
                foreach (var door in room.DoorNodes)
                    AlgorithmsUtils.DebugRectInt(door.Dimensions, Color.blue, 0.1f);
        });
    }

    private void BatchRoomsDebug()
    {
        DebugDrawingBatcher.BatchCall("Rooms", () =>
        {
            foreach (var room in Rooms)
                AlgorithmsUtils.DebugRectInt(room.Dimensions, Color.yellow);
        });
    }

    private void BatchDrawGraph()
    {
        DebugDrawingBatcher.BatchCall("Graph", () =>
        {
            var rooms = Graph.GetRooms();
            var doors = Graph.GetDoors();
            foreach (var room in rooms)
                DebugExtension.DebugWireSphere(room.GetCenter(), Color.blue);

            foreach (var door in doors)
            {
                AlgorithmsUtils.DebugRectInt(door.Dimensions, Color.blue);
                DebugExtension.DebugWireSphere(door.GetCenter(), Color.blue);
                foreach (var room in door.ConnectedRooms)
                    Debug.DrawLine(room.GetCenter(), door.GetCenter(), Color.blue);
            }
        });
    }

    private void BatchFinalDungeonDebug()
    {
        DebugDrawingBatcher.BatchCall("FinalDungeon", () =>
        {
            var rooms = Graph.GetRooms();
            var doors = Graph.GetDoors();
            foreach (var room in rooms)
            {
                AlgorithmsUtils.DebugRectInt(room.Dimensions, Color.yellow);
                DebugExtension.DebugWireSphere(room.GetCenter(), Color.green);
            }

            foreach (var door in doors)
            {
                AlgorithmsUtils.DebugRectInt(door.Dimensions, Color.blue);
                DebugExtension.DebugWireSphere(door.GetCenter(), Color.green);
                foreach (var room in door.ConnectedRooms)
                    Debug.DrawLine(room.GetCenter(), door.GetCenter(), Color.green);
            }
        });
    }

    #endregion
}
