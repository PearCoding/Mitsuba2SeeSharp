using System.Numerics;

namespace Mitsuba2SeeSharp {
    /// <summary>
    /// Construct mesh from primitives
    /// </summary>
    public static class PrimitiveLoader {
        /// <summary>
        /// Construct mesh from rectangle
        /// </summary>
        public static Mesh CreateRectangle() {
            Mesh mesh = new();

            mesh.Vertices.Add(new(-1, -1, 0));
            mesh.Vertices.Add(new(1, -1, 0));
            mesh.Vertices.Add(new(1, 1, 0));
            mesh.Vertices.Add(new(-1, 1, 0));

            Vector3 normal = new(0, 0, 1);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TexCoords.Add(new(0, 0));
            mesh.TexCoords.Add(new(1, 0));
            mesh.TexCoords.Add(new(1, 1));
            mesh.TexCoords.Add(new(0, 1));

            mesh.Indices.Add(0);
            mesh.Indices.Add(1);
            mesh.Indices.Add(2);
            mesh.Indices.Add(0);
            mesh.Indices.Add(2);
            mesh.Indices.Add(3);

            return mesh;
        }
    }
}
