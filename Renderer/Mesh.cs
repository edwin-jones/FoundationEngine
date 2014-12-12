using SharpDX;
using System;

namespace FoundationEngine.Renderer
{
    class Mesh
    {
        public String Name { get; set; }
        public Vector3[] Vertices { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(String name, Int32 verticesCount)
        {
            Vertices = new Vector3[verticesCount];
            Name = name;
        }

        public Mesh(string name, Vector3[] verts)
        {
            Vertices = verts;
            Name = name;
        }
    }
}
