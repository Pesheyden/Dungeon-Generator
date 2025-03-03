using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt _dungeon;
    [SerializeField] private Vector2Int _minimalRoomSize;
    [SerializeField] private int _wallWidth;

    public RoomNode RootNode;

    private List<RectInt> _debugRooms = new();
    private List<RectInt> _debugWalls = new();

    private void Awake()
    {
        //Create root node

        RootNode = new RoomNode(_dungeon);
    }

    private void Start()
    {
        StartCoroutine(TraverseSpilt(RootNode, false));
    }

    private IEnumerator TraverseSpilt(RoomNode startRoom, bool doHorizontalSplit)
    {
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
            yield break;
        if(newRoom2.width < _minimalRoomSize.x || newRoom2.height < _minimalRoomSize.y)
            yield break;
        
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
        _debugRooms.Add(newRoom1);
        _debugRooms.Add(newRoom2);
        _debugWalls.Add(newWall);
        Debug.Log("Rooms:" + newRoom1 + ":!:" + newRoom2 + "Wall: " + newWall);
        
        //Filling nodes

        RoomNode roomNode1 = new RoomNode(newRoom1);
        RoomNode roomNode2 = new RoomNode(newRoom2);

        startRoom.Room1 = roomNode1;
        startRoom.Room2 = roomNode2;
        startRoom.WallValue = newWall;
        
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(TraverseSpilt(roomNode1, !doHorizontalSplit));
        StartCoroutine(TraverseSpilt(roomNode2, !doHorizontalSplit));

    }

    private void Update()
    {
        foreach (var room in _debugRooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow);
        }
        foreach (var wall in _debugWalls)
        {
            AlgorithmsUtils.DebugRectInt(wall, Color.blue);
        }

        AlgorithmsUtils.DebugRectInt(_dungeon, Color.red);
    }
}

public class RoomNode
{
    public RoomNode(RectInt roomValue)
    {
        RoomValue = roomValue;
    }

    public RectInt RoomValue;
    public RectInt WallValue;
    public RoomNode Room1;
    public RoomNode Room2;
}
