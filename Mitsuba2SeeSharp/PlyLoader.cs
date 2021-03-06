using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Mitsuba2SeeSharp {

    public class MeshLoadingOptions {
        public bool IgnoreNormals { get; set; } = false;
        public bool IgnoreTexCoords { get; set; } = false;
    }

    /// <summary>
    /// Simple class allowing to mix ascii text and binary data reading
    /// </summary>
    internal class MixReader : BinaryReader {
        public MixReader(string path, Encoding encoding) : base(new FileStream(path, FileMode.Open, FileAccess.Read), encoding) {
        }

        public string ReadLineAsString(ref bool eos) {
            StringBuilder stringBuffer = new(1024);

            eos = false;
            try {
                while (true) {
                    char ch = base.ReadChar();
                    if (ch == '\r') { // Windows style
                        ch = base.ReadChar();
                        if (ch == '\n') {
                            break;
                        } else {
                            stringBuffer.Append(ch);
                        }
                    } else if (ch == '\n') { // Unix style
                        break;
                    } else {
                        stringBuffer.Append(ch);
                    }
                }
            } catch (EndOfStreamException) {
                eos = true;
            }

            if (stringBuffer.Length == 0)
                return "";
            else
                return stringBuffer.ToString();
        }
    }

    /// <summary>
    /// Ply loader mesh
    /// </summary>
    public static class PlyLoader {
        /// <summary>
        /// A face with arbitrarily many vertices, given by a list of indices
        /// </summary>
        public class Face {
            /// <summary> Indices of the face </summary>
            public List<int> Indices = new();
        }

        /// <summary>
        /// Loads .ply file and returns list of errors
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>List of errors (and warnings)</returns>
        public static Mesh ParseFile(string filename, MeshLoadingOptions options) {
            // Parse the .ply itself
            using MixReader file = new(filename, Encoding.ASCII);
            return ParsePlyFile(file, options);
        }

        /// <summary>
        /// Construct list of triangle indices from convex polygons
        /// </summary>
        private static List<int> ToTriangleList(List<Face> faces) {
            List<int> list = new();
            foreach (Face f in faces) {
                if (f.Indices.Count == 3) {
                    list.AddRange(f.Indices);
                } else if (f.Indices.Count < 3) {
                    // TODO: Print warning?
                } else { // Fan triangulation, only works for convex polygons
                    int pin = f.Indices[0];
                    for (int i = 2; i < f.Indices.Count; ++i) {
                        list.AddRange(new int[3] { pin, f.Indices[i - 1], f.Indices[i] });
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Interface to read binary and ascii based data region
        /// </summary>
        private interface IDataReader {
            public (Vector3, Vector3, Vector2) ReadPerVertexLine();
            public Face ReadFaceLine();
        }

        /// <summary>
        /// Data reader for ascii based files
        /// </summary>
        private class AsciiDataReader : IDataReader {
            public AsciiDataReader(PlyHeader header, MixReader stream) {
                Header = header;
                Stream = stream;
            }

            public Face ReadFaceLine() {
                bool eos = false;// Ignore
                string[] parts = Stream.ReadLineAsString(ref eos).Split();
                int count = int.Parse(parts[0]);
                return new() { Indices = parts.Skip(1).Take(count).Select(p => int.Parse(p)).ToList() };
            }

            public (Vector3, Vector3, Vector2) ReadPerVertexLine() {
                bool eos = false;// Ignore
                string[] parts = Stream.ReadLineAsString(ref eos).Split();

                float GetPart(int elem) {
                    if (elem != -1 && elem < parts.Length)
                        return float.Parse(parts[elem]);
                    else
                        return 0;
                }

                float x = GetPart(Header.XElem);
                float y = GetPart(Header.YElem);
                float z = GetPart(Header.ZElem);
                float nx = GetPart(Header.NXElem);
                float ny = GetPart(Header.NYElem);
                float nz = GetPart(Header.NZElem);
                float u = GetPart(Header.UElem);
                float v = GetPart(Header.VElem);

                return (new(x, y, z), new(nx, ny, nz), new(u, v));
            }

            private readonly PlyHeader Header;
            private readonly MixReader Stream;
        }

        /// <summary>
        /// Data reader for binary based files
        /// </summary>
        private class BinaryDataReader : IDataReader {
            public BinaryDataReader(PlyHeader header, MixReader stream) {
                Header = header;
                Stream = stream;
                IsBigEndian = header.IsBigEndian;
            }

            private int GetIndex() {
                bool swap = IsBigEndian == BitConverter.IsLittleEndian;
                int val = Stream.ReadInt32();
                if (swap) {
                    byte[] bytes = BitConverter.GetBytes(val);
                    Array.Reverse(bytes);
                    return BitConverter.ToInt32(bytes);
                } else {
                    return val;
                }
            }

            private float GetSingle() {
                bool swap = IsBigEndian == BitConverter.IsLittleEndian;
                float val = Stream.ReadSingle();
                if (swap) {
                    byte[] bytes = BitConverter.GetBytes(val);
                    Array.Reverse(bytes);
                    return BitConverter.ToSingle(bytes);
                } else {
                    return val;
                }
            }

            public Face ReadFaceLine() {
                byte elemcount = Stream.ReadByte();

                Face face = new();
                for (byte i = 0; i < elemcount; ++i) {
                    face.Indices.Add(GetIndex());
                }

                return face;
            }

            public (Vector3, Vector3, Vector2) ReadPerVertexLine() {
                float x = 0, y = 0, z = 0;
                float nx = 0, ny = 0, nz = 0;
                float u = 0, v = 0;

                for (int i = 0; i < Header.VertexPropCount; ++i) {
                    float val = GetSingle();
                    if (i == Header.XElem) x = val;
                    if (i == Header.YElem) y = val;
                    if (i == Header.ZElem) z = val;
                    if (i == Header.NXElem) nx = val;
                    if (i == Header.NYElem) ny = val;
                    if (i == Header.NZElem) nz = val;
                    if (i == Header.UElem) u = val;
                    if (i == Header.VElem) v = val;
                }

                return (new(x, y, z), new(nx, ny, nz), new(u, v));
            }

            private readonly PlyHeader Header;
            private readonly MixReader Stream;
            private readonly bool IsBigEndian;
        }

        /// <summary>
        /// Essential informations from the .ply header which is always given in ascii format
        /// </summary>
        private class PlyHeader {
            public int VertexCount = 0;
            public int FaceCount = 0;
            public int XElem = -1;
            public int YElem = -1;
            public int ZElem = -1;
            public int NXElem = -1;
            public int NYElem = -1;
            public int NZElem = -1;
            public int UElem = -1;
            public int VElem = -1;
            public int VertexPropCount = 0;
            public int IndElem = -1;

            public bool HasVertices => XElem >= 0 && YElem >= 0 && ZElem >= 0;
            public bool HasNormals => NXElem >= 0 && NYElem >= 0 && NZElem >= 0;
            public bool HasUVs => UElem >= 0 && VElem >= 0;
            public bool HasIndices => IndElem >= 0;

            public string Method = "ascii";
            public bool IsAscii => Method == "ascii";
            public bool IsBinary => !IsAscii;
            public bool IsBigEndian => IsBinary && Method == "binary_big_endian";
        }

        /// <summary>
        /// Returns true if the method of the data region is feasible
        /// </summary>
        private static bool IsAllowedMethod(string method) {
            return method == "ascii" || method == "binary_little_endian" || method == "binary_big_endian";
        }

        /// <summary>
        /// Returns true if the counter type for lists is supported
        /// </summary>
        private static bool IsAllowedVertCountType(string type) {
            return type == "char" || type == "uchar" || type == "int8" || type == "uint8";
        }

        /// <summary>
        /// Returns true if the index type for lists is supported
        /// </summary>
        private static bool IsAllowedVertIndType(string type) {
            return type == "int" || type == "uint";
        }

        /// <summary>
        /// Will parse the header and populate the PlyHeader structure
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        /// <returns>List of errors</returns>
        private static PlyHeader ParsePlyHeader(MixReader stream) {
            bool eos = false;
            string magic = stream.ReadLineAsString(ref eos);
            if (magic != "ply") {
                Log.Error("Trying to load invalid .ply file");
                return null;
            }

            if (eos) {
                Log.Error("Trying to load empty .ply file");
                return null;
            }

            PlyHeader header = new();
            int facePropCounter = 0;
            while (!eos) {
                string line = stream.ReadLineAsString(ref eos);
                string[] parts = line.Split();
                if (parts.Length == 0)
                    continue;

                string action = parts[0];
                if (action == "comment") {
                    continue;
                } else if (action == "format") {
                    if (!IsAllowedMethod(parts[1])) {
                        Log.Warning($"Unknown format {parts[1]} given. Ignoring it");
                        continue;
                    }

                    header.Method = parts[1];
                } else if (action == "element") {
                    string type = parts[1];
                    if (type == "vertex")
                        header.VertexCount = int.Parse(parts[2]);
                    else if (type == "face")
                        header.FaceCount = int.Parse(parts[2]);
                    else
                        Log.Warning($"Unknown element type {type}");
                } else if (action == "property") {
                    string type = parts[1];
                    if (type == "float") {
                        string name = parts[2];
                        if (name == "x")
                            header.XElem = header.VertexPropCount;
                        else if (name == "y")
                            header.YElem = header.VertexPropCount;
                        else if (name == "z")
                            header.ZElem = header.VertexPropCount;
                        else if (name == "nx")
                            header.NXElem = header.VertexPropCount;
                        else if (name == "ny")
                            header.NYElem = header.VertexPropCount;
                        else if (name == "nz")
                            header.NZElem = header.VertexPropCount;
                        else if (name == "u")
                            header.UElem = header.VertexPropCount;
                        else if (name == "v")
                            header.VElem = header.VertexPropCount;
                        ++header.VertexPropCount;
                    } else if (type == "list") {
                        ++facePropCounter;

                        string countType = parts[2];
                        string indType = parts[3];
                        string name = parts[4];

                        if (!IsAllowedVertCountType(countType)) {
                            Log.Error($"Only 'property list uchar int' is supported. Ignoring {countType}");
                            continue;
                        }

                        if (!IsAllowedVertIndType(indType)) {
                            Log.Error($"Only 'property list uchar int' is supported. Ignoring {indType}");
                            continue;
                        }

                        if (name == "vertex_indices")
                            header.IndElem = facePropCounter - 1;
                    } else {
                        Log.Error($"Only float or list properties allowed. Ignoring {type}");
                        ++header.VertexPropCount;
                    }
                } else if (action == "end_header") {
                    break;
                } else {
                    Log.Warning($"Unknown header entry {action}");
                }
            }

            return header;
        }

        /// <summary>
        /// Will parse the whole file, starting with the header and following up with the data region
        /// </summary>
        private static Mesh ParsePlyFile(MixReader stream, MeshLoadingOptions options) {
            PlyHeader header = ParsePlyHeader(stream);
            if (header == null) return null;

            if (!header.HasVertices || !header.HasIndices) {
                Log.Error("Reached end of stream without data");
                return null;
            }

            // Setup reader
            IDataReader reader;
            if (header.IsBinary) {
                reader = new BinaryDataReader(header, stream);
            } else {
                reader = new AsciiDataReader(header, stream);
            }

            Mesh mesh = new();

            bool useNormals = !options.IgnoreNormals && header.HasNormals;
            bool useTexCoords = !options.IgnoreTexCoords && header.HasUVs;

            // Reserve memory
            mesh.Vertices.Capacity = header.VertexCount;
            if (useNormals) mesh.Normals.Capacity = header.VertexCount;
            if (useTexCoords) mesh.TexCoords.Capacity = header.VertexCount;

            // Read per vertex stuff
            for (int i = 0; i < header.VertexCount; ++i) {
                (Vector3 vertex, Vector3 normal, Vector2 tex) = reader.ReadPerVertexLine();

                mesh.Vertices.Add(vertex);
                if (useNormals) mesh.Normals.Add(normal);
                if (useTexCoords) mesh.TexCoords.Add(tex);
            }

            // Read per face indices
            if (header.IndElem != 0) {
                Log.Warning("No support for multiple face properties. Assuming first entry to be the list of indices");
            }

            List<Face> faces = new();
            for (int i = 0; i < header.FaceCount; ++i) {
                faces.Add(reader.ReadFaceLine());
            }
            mesh.Indices = ToTriangleList(faces);

            return mesh;
        }
    }
}
