using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private RectInt _dungeon;
    [SerializeField] private Vector2Int _minimalRoomSize;
    [SerializeField] private int _wallWidth;

    private List<RectInt> _debugRooms = new();

    private void Start()
    {
        StartCoroutine(TraverseSpilt(_dungeon, false));
    }

    private IEnumerator TraverseSpilt(RectInt rawRoom, bool doHorizontalSplit)
    {
        RectInt newRoom1;
        RectInt newRoom2;

        if (doHorizontalSplit)
        {
            newRoom1 = new RectInt(
                rawRoom.x,
                rawRoom.y,
                rawRoom.width,
                rawRoom.height / 2  + _wallWidth / 2);

            newRoom2 = new RectInt(
                rawRoom.x,
                rawRoom.y + rawRoom.height / 2 - _wallWidth / 2,
                rawRoom.width,
                rawRoom.height / 2 + _wallWidth / 2);
        }
        else
        {
            newRoom1 = new RectInt(
                rawRoom.x,
                rawRoom.y,
                rawRoom.width / 2  + _wallWidth / 2,
                rawRoom.height);

            newRoom2 = new RectInt(
                rawRoom.x + rawRoom.width / 2 - _wallWidth / 2,
                rawRoom.y,
                rawRoom.width / 2 + _wallWidth / 2,
                rawRoom.height);
        }

        if (newRoom1.width < _minimalRoomSize.x || newRoom1.height < _minimalRoomSize.y)
            yield break;
        if(newRoom2.width < _minimalRoomSize.x || newRoom2.height < _minimalRoomSize.y)
            yield break;
        
        
        _debugRooms.Add(newRoom1);
        _debugRooms.Add(newRoom2);
        Debug.Log(newRoom1 + " " + newRoom2);
        yield return new WaitForSeconds(1f);
        StartCoroutine(TraverseSpilt(newRoom1, !doHorizontalSplit));
        StartCoroutine(TraverseSpilt(newRoom2, !doHorizontalSplit));

    }

    private void Update()
    {
        foreach (var room in _debugRooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow);
        }

        AlgorithmsUtils.DebugRectInt(_dungeon, Color.red);
    }
}

public class RoomNode
{
    public RectInt RoomValue;
    public RoomNode Room1;
    public RoomNode Room2;
}
