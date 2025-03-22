using DelaunayTriangulation.Objects2D;
using UnityEngine;

namespace DungeonGeneration.Graph
{
    public class GraphNode
    {
        public Vertex Vertex { get; protected set; }
        public RectInt Size { get; protected set; }

    }
    
}