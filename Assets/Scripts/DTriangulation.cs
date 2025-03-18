using System.Collections.Generic;
using System.Linq;
using DelaunayTriangulation.Links;
using UnityEngine;
using DelaunayTriangulation.Objects2D;

namespace DelaunayTriangulation
{
    public class DTriangulation
    {
        public List<Vertex> Vertices { get; private set; }
        public List<Edge> Edges { get; private set; }
        public List<Triangle> Triangles { get; private set; }

        DTriangulation()
        {
            Edges = new List<Edge>();
            Triangles = new List<Triangle>();
        }

        public static DTriangulation Triangulate(Graph<GraphNode> graph)
        {
            DTriangulation d = new DTriangulation();
            d.Vertices = new List<Vertex>();
            d.Triangulate();

            return d;
        }

        void Triangulate()
        {
            
            //Create a starting triangle so all other vertexes are inside of it
            
            //Determine min and max vertices' positions
            float minX = Vertices[0].Position.x;
            float minY = Vertices[0].Position.y;
            float maxX = minX;
            float maxY = minY;

            foreach (var vertex in Vertices)
            {
                if (vertex.Position.x < minX) minX = vertex.Position.x;
                if (vertex.Position.x > maxX) maxX = vertex.Position.x;
                if (vertex.Position.y < minY) minY = vertex.Position.y;
                if (vertex.Position.y > maxY) maxY = vertex.Position.y;
            }

            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            //Create new vertices
            Vertex p1 = new Vertex(new Vector2(minX - 1, minY - 1));
            Vertex p2 = new Vertex(new Vector2(minX - 1, maxY + deltaMax));
            Vertex p3 = new Vertex(new Vector2(maxX + deltaMax, minY - 1));

            //Add triangle
            //Triangles.Add(new Triangle(p1, p2, p3));

            //Triangulate
            //Loop through all vertices 
            foreach (var vertex in Vertices)
            {
                List<Edge> polygon = new List<Edge>();

                //Check if vertex is inside the existing triangle. If yes divide it into edges
                foreach (var t in Triangles)
                {
                    if (t.CircumCircleContains(vertex.Position))
                    {
                        t.IsBad = true;
                        polygon.Add(new Edge(t.A, t.B));
                        polygon.Add(new Edge(t.B, t.C));
                        polygon.Add(new Edge(t.C, t.A));
                    }
                }

                Triangles.RemoveAll((Triangle t) => t.IsBad);

                //Remove almost equal edges
                for (int i = 0; i < polygon.Count; i++)
                {
                    for (int j = i + 1; j < polygon.Count; j++)
                    {
                        if (Edge.AlmostEqual(polygon[i], polygon[j]))
                        {
                            polygon[i].IsBad = true;
                            polygon[j].IsBad = true;
                        }
                    }
                }

                polygon.RemoveAll((Edge e) => e.IsBad);

                //Create new triangles
                foreach (var edge in polygon)
                {
                    //Triangle newTriangle = new Triangle(edge.A, edge.B, vertex);
                    //Triangles.Add(newTriangle);
                }
            }

            //Remove all triangles that were formed with vertices of the starting one
            Triangles.RemoveAll((Triangle t) =>
                t.ContainsVertex(p1.Position) || t.ContainsVertex(p2.Position) || t.ContainsVertex(p3.Position));

            //Fill Edges list with unique edges 
            HashSet<Edge> edgeSet = new HashSet<Edge>();

            foreach (var t in Triangles)
            {
                var ab = new Edge(t.A, t.B);
                var bc = new Edge(t.B, t.C);
                var ca = new Edge(t.C, t.A);

                if (edgeSet.Add(ab))
                {
                    Edges.Add(ab);
                }

                if (edgeSet.Add(bc))
                {
                    Edges.Add(bc);
                }

                if (edgeSet.Add(ca))
                {
                    Edges.Add(ca);
                }
            }
        }
    }

    namespace Objects2D
    {
        public class Vertex
        {
            public Vector2 Position;

            public Vertex(Vector2 position)
            {
                Position = position;
            }

            public static bool AlmostEqual(float x, float y)
            {
                return Mathf.Abs(x - y) <= float.Epsilon * Mathf.Abs(x + y) * 2
                       || Mathf.Abs(x - y) < float.MinValue;
            }

            public static bool AlmostEqual(Vertex left, Vertex right)
            {
                return AlmostEqual(left.Position.x, right.Position.x) && AlmostEqual(left.Position.y, right.Position.y);
            }
        }

        public class Edge
        {
            public readonly GraphNode A;
            public readonly GraphNode B;

