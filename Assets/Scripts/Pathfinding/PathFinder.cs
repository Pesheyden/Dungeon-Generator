using System;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.Graph;
using UnityEngine;

public static class PathFinder
{
    public static PathFindingMap PathFindingMap;

    public static int[,] RoomTileMap;

    #region Initialization
    public static void Initialize(int[,] tileMap, int[,] roomTileMap, Graph graph)
    {
        RoomTileMap = roomTileMap;
        CreatePathFindingMap(tileMap, roomTileMap, graph);
    }
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
                var newTile = new PathFindingTile(new Vector3(x + 0.5f, PathFindingMap.Groups[roomTileMap[z, x]].Position.y, z + 0.5f),
                    tileMap[z, x] == 0);
                PathFindingMap.Groups[roomTileMap[z,x]].Tiles.Add(newTile);
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

    private static void TryAddTileConnections(PathFindingTile tile ,Vector3 target)
    {
        if(RoomTileMap.GetLength(0) <= target.z || RoomTileMap.GetLength(1) <= target.x || target.x < 0 || target.z < 0)
            return;
        
        if (PathFindingMap.Groups[RoomTileMap[(int)(target.z -.5f), (int)(target.x - .5f)]].TryGetTileByPosition(target, out var connectedTile))
        {
            connectedTile.ConnectedTiles.Add(tile);
            tile.ConnectedTiles.Add(connectedTile);
        }
    }
    
    private static void BatchMapDebug()
    {
        DebugDrawingBatcher.BatchCall("PathFindingGraph", () =>
        {
            foreach (var group in PathFindingMap.Groups)
            {
                DebugExtension.DebugWireSphere(group.Position + Vector3.up, Color.magenta);
                foreach (var connectedGroup in group.ConnectedGroups)
                {
                    Debug.DrawLine(group.Position + Vector3.up, connectedGroup.Position + Vector3.up, Color.magenta);
                }
                foreach (var tile in group.Tiles)
                {
                    DebugExtension.DebugWireSphere(tile.Position, tile.Walkable ? Color.cyan : Color.black, 0.2f);
                    foreach (var connectedTile in tile.ConnectedTiles)
                    {
                        Debug.DrawLine(tile.Position, connectedTile.Position, tile.Walkable ? (connectedTile.Walkable ? Color.cyan: Color.black) : Color.black);
                    }
                }
            }
        });
    }
    
    #endregion

    #region PathFinding

    public static List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        PathFindingTile startTile;
        PathFindingTile endTile;
        Debug.Log(1);

