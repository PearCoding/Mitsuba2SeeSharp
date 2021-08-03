using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Mitsuba2SeeSharp {
    /// <summary>
    /// Triangle mesh
    /// </summary>
    public class Mesh {
        public List<Vector3> Vertices = new();
        public List<Vector3> Normals = new();
        public List<Vector2> TexCoords = new();
        public List<int> Indices = new();

        public int FaceCount => Indices.Count / 3;

        public void ApplyTransform(SeeTransform transform) {
            if (transform.IsIdentity)
                return;

            transform.TransformList(ref Vertices);
            transform.TransformNormalList(ref Normals);
        }

        public void FlipNormals() {
            for (int i = 0; i < Normals.Count; ++i) {
                Normals[i] = -Normals[i];
            }
        }

        public void FlipTexUp() {
            for (int i = 0; i < TexCoords.Count; ++i) {
                var tex = TexCoords[i];
                tex.Y = 1 - tex.Y;
                TexCoords[i] = tex;
            }
        }

        public void ComputeFaceNormals() {
            // Make all vertices unique
            List<Vector3> vertices = new(FaceCount * 3);
            for (int f = 0; f < FaceCount; ++f) {
                vertices.Add(Vertices[Indices[3 * f + 0]]);
                vertices.Add(Vertices[Indices[3 * f + 1]]);
                vertices.Add(Vertices[Indices[3 * f + 2]]);
            }

            // Use face normals for each vertex
            List<Vector3> normals = new(FaceCount * 3);
            for (int f = 0; f < FaceCount; ++f) {
                Vector3 x0 = Vertices[Indices[3 * f + 0]];
                Vector3 x1 = Vertices[Indices[3 * f + 1]];
                Vector3 x2 = Vertices[Indices[3 * f + 2]];

                Vector3 N = Vector3.Cross(x1 - x0, x2 - x0);
                normals.Add(N);
                normals.Add(N);
                normals.Add(N);
            }

            Vertices = vertices;
            Normals = normals;

            // Copy texcoords if necessary
            if (TexCoords.Count > 0) {
                List<Vector2> texcoords = new(FaceCount * 3);
                for (int f = 0; f < FaceCount; ++f) {
                    texcoords.Add(TexCoords[Indices[3 * f + 0]]);
                    texcoords.Add(TexCoords[Indices[3 * f + 1]]);
                    texcoords.Add(TexCoords[Indices[3 * f + 2]]);
                }
                TexCoords = texcoords;
            }

            // Linearize index list
            for (int f = 0; f < FaceCount; ++f) {
                Indices[3 * f + 0] = 3 * f + 0;
                Indices[3 * f + 1] = 3 * f + 1;
                Indices[3 * f + 2] = 3 * f + 2;
            }
        }

        public float SurfaceArea { get => GetSurfaceArea(); }

        public float GetSurfaceArea() {
            float area = 0;
            for (int face = 0; face < FaceCount; ++face) {
                var v1 = Vertices[Indices[face * 3 + 0]];
                var v2 = Vertices[Indices[face * 3 + 1]];
                var v3 = Vertices[Indices[face * 3 + 2]];
                Vector3 n = Vector3.Cross(v2 - v1, v3 - v1);
                area += n.Length() * 0.5f;
            }
            return area;
        }

        public byte[] ToPly() {
            List<byte> data = new();

            data.AddRange(CreatePlyHeader());

            for (int i = 0; i < Vertices.Count; ++i) {
                Vector3 vertex = Vertices[i];
                data.AddRange(BitConverter.GetBytes(vertex.X));
                data.AddRange(BitConverter.GetBytes(vertex.Y));
                data.AddRange(BitConverter.GetBytes(vertex.Z));
                if (Normals.Count > 0) {
                    Vector3 normal = Normals[i];
                    data.AddRange(BitConverter.GetBytes(normal.X));
                    data.AddRange(BitConverter.GetBytes(normal.Y));
                    data.AddRange(BitConverter.GetBytes(normal.Z));
                }
                if (TexCoords.Count > 0) {
                    Vector2 tex = TexCoords[i];
                    data.AddRange(BitConverter.GetBytes(tex.X));
                    data.AddRange(BitConverter.GetBytes(tex.Y));
                }
            }

            for (int i = 0; i < FaceCount; ++i) {
                data.Add(3);//Byte
                data.AddRange(BitConverter.GetBytes(Indices[i * 3 + 0]));
                data.AddRange(BitConverter.GetBytes(Indices[i * 3 + 1]));
                data.AddRange(BitConverter.GetBytes(Indices[i * 3 + 2]));
            }

            return data.ToArray();
        }

        private byte[] CreatePlyHeader() {
            string format = BitConverter.IsLittleEndian ? "binary_little_endian" : "binary_big_endian";
            List<string> properties = new();
            properties.Add("property float x");
            properties.Add("property float y");
            properties.Add("property float z");
            if (Normals.Count > 0) {
                properties.Add("property float nx");
                properties.Add("property float ny");
                properties.Add("property float nz");
            }
            if (TexCoords.Count > 0) {
                properties.Add("property float u");
                properties.Add("property float v");
            }

            string plyHeader = $@"ply
format {format} 1.0
comment Created by Mitsuba2SeeSharp
element vertex {Vertices.Count}
{string.Join('\n', properties)}
element face {FaceCount}
property list uchar int vertex_indices
end_header
";

            return Encoding.ASCII.GetBytes(plyHeader);
        }
    }
}
