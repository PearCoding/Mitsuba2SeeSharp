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
            Matrix4x4 m = new(
                transform.matrix.elements[0], transform.matrix.elements[1], transform.matrix.elements[2], transform.matrix.elements[3],
                transform.matrix.elements[4], transform.matrix.elements[5], transform.matrix.elements[6], transform.matrix.elements[7],
                transform.matrix.elements[8], transform.matrix.elements[9], transform.matrix.elements[10], transform.matrix.elements[11],
                transform.matrix.elements[12], transform.matrix.elements[13], transform.matrix.elements[14], transform.matrix.elements[15]);

            for (int i = 0; i < Vertices.Count; ++i) {
                Vertices[i] = Vector3.Transform(Vertices[i], m);
            }

            for (int i = 0; i < Normals.Count; ++i) {
                Normals[i] = Vector3.TransformNormal(Normals[i], m);
            }
        }

        public void FlipNormals() {
            for (int i = 0; i < Normals.Count; ++i) {
                Normals[i] = -Normals[i];
            }
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
                data.AddRange(BitConverter.GetBytes(Indices[i * 3 + 0]));
                data.AddRange(BitConverter.GetBytes(Indices[i * 3 + 0]));
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