        if (!PathFindingMap.Groups[RoomTileMap[(int)(start.z - .5f), (int)(start.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(start), out startTile))
            return null;

        if (!PathFindingMap.Groups[RoomTileMap[(int)(end.z - .5f), (int)(end.x - .5f)]]
                .TryGetTileByPosition(GetClosestNodeToPosition(end), out endTile))
            return null;

        Debug.Log(2);
        var groupPath = FindGroupPath(startTile, endTile);
        List<Vector3> result = new List<Vector3>();

        PathFindingTile lastTile = groupPath[0];

        for (var index = 1; index < groupPath.Count; index++)
        {
            var point = groupPath[index];
            result.AddRange(FindPathPart(lastTile, point));
            Debug.Log(lastTile.Position + " " + point.Position);
            lastTile = point;
        }

        return result;
    }

    #region GroupPath

     private static List<PathFindingTile> FindGroupPath(PathFindingTile start, PathFindingTile end)
    {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()

        PathFindingGroup startGroup =
            PathFindingMap.Groups[RoomTileMap[(int)(start.Position.z - .5f), (int)(start.Position.x - .5f)]];
        PathFindingGroup endGroup =             
            PathFindingMap.Groups[RoomTileMap[(int)(end.Position.z - .5f), (int)(end.Position.x - .5f)]];;
        HashSet<PathFindingGroup> discovered = new HashSet<PathFindingGroup>();
        List<(PathFindingGroup node, float priority)> q = new List<(PathFindingGroup, float)>();
        Dictionary<PathFindingGroup, PathFindingGroup> p = new Dictionary<PathFindingGroup, PathFindingGroup>();
        Dictionary<PathFindingGroup, float> c = new Dictionary<PathFindingGroup, float>();
        (PathFindingGroup node, float priority) v;
        q.Add((startGroup, 0));

        while (q.Count > 0)
        {
            v = q[^1];
            q.Remove(q[^1]);
            if (v.node == endGroup)
            {
                Debug.Log("FoundPath");
                return ReconstructGroupPath(p, start, end, startGroup, endGroup);
            }


            foreach (var w in v.node.ConnectedGroups)
            {
                if (!discovered.Add(w))
                    continue;
                
                
                float newCost = v.priority + Cost(v.node.Position, w.Position);
                if (!c.ContainsKey(w))
                {
                    c.Add(w, newCost);
                    p.Add(w,v.node);
                }
                else if(c[w] > newCost)
                {
                    c[w] = newCost;
                    p[w] = v.node;
                }
                
                q.Add((w, c[w] + Heuristic(w.Position,end.Position)));
                q = q.OrderByDescending(node => node.priority).ToList();
            }
        }
        Debug.Log("No path found");
        return new List<PathFindingTile>(); // No path found
    }
     
    private static List<PathFindingTile> ReconstructGroupPath(Dictionary<PathFindingGroup, PathFindingGroup> parentMap, PathFindingTile start, PathFindingTile end, PathFindingGroup startGroup ,PathFindingGroup endGroup)
    {
        List<PathFindingTile> path = new List<PathFindingTile>();
        PathFindingTile  currentNode = end;
        Dictionary<PathFindingTile, PathFindingTile> tileParentMap = new();

        PathFindingTile lastTile = end;
        PathFindingGroup lastGroup = endGroup;
        
        while (lastGroup != startGroup)
        {
            var group = parentMap[lastGroup];
            var tile = GetDoorTileConnectingGroups(group, lastGroup);
            Debug.Log(lastGroup.Position + " " + group.Position + " ! " + lastTile.Position + " " + tile.Position);
            tileParentMap.Add(lastTile, tile);
            lastGroup = group;
            lastTile = tile;
        }
        tileParentMap.Add(lastTile, start);
        Debug.Log(1);

        while (currentNode != start)
        {
            Debug.Log(currentNode.Position);
            path.Add(currentNode);
            currentNode = tileParentMap[currentNode];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }
    #endregion

    #region FullPath
    
    private static List<Vector3> FindPathPart(PathFindingTile start, PathFindingTile end)
    {
        HashSet<PathFindingTile> discovered = new HashSet<PathFindingTile>();
        List<(PathFindingTile node, float priority)> q = new List<(PathFindingTile, float)>();
        Dictionary<PathFindingTile, PathFindingTile> p = new Dictionary<PathFindingTile, PathFindingTile>();
        Dictionary<PathFindingTile, float> c = new Dictionary<PathFindingTile, float>();
        (PathFindingTile node, float priority) v;
        q.Add((start, 0));

        while (q.Count > 0)
        {
            v = q[^1];
            q.Remove(q[^1]);
            if (v.node == end)
                return ReconstructPathPart(p, start, end);
            
            if(!v.node.Walkable)
                continue;
            
            foreach (var w in v.node.ConnectedTiles)
            {
                if (!discovered.Add(w))
                    continue;
                
                float newCost = v.priority + Cost(v.node.Position, w.Position);
                if (!c.ContainsKey(w))
                {
                    c.Add(w, newCost);
                    p.Add(w,v.node);
                }
                else if(c[w] > newCost)
                {
                    c[w] = newCost;
                    p[w] = v.node;
                }
                
                q.Add((w, c[w] + Heuristic(w.Position,end.Position)));
                q = q.OrderByDescending(node => node.priority).ToList();
            }
        }
        
        return new List<Vector3>(); // No path found
    }

    private static List<Vector3> ReconstructPathPart(Dictionary<PathFindingTile, PathFindingTile> parentMap, PathFindingTile start, PathFindingTile end)
    {
        List<Vector3> path = new List<Vector3>();
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

    private static Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        Vector3 closestNode = Vector3.zero;
        float distance = Mathf.Infinity;

        foreach (var group in PathFindingMap.Groups)
        {
            foreach (var tile in group.Tiles)
            {
                if(!tile.Walkable)
                    continue;
                if (Vector3.Distance(position, tile.Position) < distance)
                {
                    distance = Vector3.Distance(position, tile.Position);
                    closestNode = tile.Position;
                }
            }
        }
        
        return closestNode;
    }
    
    private static float Cost(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    
    private static float Heuristic(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    
    
    public static PathFindingTile GetDoorTileConnectingGroups(PathFindingGroup from,PathFindingGroup to)
    {
        var door = from.Room.GetDoorConnectingToRoom(to.Room);
        var target = new Vector3(door.Dimensions.x + .5f, door.GetCenter().y, door.Dimensions.y + .5f);
        if (PathFindingMap.Groups[RoomTileMap[(int)(target.z - .5f), (int)(target.x - .5f)]]
            .TryGetTileByPosition(target,  out var tile))
        {
            return tile;
        }

        return null;
    }

    #endregion
   
    
   

    #endregion
}

public class PathFindingMap
{
    public List<PathFindingGroup> Groups = new List<PathFindingGroup>();
    
    public PathFindingGroup GetGroupByPosition(Vector3 position)
    {
        foreach (var group in Groups)
        {
            if (group.Position == position)
            {
                return group;
            }

        }
        
        return null;
    }

    public bool TryGetGroupByTilePosition(Vector3 position, out PathFindingGroup foundGroup)
    {
        foreach (var group in Groups)
        {
            Vector3 leftBottomCorner =
                new Vector3(group.Room.Dimensions.position.x, group.Position.y, group.Room.Dimensions.position.y);
            Vector3 rightTopCorner =
                new Vector3(group.Room.Dimensions.position.x + group.Room.Dimensions.width, group.Position.y, group.Room.Dimensions.position.y + group.Room.Dimensions.height);

            if (Mathf.Approximately(position.y, group.Position.y) &&
                position.x >= leftBottomCorner.x && position.y >= leftBottomCorner.y &&
                position.x <= rightTopCorner.x && position.y <= rightTopCorner.y)
            {
                foundGroup = group;
                return true;
            }

        }

        foundGroup = null;
        return false;
    }
}

public class PathFindingGroup
{
    public List<PathFindingTile> Tiles = new List<PathFindingTile>();
    public List<PathFindingGroup> ConnectedGroups = new List<PathFindingGroup>();
    public Vector3 Position;
    public RoomNode Room;

    public PathFindingGroup(Vector3 position, RoomNode room)
    {
        Position = position;
        Room = room;
    }

    public bool TryGetTileByPosition(Vector3 position, out PathFindingTile foundTile)
    {
        foreach (var tile in Tiles)
        {
            if (tile.Position == position)
            {
                foundTile = tile;
                return true;
            }

        }

        foundTile = null;
        return false;
    }
}

public class PathFindingTile
{
    public Vector3 Position;
    public bool Walkable;
    public List<PathFindingTile> ConnectedTiles = new List<PathFindingTile>();

    public PathFindingTile(Vector3 position, bool walkable)
    {
        Position = position;
        Walkable = walkable;
    }
    
    public static bool operator == (PathFindingTile left, PathFindingTile right)
    {
        return left.Position == right.Position;
    }

    public static bool operator != (PathFindingTile left, PathFindingTile right)
    {
        return left.Position != right.Position;
    }

    public override bool Equals(object obj)
    {
        if (obj is PathFindingTile t)
        {
            return this.Position == t.Position;
        }

        return false;
    }

    protected bool Equals(PathFindingTile other)
    {
        return this.Position == other.Position;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, ConnectedTiles);
    }
}