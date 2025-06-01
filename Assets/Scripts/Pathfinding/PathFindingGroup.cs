using System.Collections.Generic;
using DungeonGeneration.Graph;
using UnityEngine;

/// <summary>
/// Represents a group of pathfinding tiles that belong to a single room in the dungeon.
/// Stores connections to other groups (rooms) and manages tile lookups.
/// </summary>
public class PathFindingGroup
{
    /// <summary>
    /// The list of walkable and non-walkable tiles that belong to this group.
    /// </summary>
    public List<PathFindingTile> Tiles = new List<PathFindingTile>();

    /// <summary>
    /// The groups (rooms) directly connected to this one.
    /// Used for high-level pathfinding across rooms.
    /// </summary>
    public List<PathFindingGroup> ConnectedGroups = new List<PathFindingGroup>();

    /// <summary>
    /// The representative world position of this group, typically the room center.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The underlying room node associated with this pathfinding group.
    /// </summary>
    public RoomNode Room;

    /// <summary>
    /// Constructs a new pathfinding group with a reference position and room node.
    /// </summary>
    /// <param name="position">The central position of the group (e.g., room center).</param>
    /// <param name="room">The associated room node in the dungeon graph.</param>
    public PathFindingGroup(Vector3 position, RoomNode room)
    {
        Position = position;
        Room = room;
    }

    /// <summary>
    /// Attempts to find a tile within this group by its exact position.
    /// </summary>
    /// <param name="position">The world position to search for.</param>
    /// <param name="foundTile">The tile at the specified position, if found.</param>
    /// <returns>True if a tile with the specified position exists in the group; otherwise, false.</returns>
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
