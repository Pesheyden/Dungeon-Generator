using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the map used for pathfinding, containing a list of pathfinding groups (rooms)
/// and helper methods to access them by position.
/// </summary>
public class PathFindingMap
{
    /// <summary>
    /// A list of all groups (rooms) within the pathfinding map.
    /// </summary>
    public List<PathFindingGroup> Groups = new List<PathFindingGroup>();

    /// <summary>
    /// Finds and returns the group located at the exact given position.
    /// </summary>
    /// <param name="position">The world position to look for.</param>
    /// <returns>The group at the given position if found; otherwise, null.</returns>
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

    /// <summary>
    /// Attempts to find the group (room) that contains a given tile position.
    /// </summary>
    /// <param name="position">The world position of the tile.</param>
    /// <param name="foundGroup">The group that contains the tile, if found.</param>
    /// <returns>True if a matching group is found; otherwise, false.</returns>
    public bool TryGetGroupByTilePosition(Vector3 position, out PathFindingGroup foundGroup)
    {
        foreach (var group in Groups)
        {
            Vector3 leftBottomCorner =
                new Vector3(group.Room.Dimensions.position.x, group.Position.y, group.Room.Dimensions.position.y);
            Vector3 rightTopCorner =
                new Vector3(group.Room.Dimensions.position.x + group.Room.Dimensions.width, group.Position.y, group.Room.Dimensions.position.y + group.Room.Dimensions.height);

            if (Mathf.Approximately(position.y, group.Position.y) &&
                position.x >= leftBottomCorner.x && position.z >= leftBottomCorner.z &&
                position.x <= rightTopCorner.x && position.z <= rightTopCorner.z)
            {
                foundGroup = group;
                return true;
            }
        }

        foundGroup = null;
        return false;
    }
}
