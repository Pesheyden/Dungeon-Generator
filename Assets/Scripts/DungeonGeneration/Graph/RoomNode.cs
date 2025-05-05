using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class RoomNode
    {
        public HashSet<DoorNode> DoorNodes = new HashSet<DoorNode>();
        public readonly RectInt Dimensions;

        public RoomNode(RectInt dimensions)
        {
            Dimensions = dimensions;
        }

        public Vector3 GetCenter()
        {
            return new Vector3(Dimensions.x + (float)Dimensions.width / 2, 0,
                Dimensions.y + (float)Dimensions.height / 2);
        }

        public List<RoomNode> GetConnectedRooms()
        {
            List<RoomNode> connectedRooms = new List<RoomNode>();

            foreach (var door in DoorNodes)
                connectedRooms.Add(door.GetOtherRoom(this));

            return connectedRooms;
        }

        public void ClearConnections()
        {
            foreach (var door in DoorNodes)
                door.GetOtherRoom(this).DoorNodes.Remove(door);

            DoorNodes.Clear();
        }

        public bool CanBeRemovedWithoutConnectionsSeparation(List<RoomNode> list)
        {
            HashSet<RoomNode> discovered = new HashSet<RoomNode>();
            Queue<RoomNode> Q = new Queue<RoomNode>();
            RoomNode v = this;
            var startNode = list[0] == v ? list[1] : list[0];
            Q.Enqueue(startNode);
            discovered.Add(startNode);
            discovered.Add(v);

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