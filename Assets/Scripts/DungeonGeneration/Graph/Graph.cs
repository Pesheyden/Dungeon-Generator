using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class Graph
    {
        public List<RoomNode> Nodes = new();

        public List<RoomNode> GetRooms()
        {
            return Nodes;
        }

        public List<DoorNode> GetDoors()
        {
            HashSet<DoorNode> doors = new HashSet<DoorNode>();

            foreach (var node in Nodes)
            {
                foreach (var doorNode in node.DoorNodes)
                {
                    doors.Add(doorNode);
                }
            }

            return doors.ToList();
        }
        public void AddNode(RoomNode newNode)
        {
            Nodes.Add(new RoomNode(newNode.Dimensions));
        }

        public bool AddEdge(RoomNode fromNode, RoomNode toNode, DoorNode edgeNode)
        {
            if(!Nodes.Contains(fromNode))
                AddNode(fromNode);
            if(!Nodes.Contains(toNode))
                AddNode(toNode);

            var nodeA = Nodes.Find(n => n == fromNode);
            var nodeB = Nodes.Find(n => n == toNode);
        
            var edgeNodeCopy = new DoorNode(edgeNode.Dimensions);
            edgeNodeCopy.ConnectedRooms[0] = nodeA;
            edgeNodeCopy.ConnectedRooms[1] = nodeB;

            nodeA.DoorNodes.Add(edgeNodeCopy);
            nodeB.DoorNodes.Add(edgeNodeCopy);
            return true;
        }
    }
}