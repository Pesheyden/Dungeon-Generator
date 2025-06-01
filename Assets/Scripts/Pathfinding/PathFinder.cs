using System;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Graph;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Types of pathfinding algorithms supported.
/// </summary>
public enum PathFindingType
{
    /// <summary>
    /// Standard A* pathfinding on a per-tile basis.
    /// </summary>
    AStar,

    /// <summary>
    /// Hierarchical pathfinding using groups (e.g., rooms) and then A* within groups.
    /// </summary>
    GroupedAStar
}

/// <summary>
/// Provides static methods for initializing a pathfinding map and performing pathfinding
/// between points across a dungeon graph.
/// </summary>
public static class PathFinder
{
    /// <summary>
    /// Global pathfinding map instance.
    /// </summary>
    public static PathFindingMap PathFindingMap;

    /// <summary>
    /// Mapping of room indices per tile location (z, x).
    /// </summary>
    public static int[,] RoomTileMap;

    private static List<PathFindingTile> _debugDiscoveredPoints = new List<PathFindingTile>();

    #region Initialization

    /// <summary>
    /// Initializes the pathfinding system with the given tile map, room map, and graph.
    /// </summary>
    /// <param name="tileMap">2D array representing tile walkability.</param>
    /// <param name="roomTileMap">2D array mapping tiles to room indices.</param>
    /// <param name="graph">Dungeon graph containing room and connectivity data.</param>
    public static void Initialize(int[,] tileMap, int[,] roomTileMap, Graph graph)
    {
        RoomTileMap = roomTileMap;
        CreatePathFindingMap(tileMap, roomTileMap, graph);
        BatchDiscoveredPointDebug();
    }

    /// <summary>
    /// Builds the internal group-based and tile-based map structure.
    /// </summary>
    private static void CreatePathFindingMap(int[,] tileMap, int[,] roomTileMap, Graph graph)
    {
        PathFindingMap = new PathFindingMap();
        var rooms = graph.GetRooms();

        foreach (var room in rooms)
        {
            PathFindingMap.Groups.Add(new PathFindingGroup(room.GetCenter(), room));
        }

        for (int z = 0; z < tileMap.GetLength(0); z++)
        {
            for (int x = 0; x < tileMap.GetLength(1); x++)
            {
                var newTile = new PathFindingTile(
                    new Vector3(x + 0.5f, PathFindingMap.Groups[roomTileMap[z, x]].Position.y, z + 0.5f),
                    tileMap[z, x] == 0);
                PathFindingMap.Groups[roomTileMap[z, x]].Tiles.Add(newTile);
            }
        }

        for (int i = 0; i < PathFindingMap.Groups.Count; i++)
        {
            foreach (var connectedRoom in rooms[i].GetConnectedRooms())
            {
                PathFindingMap.Groups[i].ConnectedGroups.Add(PathFindingMap.GetGroupByPosition(connectedRoom.GetCenter()));
            }

            foreach (var tile in PathFindingMap.Groups[i].Tiles)
            {
                TryAddTileConnections(tile, tile.Position + Vector3.back);
                TryAddTileConnections(tile, tile.Position + Vector3.forward);
                TryAddTileConnections(tile, tile.Position + Vector3.left);
                TryAddTileConnections(tile, tile.Position + Vector3.right);
                TryAddTileConnections(tile, tile.Position + Vector3.back + Vector3.right);
                TryAddTileConnections(tile, tile.Position + Vector3.forward + Vector3.right);
                TryAddTileConnections(tile, tile.Position + Vector3.left + Vector3.back);
                TryAddTileConnections(tile, tile.Position + Vector3.right + Vector3.forward);
            }
        }

        BatchMapDebug();
    }

    /// <summary>
    /// Tries to connect the given tile to a neighbor if valid.
    /// </summary>
    private static void TryAddTileConnections(PathFindingTile tile, Vector3 target)
    {
        if (RoomTileMap.GetLength(0) <= target.z || RoomTileMap.GetLength(1) <= target.x || target.x < 0 || target.z < 0)
            return;

        if (PathFindingMap.Groups[RoomTileMap[(int)(target.z - .5f), (int)(target.x - .5f)]]
            .TryGetTileByPosition(target, out var connectedTile))
        {
            connectedTile.ConnectedTiles.Add(tile);
            tile.ConnectedTiles.Add(connectedTile);
        }
    }

