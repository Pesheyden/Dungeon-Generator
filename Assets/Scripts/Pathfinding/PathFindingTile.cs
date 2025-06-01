using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single tile in the pathfinding system. 
/// Contains position, walkability status, and connected tiles for navigation.
/// </summary>
public class PathFindingTile
{
    /// <summary>
    /// The world-space position of the tile. Used for rendering and distance calculations.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Indicates whether this tile is walkable.
    /// Non-walkable tiles are ignored in pathfinding.
    /// </summary>
    public bool Walkable;

    /// <summary>
    /// The list of adjacent, connected tiles to which movement is allowed.
    /// </summary>
    public List<PathFindingTile> ConnectedTiles = new List<PathFindingTile>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PathFindingTile"/> class.
    /// </summary>
    /// <param name="position">The world position of the tile.</param>
    /// <param name="walkable">Whether the tile is walkable.</param>
    public PathFindingTile(Vector3 position, bool walkable)
    {
        Position = position;
        Walkable = walkable;
    }

    /// <summary>
    /// Equality operator overload. Compares tiles by position.
    /// </summary>
    public static bool operator ==(PathFindingTile left, PathFindingTile right)
    {
        return left.Position == right.Position;
    }

    /// <summary>
    /// Inequality operator overload. Compares tiles by position.
    /// </summary>
    public static bool operator !=(PathFindingTile left, PathFindingTile right)
    {
        return left.Position != right.Position;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current tile.
    /// Equality is based on position.
    /// </summary>
    /// <param name="obj">The object to compare with the current tile.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is PathFindingTile t)
        {
            return this.Position == t.Position;
        }

        return false;
    }

    /// <summary>
    /// Compares this tile with another tile for equality based on position.
    /// </summary>
    /// <param name="other">The tile to compare against.</param>
    /// <returns>True if positions are equal; otherwise, false.</returns>
    protected bool Equals(PathFindingTile other)
    {
        return this.Position == other.Position;
    }

    /// <summary>
    /// Gets the hash code for this tile, using position and connections.
    /// </summary>
    /// <returns>An integer hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, ConnectedTiles);
    }
}
