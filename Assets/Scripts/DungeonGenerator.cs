using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonGeneration.Data;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;
using Random = System.Random;

public class DungeonGenerator : MonoBehaviour
{
    [Popup] [Tooltip("Settings config of the dungeon. To edit use ctrl + left click")] [SerializeField]
    private DungeonGenerationSettingsSo _settings;

    public List<RoomNode> Rooms;
    public Graph Graph;

    private Random _random;

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

    [Button]
    private async void GenerateDungeon()
    {
        ResetValues();

        BatchRoomsDebug();
        BatchDoorsDebug();

        await SplitDungeon(_settings.DungeonParameters, false);
        await CreateGraph();

        BatchFinalDungeonDebug();
        Debug.Log(Time.realtimeSinceStartup);
    }

    #region BSP

    private async Task SplitDungeon(RectInt startRoomDimensions, bool doHorizontalSplit)
    {
        Stack<RoomNode> roomsToSplit = new Stack<RoomNode>();
        RoomNode startRoom = new RoomNode(startRoomDimensions);
        roomsToSplit.Push(startRoom);
        Rooms.Add(startRoom);

        while (roomsToSplit.Count > 0)
        {
            //Create new rooms
            var room = roomsToSplit.Pop();
            if (!DoSplit(doHorizontalSplit, room, out var newRooms))
                continue;

            RoomNode newRoom1 = new RoomNode(newRooms.Item1);
            RoomNode newRoom2 = new RoomNode(newRooms.Item2);


            //Generate connections 
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

            await Task.Delay(_settings.RoomGenerationAwait);
        }
    }

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

        //Check room sizes
        if (newRooms.Item1.height <= _settings.MinimalRoomSize.y ||
            newRooms.Item2.height <= _settings.MinimalRoomSize.y)
        {
            newRooms = VerticalSplit(room.Dimensions, randomNumber);

            if (newRooms.Item1.width <= _settings.MinimalRoomSize.x ||
                newRooms.Item2.width <= _settings.MinimalRoomSize.x)
            {
                return false;
            }
        }

        if (newRooms.Item1.width <= _settings.MinimalRoomSize.x ||
            newRooms.Item2.width <= _settings.MinimalRoomSize.x)
        {
            newRooms = HorizontalSplit(room.Dimensions, randomNumber);

            if (newRooms.Item1.height <= _settings.MinimalRoomSize.y ||
                newRooms.Item2.height <= _settings.MinimalRoomSize.y)
            {
                return false;
            }
        }

        return true;

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

    private async Task CreateGraph()
    {
        BatchDrawGraph();

        await RemoveSmallRooms();

        await FillGraphWithAllRooms();
    }


    private async Task RemoveSmallRooms()
    {
        var roomNodes = new List<RoomNode>(Rooms);
        roomNodes.Sort((a, b) =>
            (a.Dimensions.width * a.Dimensions.height).CompareTo(b.Dimensions.width * b.Dimensions.height));

        for (int i = 0; i < roomNodes.Count * _settings.RoomsRemovePercentage / 100; i++)
        {
            if (!roomNodes[i].CanBeRemovedWithoutConnectionsSeparation(roomNodes))
                continue;

            roomNodes[i].ClearConnections();
            roomNodes.RemoveAt(i);

            await Task.Delay(_settings.GraphFilteringAwait);
        }

        Rooms = roomNodes;
    }

    private async Task FillGraphWithAllRooms()
    {
        List<RoomNode> roomsToConnect = new List<RoomNode>(Rooms);
        HashSet<RoomNode> discovered = new HashSet<RoomNode>();
        List<RoomNode> discoveredWithChildren = new List<RoomNode>();

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

            await Task.Delay(_settings.GraphGenerationAwait);
            discoveredWithChildren.Remove(room);
            room = discoveredWithChildren[0];
        }
    }

    private bool GenerateDoor(RoomNode room1, RoomNode room2, out DoorNode doorGraphNode)
    {
        var intersect = AlgorithmsUtils.Intersect(room1.Dimensions, room2.Dimensions);
        float random = (float)_random.NextDouble();
        RectInt doorSize;

        //Check the intersection orientation and then create a door
        if (intersect.width > intersect.height)
        {
            if (intersect.width - _settings.WallWidth * 2 < _settings.DoorSize.x)
            {
                doorGraphNode = null;
                return false;
            }

            doorSize = CreateHorizontalDoor(intersect, random);
        }
        else
        {
            if (intersect.height - _settings.WallWidth * 2 < _settings.DoorSize.y)
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
            doorSize = new RectInt
            (
                (int)Mathf.Clamp(
                    Mathf.Lerp(rectInt.x + _settings.WallWidth,
                        rectInt.x + rectInt.width - _settings.WallWidth * 2, f),
                    rectInt.x + _settings.WallWidth, rectInt.x + _settings.WallWidth),
                rectInt.y,
                _settings.DoorSize.x,
                _settings.DoorSize.y
            );
            return doorSize;
        }

        RectInt CreateVerticalDoor(RectInt i, float random1)
        {
            doorSize = new RectInt
            (
                i.x,
                (int)Mathf.Clamp(
                    Mathf.Lerp(i.y + _settings.WallWidth,
                        i.y + i.height - _settings.WallWidth * 2, random1),
                    i.y + _settings.WallWidth, i.y + i.height - _settings.WallWidth * 2),
                _settings.DoorSize.x,
                _settings.DoorSize.y
            );
            return doorSize;
        }
    }

    private void ResetValues()
    {
        Rooms = new();
        Graph = new Graph();
        _random = new Random(_settings.Seed);

        DebugDrawingBatcher.ClearCalls();
    }

    #region Debugging

    private void BatchDoorsDebug()
    {
        DebugDrawingBatcher.BatchCall("Doors", () =>
        {
            foreach (var room in Rooms)
            {
                foreach (var door in room.DoorNodes)
                {
                    AlgorithmsUtils.DebugRectInt(door.Dimensions, Color.blue, 0.1f);
                }
            }
        });
    }

    private void BatchRoomsDebug()
    {
        DebugDrawingBatcher.BatchCall("Rooms", () =>
        {
            foreach (var room in Rooms)
            {
                AlgorithmsUtils.DebugRectInt(room.Dimensions, Color.yellow);
            }
        });
    }

    private void BatchDrawGraph()
    {
        DebugDrawingBatcher.BatchCall("Graph", () =>
        {
            var rooms = Graph.GetRooms();
            var doors = Graph.GetDoors();
            foreach (var room in rooms)
            {
                DebugExtension.DebugWireSphere(room.GetCenter(), Color.blue);
            }

            foreach (var door in doors)
            {
                AlgorithmsUtils.DebugRectInt(door.Dimensions, Color.blue);
                DebugExtension.DebugWireSphere(door.GetCenter(), Color.blue);
                foreach (var room in door.ConnectedRooms)
                {
                    Debug.DrawLine(room.GetCenter(), door.GetCenter(), Color.blue);
                }
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
                {
                    Debug.DrawLine(room.GetCenter(), door.GetCenter(), Color.green);
                }
            }
        });
    }

    #endregion
}