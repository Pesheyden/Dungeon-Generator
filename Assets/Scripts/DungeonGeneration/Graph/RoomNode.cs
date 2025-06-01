using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    /// <summary>
    /// Represents a room in a dungeon generation graph, with doors connecting to other rooms.
    /// </summary>
    public class RoomNode
    {
        /// <summary>
        /// The set of door nodes that connect this room to others.
        /// </summary>
        public HashSet<DoorNode> DoorNodes = new HashSet<DoorNode>();

        /// <summary>
        /// The dimensions of this room in the dungeon grid.
        /// </summary>
        public readonly RectInt Dimensions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomNode"/> class with the specified dimensions.
        /// </summary>
        /// <param name="dimensions">The rectangular area occupied by this room.</param>
        public RoomNode(RectInt dimensions)
        {
            Dimensions = dimensions;
        }

        /// <summary>
        /// Gets the center position of the room in world space (Y is fixed to 0).
        /// </summary>
        /// <returns>The center point of the room as a Vector3.</returns>
        public Vector3 GetCenter()
        {
            return new Vector3(Dimensions.x + (float)Dimensions.width / 2, 0,
                Dimensions.y + (float)Dimensions.height / 2);
        }

        /// <summary>
        /// Gets a list of rooms connected to this room via doors.
        /// </summary>
        /// <returns>List of connected <see cref="RoomNode"/> instances.</returns>
        public List<RoomNode> GetConnectedRooms()
        {
            List<RoomNode> connectedRooms = new List<RoomNode>();

            foreach (var door in DoorNodes)
                connectedRooms.Add(door.GetOtherRoom(this));

            return connectedRooms;
        }

        /// <summary>
        /// Gets the door that connects this room to a specified other room.
        /// </summary>
        /// <param name="roomNode">The other room to check the connection for.</param>
        /// <returns>The <see cref="DoorNode"/> connecting the rooms, or null if none exists.</returns>
        public DoorNode GetDoorConnectingToRoom(RoomNode roomNode)
        {
            foreach (var door in DoorNodes)
            {
                if (door.GetOtherRoom(this) == roomNode)
                    return door;
            }

            return null;
        }

        /// <summary>
        /// Removes all connections (doors) from this room and updates connected rooms accordingly.
        /// </summary>
        public void ClearConnections()
        {
            foreach (var door in DoorNodes)
                door.GetOtherRoom(this).DoorNodes.Remove(door);

            DoorNodes.Clear();
        }

        /// <summary>
        /// Determines whether this room can be removed without disconnecting the dungeon graph.
        /// </summary>
        /// <param name="list">List of all rooms in the graph.</param>
        /// <returns>True if the graph remains connected after removal, false otherwise.</returns>
        public bool CanBeRemovedWithoutConnectionsSeparation(List<RoomNode> list)
        {
            HashSet<RoomNode> discovered = new HashSet<RoomNode>();
            Queue<RoomNode> Q = new Queue<RoomNode>();
            RoomNode v = this;

            // Start from a node other than this one
            var startNode = list[0] == v ? list[1] : list[0];
            Q.Enqueue(startNode);
            discovered.Add(startNode);
            discovered.Add(v); // Mark current room as discovered to simulate its removal

            // Perform BFS
            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                foreach (RoomNode w in v.GetConnectedRooms())
                {
                    if (!discovered.Contains(w))
                    {
                        Q.Enqueue(w);
                        discovered.Add(w);
                    }
                }
            }

            return discovered.Count == list.Count;
        }
        
        public static bool operator ==(RoomNode left, RoomNode right)
        {
            return left.Dimensions == right.Dimensions;
        }
        
        public static bool operator !=(RoomNode left, RoomNode right)
        {
            return left.Dimensions != right.Dimensions;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is RoomNode r)
            {
                return this.Dimensions == r.Dimensions;
            }

            return false;
        }
        
        protected bool Equals(RoomNode other)
        {
            return this.Dimensions == other.Dimensions;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Dimensions);
        }
    }
}
