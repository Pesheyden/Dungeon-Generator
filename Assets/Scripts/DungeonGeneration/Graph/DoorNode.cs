using System;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    /// <summary>
    /// Represents a door connecting two rooms in the dungeon graph.
    /// </summary>
    public class DoorNode
    {
        /// <summary>
        /// The rectangular area representing the door's position and size.
        /// </summary>
        public readonly RectInt Dimensions;

        /// <summary>
        /// The two rooms that this door connects.
        /// </summary>
        public RoomNode[] ConnectedRooms = new RoomNode[2];

        /// <summary>
        /// Initializes a new instance of the <see cref="DoorNode"/> class with the specified dimensions.
        /// </summary>
        /// <param name="dimensions">The dimensions of the door.</param>
        public DoorNode(RectInt dimensions)
        {
            Dimensions = dimensions;
        }

        /// <summary>
        /// Gets the center position of the door in world space (Y-axis is fixed to 0).
        /// </summary>
        /// <returns>The center of the door as a <see cref="Vector3"/>.</returns>
        public Vector3 GetCenter()
        {
            return new Vector3(Dimensions.x + (float)Dimensions.width / 2, 0,
                Dimensions.y + (float)Dimensions.height / 2);
        }

        /// <summary>
        /// Returns the other room connected by this door.
        /// </summary>
        /// <param name="roomNode">One of the rooms connected to the door.</param>
        /// <returns>The other connected room.</returns>
        public RoomNode GetOtherRoom(RoomNode roomNode)
        {
            if (ConnectedRooms[0] == roomNode)
                return ConnectedRooms[1];
            else
                return ConnectedRooms[0];
        }

        /// <summary>
        /// Equality operator to compare two door nodes based on their connected rooms (order-insensitive).
        /// </summary>
        public static bool operator ==(DoorNode left, DoorNode right)
        {
            return (left.ConnectedRooms[0] == right.ConnectedRooms[0] ||
                    left.ConnectedRooms[0] == right.ConnectedRooms[1]) &&
                   (left.ConnectedRooms[1] == right.ConnectedRooms[0] ||
                    left.ConnectedRooms[1] == right.ConnectedRooms[1]);
        }

        /// <summary>
        /// Inequality operator to compare two door nodes based on their connected rooms (order-insensitive).
        /// </summary>
        public static bool operator !=(DoorNode left, DoorNode right)
        {
            return (left.ConnectedRooms[0] != right.ConnectedRooms[0] &&
                    left.ConnectedRooms[0] != right.ConnectedRooms[1]) ||
                   (left.ConnectedRooms[1] != right.ConnectedRooms[0] &&
                    left.ConnectedRooms[1] != right.ConnectedRooms[1]);
        }

        /// <summary>
        /// Checks whether the specified object is equal to the current door.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is DoorNode d)
            {
                return (this.ConnectedRooms[0] == d.ConnectedRooms[0] ||
                        this.ConnectedRooms[0] == d.ConnectedRooms[1]) &&
                       (this.ConnectedRooms[1] == d.ConnectedRooms[0] ||
                        this.ConnectedRooms[1] == d.ConnectedRooms[1]);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified door node is equal to this one.
        /// </summary>
        /// <param name="other">The other door node.</param>
        /// <returns>True if they connect the same rooms, otherwise false.</returns>
        protected bool Equals(DoorNode other)
        {
            return (this.ConnectedRooms[0] == other.ConnectedRooms[0] ||
                    this.ConnectedRooms[0] == other.ConnectedRooms[1]) &&
                   (this.ConnectedRooms[1] == other.ConnectedRooms[0] ||
                    this.ConnectedRooms[1] == other.ConnectedRooms[1]);
        }

        /// <summary>
        /// Returns a hash code for this door based on its dimensions and connected rooms.
        /// </summary>
        /// <returns>A hash code representing the door.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Dimensions, ConnectedRooms);
        }
    }
}
