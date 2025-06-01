using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    /// <summary>
    /// Represents a graph structure consisting of room nodes and door edges connecting them.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// The list of room nodes in the graph.
        /// </summary>
        public List<RoomNode> Nodes = new();

        /// <summary>
        /// Returns all room nodes in the graph.
        /// </summary>
        /// <returns>A list of <see cref="RoomNode"/> instances.</returns>
        public List<RoomNode> GetRooms()
        {
            return Nodes;
        }

        /// <summary>
        /// Returns all unique door nodes (edges) in the graph.
        /// </summary>
        /// <returns>A list of <see cref="DoorNode"/> instances.</returns>
        public List<DoorNode> GetDoors()
        {
            HashSet<DoorNode> doors = new HashSet<DoorNode>();

            foreach (var node in Nodes)
            {
                foreach (var doorNode in node.DoorNodes)
                {
                    doors.Add(doorNode); // HashSet prevents duplicates
                }
            }

            return doors.ToList();
        }

        /// <summary>
        /// Adds a new room node to the graph.
        /// </summary>
        /// <param name="newNode">The room node to add.</param>
        public void AddNode(RoomNode newNode)
        {
            // Clone the node to prevent unintended reference issues
            Nodes.Add(new RoomNode(newNode.Dimensions));
        }

        /// <summary>
        /// Adds a door (edge) between two room nodes in the graph. If either node is not already in the graph, it is added.
        /// </summary>
        /// <param name="fromNode">The first room node.</param>
        /// <param name="toNode">The second room node.</param>
        /// <param name="edgeNode">The door node connecting the rooms.</param>
        /// <returns>True if the edge is successfully added.</returns>
        public bool AddEdge(RoomNode fromNode, RoomNode toNode, DoorNode edgeNode)
        {
            // Ensure both nodes are part of the graph
            if (!Nodes.Contains(fromNode))
                AddNode(fromNode);
            if (!Nodes.Contains(toNode))
                AddNode(toNode);

            // Retrieve the actual instances from the list
            var nodeA = Nodes.Find(n => n == fromNode);
            var nodeB = Nodes.Find(n => n == toNode);

            // Clone the door and assign connected rooms
            var edgeNodeCopy = new DoorNode(edgeNode.Dimensions);
            edgeNodeCopy.ConnectedRooms[0] = nodeA;
            edgeNodeCopy.ConnectedRooms[1] = nodeB;

            // Connect rooms to the door
            nodeA.DoorNodes.Add(edgeNodeCopy);
            nodeB.DoorNodes.Add(edgeNodeCopy);

            return true;
        }
    }
}