    /// <summary>
    /// Queues debug drawing of tile and group connections.
    /// </summary>
    private static void BatchMapDebug()
    {
        DebugDrawingBatcher.BatchCall("PathFindingGraph", () =>
        {
            foreach (var group in PathFindingMap.Groups)
            {
                DebugExtension.DebugWireSphere(group.Position + Vector3.up, Color.gray);
                foreach (var connectedGroup in group.ConnectedGroups)
                {
                    Debug.DrawLine(group.Position + Vector3.up, connectedGroup.Position + Vector3.up, Color.gray);
                }

                foreach (var tile in group.Tiles)
                {
                    DebugExtension.DebugWireSphere(tile.Position, tile.Walkable ? Color.cyan : Color.black, 0.2f);
                    foreach (var connectedTile in tile.ConnectedTiles)
                    {
                        Debug.DrawLine(tile.Position, connectedTile.Position, tile.Walkable && connectedTile.Walkable ? Color.cyan : Color.black);
                    }
                }
            }
        });
    }

    /// <summary>
    /// Queues debug drawing of tiles discovered during last path search.
    /// </summary>
    private static void BatchDiscoveredPointDebug()
    {
        DebugDrawingBatcher.BatchCall("PathFindingDiscoveredPoints", () =>
        {
            foreach (var point in _debugDiscoveredPoints)
            {
                DebugExtension.DebugWireSphere(point.Position, Color.magenta, 0.25f);
            }
        });
    }

    #endregion

    #region PathFinding

