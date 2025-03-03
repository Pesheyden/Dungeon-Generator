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
        StartCoroutine(TraverseSpilt(_dungeon, true));
    }

    private IEnumerator TraverseSpilt(RectInt rawRoom, bool doHorizontalSplit)
    {
        RectInt newRoom1;
        RectInt newRoom2;
        
        if (doHorizontalSplit)
        {
            if (rawRoom.width / 2 < _minimalRoomSize.x)
            {
                _debugRooms.Add(rawRoom);
                yield break;
            }

            newRoom1 = new RectInt(rawRoom.x, rawRoom.y, rawRoom.width, rawRoom.height + _wallWidth / 2);
            newRoom2 = new RectInt(rawRoom.x + rawRoom.width / 2 - _wallWidth / 2, rawRoom.y, 
                rawRoom.width, rawRoom.height + _wallWidth / 2);
        }
        else
        {
            if (rawRoom.height / 2 < _minimalRoomSize.y)
            {
                _debugRooms.Add(rawRoom);
                yield break;
            }
            newRoom1 = new RectInt(rawRoom.x, rawRoom.y, rawRoom.width / 2 + _wallWidth / 2, rawRoom.height);
            newRoom2 = new RectInt(rawRoom.x + rawRoom.width / 2 - _wallWidth / 2, rawRoom.y, 
                rawRoom.width / 2 + _wallWidth / 2, rawRoom.height);
        }
        yield return new WaitForSeconds(1f);
        _debugRooms.Add(rawRoom);
        StartCoroutine(TraverseSpilt(newRoom1, !doHorizontalSplit));
        StartCoroutine(TraverseSpilt(newRoom2, !doHorizontalSplit));

    }

    private void Update()
    {
        foreach (var room in _debugRooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow);
        }
    }
}
