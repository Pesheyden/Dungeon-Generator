using DelaunayTriangulation.Objects2D;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class DoorGraphNode : GraphNode
    {
        public DoorGraphNode(RectInt size)
        {
            Size = size;

            Vertex = new Vertex(new Vector2(size.x + (float)size.width / 2, size.y + (float)size.height / 2));
        }

        public DoorGraphNode(Vertex vertex, RectInt size)
        {
            Vertex = vertex;
            Size = size;
        }
    }
}