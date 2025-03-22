using System.Collections.Generic;
using UnityEngine;
using DelaunayTriangulation.Objects2D;
using DungeonGeneration.Graph;

namespace DelaunayTriangulation
{
    public static class DTriangulation
    {
        public static List<Edge> Triangulate(Graph graph)
        {
            List<GraphNode> nodes = graph.GetNodes();
            List<Triangle> triangles = new List<Triangle>();
            List<Edge> edges = new List<Edge>();
        
            //Create a starting triangle so all other vertexes are inside of it
        
            //Determine min and max vertices' positions
            float minX = nodes[0].Vertex.Position.x;
            float minY = nodes[0].Vertex.Position.y;
            float maxX = minX;
            float maxY = minY;

            foreach (var node in nodes)
            {
                if (node.Vertex.Position.x < minX) minX = node.Vertex.Position.x;
                if (node.Vertex.Position.x > maxX) maxX = node.Vertex.Position.x;
                if (node.Vertex.Position.y < minY) minY = node.Vertex.Position.y;
                if (node.Vertex.Position.y > maxY) maxY = node.Vertex.Position.y;
            }

            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            //Create new vertices
            Vertex p1 = new Vertex(new Vector2(minX - 1, minY - 1));
            Vertex p2 = new Vertex(new Vector2(minX - 1, maxY + deltaMax));
            Vertex p3 = new Vertex(new Vector2(maxX + deltaMax, minY - 1));

            //Create new rooms
            RoomGraphNode r1 = new RoomGraphNode(p1, new RectInt());
            RoomGraphNode r2 = new RoomGraphNode(p2, new RectInt());
            RoomGraphNode r3 = new RoomGraphNode(p3, new RectInt());

            //Add triangle
            triangles.Add(new Triangle(r1, r2, r3));

            //Triangulate
            //Loop through all vertices
            foreach (var node in nodes)
            {
                List<Edge> polygon = new List<Edge>();

                //Check if vertex is inside the existing triangle. If yes divide it into edges
                foreach (var t in triangles)
                {
                    if (t.CircumCircleContains(node.Vertex.Position))
                    {
                        t.IsBad = true;
                        polygon.Add(new Edge(t.A, t.B));
                        polygon.Add(new Edge(t.B, t.C));
                        polygon.Add(new Edge(t.C, t.A));
                    }
                }

                triangles.RemoveAll((Triangle t) => t.IsBad);

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
                    Triangle newTriangle = new Triangle(edge.A, edge.B, node);
                    triangles.Add(newTriangle);
                }
            }

            //Remove all triangles that were formed with vertices of the starting one
            triangles.RemoveAll((Triangle t) =>
                t.ContainsVertex(p1.Position) || t.ContainsVertex(p2.Position) || t.ContainsVertex(p3.Position));

            //Fill graph with unique doors
            HashSet<Edge> edgeSet = new HashSet<Edge>();
        
            foreach (var t in triangles)
            {
                var ab = new Edge(t.A, t.B);

                var bc = new Edge(t.B, t.C);

                var ca = new Edge(t.C, t.A);

                if (edgeSet.Add(ab))
                {
                    edges.Add(ab);
                }

                if (edgeSet.Add(bc))
                {
                    edges.Add(bc);

                }

                if (edgeSet.Add(ca))
                {
                    edges.Add(ca);
                }
            }

            return edges;
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
}