            public bool IsBad;

            public Edge(GraphNode a, GraphNode b)
            {
                A = a;
                B = b;
            }

            public float Distance => Vector2.Distance(A.Vertex.Position, B.Vertex.Position);
            public static bool operator ==(Edge left, Edge right) {
                return (left.A == right.A || left.A == right.B)
                       && (left.B == right.A || left.B == right.B);
            }

            public static bool operator !=(Edge left, Edge right) {
                return !(left == right);
            }

            public override bool Equals(object obj) {
                if (obj is Edge e) {
                    return this == e;
                }

                return false;
            }

            public bool Equals(Edge e) {
                return this == e;
            }

            public override int GetHashCode() {
                return A.GetHashCode() ^ B.GetHashCode();
            }

            public static bool AlmostEqual(Edge left, Edge right)
            {
                return Vertex.AlmostEqual(left.A.Vertex, right.A.Vertex) && Vertex.AlmostEqual(left.B.Vertex, right.B.Vertex)
                       || Vertex.AlmostEqual(left.A.Vertex, right.B.Vertex) && Vertex.AlmostEqual(left.B.Vertex, right.A.Vertex);
            }
            
            
        }

        public class Triangle
        {
            public readonly GraphNode A;
            public readonly GraphNode B;
            public readonly GraphNode C;

            public bool IsBad;

            public Triangle(GraphNode a, GraphNode b, GraphNode c)
            {
                A = a;
                B = b;
                C = c;
            }

            public bool ContainsVertex(Vector2 v)
            {
                return Vector2.Distance(v, A.Vertex.Position) < 0.01f
                       || Vector2.Distance(v, B.Vertex.Position) < 0.01f
                       || Vector2.Distance(v, C.Vertex.Position) < 0.01f;
            }

            public bool CircumCircleContains(Vector2 v)
            {
                Vector2 a = A.Vertex.Position;
                Vector2 b = B.Vertex.Position;
                Vector2 c = C.Vertex.Position;

                float oa = a.sqrMagnitude;
                float ob = b.sqrMagnitude;
                float oc = c.sqrMagnitude;

                float circumX = (oa * (c.y - b.y) + ob * (a.y - c.y) + oc * (b.y - a.y)) /
                                (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y));
                float circumY = (oa * (c.x - b.x) + ob * (a.x - c.x) + oc * (b.x - a.x)) /
                                (a.y * (c.x - b.x) + b.y * (a.x - c.x) + c.y * (b.x - a.x));

                Vector2 circum = new Vector2(circumX / 2, circumY / 2);
                float circumRadius = Vector2.SqrMagnitude(a - circum);
                float dist = Vector2.SqrMagnitude(v - circum);
                return dist <= circumRadius;
            }
            public static bool operator ==(Triangle left, Triangle right) {
                return (left.A == right.A || left.A == right.B || left.A == right.C)
                       && (left.B == right.A || left.B == right.B || left.B == right.C)
                       && (left.C == right.A || left.C == right.B || left.C == right.C);
            }

            public static bool operator !=(Triangle left, Triangle right) {
                return !(left == right);
            }

            public override bool Equals(object obj) {
                if (obj is Triangle t) {
                    return this == t;
                }

                return false;
            }

            public bool Equals(Triangle t) {
                return this == t;
            }

            public override int GetHashCode() {
                return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
            }
        }
    }
    namespace Links
    {
        public class DualLinkedVertex
        {
            public Vertex MainVertex { get; private set; }
            private readonly HashSet<DualLinkedVertex> _connectedVertexes = new();

            public DualLinkedVertex(Vertex mainVertex)
            {
                MainVertex = mainVertex;
            }

            public bool AddConnection(DualLinkedVertex newVertex) => _connectedVertexes.Add(newVertex);
            public bool RemoveConnection(DualLinkedVertex newVertex) => _connectedVertexes.Remove(newVertex);
            public List<DualLinkedVertex> GetConnections() => _connectedVertexes.ToList();

            public bool VertexEqual(Vertex vertex) => MainVertex == vertex;
        }

        public class GraphNode
        {
            public Vertex Vertex { get; protected set; }
            public RectInt Size { get; protected set; }
            
        }

        public class RoomGraphNode : GraphNode
        {
            public RoomGraphNode(RectInt size)
            {
                Size = size;

                Vertex = new Vertex(new Vector2(size.x + (float)size.width / 2, size.y + (float)size.height / 2));
            }
            public RoomGraphNode(Vertex vertex, RectInt size)
            {
                Vertex = vertex;
                Size = size;
            }
        }       
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
}