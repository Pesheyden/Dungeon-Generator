using System;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class DoorNode
    {
        public readonly RectInt Dimensions;
        public RoomNode[] ConnectedRooms = new RoomNode[2];

        public DoorNode(RectInt dimensions)
        {
            Dimensions = dimensions;
        }

        public Vector3 GetCenter()
        {
            return new Vector3(Dimensions.x + (float)Dimensions.width / 2, 0,
                Dimensions.y + (float)Dimensions.height / 2);
        }

        public RoomNode GetOtherRoom(RoomNode roomNode)
        {
            if (ConnectedRooms[0] == roomNode)
                return ConnectedRooms[1];
            else
                return ConnectedRooms[0];
        }

        public static bool operator ==(DoorNode left, DoorNode right)
        {
            return (left.ConnectedRooms[0] == right.ConnectedRooms[0] ||
                    left.ConnectedRooms[0] == right.ConnectedRooms[1]) &&
                   (left.ConnectedRooms[1] == right.ConnectedRooms[0] ||
                    left.ConnectedRooms[1] == right.ConnectedRooms[1]);
        }

        public static bool operator !=(DoorNode left, DoorNode right)
        {
            return (left.ConnectedRooms[0] != right.ConnectedRooms[0] &&
                    left.ConnectedRooms[0] != right.ConnectedRooms[1]) ||
                   (left.ConnectedRooms[1] != right.ConnectedRooms[0] &&
                    left.ConnectedRooms[1] != right.ConnectedRooms[1]);
        }

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

        protected bool Equals(DoorNode other)
        {
            return (this.ConnectedRooms[0] == other.ConnectedRooms[0] ||
                    this.ConnectedRooms[0] == other.ConnectedRooms[1]) &&
                   (this.ConnectedRooms[1] == other.ConnectedRooms[0] ||
                    this.ConnectedRooms[1] == other.ConnectedRooms[1]);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Dimensions, ConnectedRooms);
        }
    }
}