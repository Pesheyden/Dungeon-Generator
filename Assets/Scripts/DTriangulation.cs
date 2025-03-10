using System.Collections.Generic;
using System.Linq;
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

        public static DTriangulation Triangulate(List<Vertex> vertices)
        {
            DTriangulation d = new DTriangulation();
            d.Vertices = new List<Vertex>(vertices);
            d.Triangulate();

            return d;
        }

        void Triangulate()
        {
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

            Vertex p1 = new Vertex(new Vector2(minX - 1, minY - 1));
            Vertex p2 = new Vertex(new Vector2(minX - 1, maxY + deltaMax));
            Vertex p3 = new Vertex(new Vector2(maxX + deltaMax, minY - 1));

            Triangles.Add(new Triangle(p1, p2, p3));

            foreach (var vertex in Vertices)
            {
                List<Edge> polygon = new List<Edge>();

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

                foreach (var edge in polygon)
                {
                    Triangles.Add(new Triangle(edge.A, edge.B, vertex));
                }
            }

            Triangles.RemoveAll((Triangle t) =>
                t.ContainsVertex(p1.Position) || t.ContainsVertex(p2.Position) || t.ContainsVertex(p3.Position));

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
            public readonly Vertex A;
            public readonly Vertex B;

            public bool IsBad;

            public Edge(Vertex a, Vertex b)
            {
                A = a;
                B = b;
            }

            public float Distance => Vector2.Distance(A.Position, B.Position);

            public static bool AlmostEqual(Edge left, Edge right)
            {
                return Vertex.AlmostEqual(left.A, right.A) && Vertex.AlmostEqual(left.B, right.B)
                       || Vertex.AlmostEqual(left.A, right.B) && Vertex.AlmostEqual(left.B, right.A);
            }
        }

        public class Triangle
        {
            public readonly Vertex A;
            public readonly Vertex B;
            public readonly Vertex C;

            public bool IsBad;

            public Triangle(Vertex a, Vertex b, Vertex c)
            {
                A = a;
                B = b;
                C = c;
            }

            public bool ContainsVertex(Vector2 v)
            {
                return Vector2.Distance(v, A.Position) < 0.01f
                       || Vector2.Distance(v, B.Position) < 0.01f
                       || Vector2.Distance(v, C.Position) < 0.01f;
            }

            public bool CircumCircleContains(Vector2 v)
            {
                Vector2 a = A.Position;
                Vector2 b = B.Position;
                Vector2 c = C.Position;

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
        }
    }

    namespace Links
    {
        public class VertexLink
        {
            public Vertex MainVertex { get; private set; }
            private readonly HashSet<Vertex> _connectedVertexes = new();

            public VertexLink(Vertex mainVertex)
            {
                MainVertex = mainVertex;
            }

            public bool AddConnection(Vertex newVertex) => _connectedVertexes.Add(newVertex);
            public bool RemoveConnection(Vertex newVertex) => _connectedVertexes.Remove(newVertex);
            public List<Vertex> GetConnections() => _connectedVertexes.ToList();
        }
    }
}