using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt _dungeon;
    [SerializeField] private Vector2Int _minimalRoomSize;
    [SerializeField] private int _wallWidth;

    [Header("Debug")] 
    [SerializeField] private bool _debugRooms;
    [SerializeField] private bool _debugWalls;

    public RoomNode RootNode;

    private List<RectInt> _debugRoomsList = new();
    private List<RectInt> _debugWallsList = new();

    private int _roomsAmount = 0; 

    private void Awake()
    {
        //Create root node
        RootNode = new RoomNode(_dungeon);
    }

    private void Start()
    {
        TraverseSpilt(RootNode, false);
    }

    private async Task TraverseSpilt(RoomNode startRoom, bool doHorizontalSplit)
    {
        _roomsAmount++;
        
        
        RectInt newRoom1;
        RectInt newRoom2;

        //Create new raw rooms
        if (doHorizontalSplit)
        {
            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width,
                startRoom.RoomValue.height / 2  + _wallWidth / 2);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y + startRoom.RoomValue.height / 2 - _wallWidth / 2,
                startRoom.RoomValue.width,
                startRoom.RoomValue.height / 2 + _wallWidth / 2);
        }
        else
        {
            newRoom1 = new RectInt(
                startRoom.RoomValue.x,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width / 2  + _wallWidth / 2,
                startRoom.RoomValue.height);

            newRoom2 = new RectInt(
                startRoom.RoomValue.x + startRoom.RoomValue.width / 2 - _wallWidth / 2,
                startRoom.RoomValue.y,
                startRoom.RoomValue.width / 2 + _wallWidth / 2,
                startRoom.RoomValue.height);
        }

        //If new rooms are too small - break
        if (newRoom1.width < _minimalRoomSize.x || newRoom1.height < _minimalRoomSize.y)
        {
            return;
        }

        if(newRoom2.width < _minimalRoomSize.x || newRoom2.height < _minimalRoomSize.y)
        {
            return;
        }
        
        //Extract wall
        RectInt newWall = AlgorithmsUtils.Intersect(newRoom1, newRoom2);
        
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
        }
        
        //Debug
        _debugRoomsList.Add(newRoom1);
        _debugRoomsList.Add(newRoom2);
        _debugWallsList.Add(newWall);
        
        //Filling nodes

        RoomNode roomNode1 = new RoomNode(newRoom1);
        RoomNode roomNode2 = new RoomNode(newRoom2);

        startRoom.Room1 = roomNode1;
        startRoom.Room2 = roomNode2;
        startRoom.WallValue = newWall;
        
        //Debug
        _debugRoomsList.Add(newRoom1);
        _debugRoomsList.Add(newRoom2);
        _debugWallsList.Add(newWall);
        
        await Task.Delay(100);
        await TraverseSpilt(roomNode1, !doHorizontalSplit);
        await TraverseSpilt(roomNode2, !doHorizontalSplit);
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

        AlgorithmsUtils.DebugRectInt(_dungeon, Color.red);
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
