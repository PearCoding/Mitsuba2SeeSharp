using System.Numerics;
using System;

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

        /// <summary>
        /// Construct triangulated sphere
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="sliceCount"></param>
        /// <param name="stackCount"></param>
        /// <returns></returns>
        public static Mesh CreateSphere(Vector3 center, float radius, int sliceCount = 32, int stackCount = 64) {
            Mesh mesh = new();

            int count = sliceCount * stackCount;
            mesh.Vertices.Capacity = count;
            mesh.Normals.Capacity = count;
            mesh.TexCoords.Capacity = count;

            float drho = MathF.PI / (float)stackCount;
            float dtheta = 2 * MathF.PI / (float)sliceCount;

            // TODO: We create a 2*stacks of redundant vertices at the two critical points... remove them
            // Vertices
            for (int i = 0; i <= stackCount; ++i) {
                float rho = (float)i * drho;
                float srho = (float)(MathF.Sin(rho));
                float crho = (float)(MathF.Cos(rho));

                for (int j = 0; j < sliceCount; ++j) {
                    float theta = (j == sliceCount) ? 0.0f : j * dtheta;
                    float stheta = (float)(-MathF.Sin(theta));
                    float ctheta = (float)(MathF.Cos(theta));

                    Vector3 N = new(stheta * srho, ctheta * srho, crho);

                    mesh.Vertices.Add(N * radius + center);
                    mesh.Normals.Add(N);
                    mesh.TexCoords.Add(new(0.5f * theta / MathF.PI, rho / MathF.PI));
                }
            }

            // Indices
            for (int i = 0; i <= stackCount; ++i) {
                int currSliceOff = i * sliceCount;
                int nextSliceOff = ((i + 1) % (stackCount + 1)) * sliceCount;

                for (int j = 0; j < sliceCount; ++j) {
                    int nextJ = ((j + 1) % sliceCount);
                    int id0 = currSliceOff + j;
                    int id1 = currSliceOff + nextJ;
                    int id2 = nextSliceOff + j;
                    int id3 = nextSliceOff + nextJ;

                    mesh.Indices.AddRange(new int[] { id2, id3, id1, id2, id1, id0 });
                }
            }

            return mesh;
        }
    }
}
