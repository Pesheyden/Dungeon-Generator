using DelaunayTriangulation.Objects2D;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class RoomGraphNode : GraphNode
    {
        public RoomNode RoomNode;
        public RoomGraphNode(RectInt size)
        {
            Size = size;

            Vertex = new Vertex(new Vector2(size.x + (float)size.width / 2, size.y + (float)size.height / 2));
        }

        public RoomGraphNode(Vertex vertex, RoomNode room)
        {
            Vertex = vertex;
            RoomNode = room;
        }
    }
}