    /// <summary>
    /// Finds a path between two world positions using the specified algorithm.
    /// </summary>
    /// <param name="start">Start world position.</param>
    /// <param name="end">Target world position.</param>
    /// <param name="pathFindingType">Pathfinding strategy to use.</param>
    /// <returns>List of waypoints (world positions) from start to end, or empty list if no path.</returns>
    public static List<Vector3> FindPath(Vector3 start, Vector3 end, PathFindingType pathFindingType)
    {
        List<Vector3> result = new List<Vector3>();
        _debugDiscoveredPoints.Clear();

        switch (pathFindingType)
        {
            case PathFindingType.AStar:
                result = AStar(start, end);
                break;
            case PathFindingType.GroupedAStar:
                result = GroupedAStar(start, end);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pathFindingType), pathFindingType, null);
        }

        return result;
    }

    /// <summary>
    /// Runs A* on all tiles between two world positions.
    /// </summary>
    private static List<Vector3> AStar(Vector3 start, Vector3 end)
    {
        if (!PathFindingMap.Groups[RoomTileMap[(int)(start.z - .5f), (int)(start.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(start), out var startTile))
            return null;

        if (!PathFindingMap.Groups[RoomTileMap[(int)(end.z - .5f), (int)(end.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(end), out var endTile))
            return null;

        return FindPath(startTile, endTile);
    }

    /// <summary>
    /// Runs group-level pathfinding followed by A* between segments.
    /// </summary>
    private static List<Vector3> GroupedAStar(Vector3 start, Vector3 end)
    {
        List<Vector3> result = new List<Vector3>();

        if (!PathFindingMap.Groups[RoomTileMap[(int)(start.z - .5f), (int)(start.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(start), out var startTile))
            return null;

        if (!PathFindingMap.Groups[RoomTileMap[(int)(end.z - .5f), (int)(end.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(end), out var endTile))
            return null;

        var groupPath = FindGroupPath(startTile, endTile);

        PathFindingTile lastTile = groupPath[0];

        for (var index = 1; index < groupPath.Count; index++)
        {
            var point = groupPath[index];
            result.AddRange(FindPath(lastTile, point));
            lastTile = point;
        }

        return result;
    }

    #region GroupPath

    /// <summary>
    /// Runs a simple A*/Dijkstra-like algorithm to get the group (room) traversal path.
    /// </summary>
    private static List<PathFindingTile> FindGroupPath(PathFindingTile start, PathFindingTile end)
    {
        PathFindingGroup startGroup = PathFindingMap.Groups[RoomTileMap[(int)(start.Position.z - .5f), (int)(start.Position.x - .5f)]];
        PathFindingGroup endGroup = PathFindingMap.Groups[RoomTileMap[(int)(end.Position.z - .5f), (int)(end.Position.x - .5f)]];

        HashSet<PathFindingGroup> discovered = new();
        List<(PathFindingGroup node, float priority)> q = new();
        Dictionary<PathFindingGroup, PathFindingGroup> p = new();
        Dictionary<PathFindingGroup, float> c = new();

        q.Add((startGroup, 0));

        while (q.Count > 0)
        {
            var v = q[^1];
            q.RemoveAt(q.Count - 1);

            if (v.node == endGroup)
                return ReconstructGroupPath(p, start, end, startGroup, endGroup);

            foreach (var w in v.node.ConnectedGroups)
            {
                if (!discovered.Add(w))
                    continue;

                float newCost = v.priority + Cost(v.node.Position, w.Position);
                if (!c.ContainsKey(w))
                {
                    c.Add(w, newCost);
                    p.Add(w, v.node);
                }
                else if (c[w] > newCost)
                {
                    c[w] = newCost;
                    p[w] = v.node;
                }

                q.Add((w, c[w] + Heuristic(w.Position, end.Position)));
                q = q.OrderByDescending(n => n.priority).ToList();
            }
        }

        return new List<PathFindingTile>();
    }

    /// <summary>
    /// Reconstructs group-level path from A*/Dijkstra result.
    /// </summary>
    private static List<PathFindingTile> ReconstructGroupPath(
        Dictionary<PathFindingGroup, PathFindingGroup> parentMap,
        PathFindingTile start, PathFindingTile end,
        PathFindingGroup startGroup, PathFindingGroup endGroup)
    {
        List<PathFindingTile> path = new();
        Dictionary<PathFindingTile, PathFindingTile> tileParentMap = new();

        PathFindingTile currentNode = end;
        PathFindingTile lastTile = end;
        PathFindingGroup lastGroup = endGroup;

        while (lastGroup != startGroup)
        {
            var group = parentMap[lastGroup];
            var tile = GetDoorTileConnectingGroups(group, lastGroup);
            tileParentMap[lastTile] = tile;
            lastGroup = group;
            lastTile = tile;
        }

        tileParentMap[lastTile] = start;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = tileParentMap[currentNode];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    #endregion

    #region FullPath

    /// <summary>
    /// Runs standard A* between two given tiles.
    /// </summary>
    private static List<Vector3> FindPath(PathFindingTile start, PathFindingTile end)
    {
        HashSet<PathFindingTile> discovered = new();
        List<(PathFindingTile node, float priority)> q = new();
        Dictionary<PathFindingTile, PathFindingTile> p = new();
        Dictionary<PathFindingTile, float> c = new();

        q.Add((start, 0));

        while (q.Count > 0)
        {
            var v = q[^1];
            q.RemoveAt(q.Count - 1);

            if (v.node == end)
            {
                _debugDiscoveredPoints.AddRange(discovered);
                return ReconstructPathPart(p, start, end);
            }

            if (!v.node.Walkable)
                continue;

            foreach (var w in v.node.ConnectedTiles)
            {
                if (!discovered.Add(w))
                    continue;

                float newCost = v.priority + Cost(v.node.Position, w.Position);
                if (!c.ContainsKey(w))
                {
                    c[w] = newCost;
                    p[w] = v.node;
                }
                else if (c[w] > newCost)
                {
                    c[w] = newCost;
                    p[w] = v.node;
                }

                q.Add((w, c[w] + Heuristic(w.Position, end.Position)));
                q = q.OrderByDescending(n => n.priority).ToList();
            }
        }

        _debugDiscoveredPoints = discovered.ToList();
        return new List<Vector3>();
    }

    /// <summary>
    /// Builds the world path from parent map.
    /// </summary>
    private static List<Vector3> ReconstructPathPart(Dictionary<PathFindingTile, PathFindingTile> parentMap, PathFindingTile start, PathFindingTile end)
    {
        List<Vector3> path = new();
        PathFindingTile currentNode = end;

        while (currentNode != start)
        {
            path.Add(currentNode.Position);
            currentNode = parentMap[currentNode];
        }

        path.Add(start.Position);
        path.Reverse();
        return path;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Finds the closest walkable tile to the given position.
    /// </summary>
    private static Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        Vector3 closestNode = Vector3.zero;
        float distance = Mathf.Infinity;

        foreach (var group in PathFindingMap.Groups)
        {
            foreach (var tile in group.Tiles)
            {
                if (!tile.Walkable)
                    continue;

                float dist = Vector3.Distance(position, tile.Position);
                if (dist < distance)
                {
                    distance = dist;
                    closestNode = tile.Position;
                }
            }
        }

        return closestNode;
    }

    private static float Cost(Vector3 from, Vector3 to) => Vector3.Distance(from, to);
    private static float Heuristic(Vector3 from, Vector3 to) => Vector3.Distance(from, to);

    /// <summary>
    /// Gets a tile near the door connecting two groups (rooms).
    /// </summary>
    public static PathFindingTile GetDoorTileConnectingGroups(PathFindingGroup from, PathFindingGroup to)
    {
        var door = from.Room.GetDoorConnectingToRoom(to.Room);
        var target = new Vector3(door.Dimensions.x + .5f, door.GetCenter().y, door.Dimensions.y + .5f);

        if (PathFindingMap.Groups[RoomTileMap[(int)(target.z - .5f), (int)(target.x - .5f)]]
            .TryGetTileByPosition(target, out var tile))
        {
            return tile;
        }

        return null;
    }

    #endregion
    #endregion
}
