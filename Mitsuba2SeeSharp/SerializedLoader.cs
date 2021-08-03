using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mitsuba2SeeSharp {
    /// <summary>
    /// Mitsuba serialized loader mesh
    /// </summary>
    public static class SerializedLoader {
        /// <summary>
        /// Flags given for each shape in the .serialized file
        /// </summary>
        [Flags]
        private enum MeshFlags : int {
            VertexNormals = 0x0001,
            TexCoords = 0x0002,
            VertexColors = 0x0008,
            FaceNormals = 0x0010,
            Float = 0x1000,
            Double = 0x2000
        };


        /// <summary>
        /// Loads .serialized file
        /// </summary>
        public static Mesh ParseFile(string filename, MeshLoadingOptions options, int shape = 0) {
            using FileStream file = File.OpenRead(filename);
            return ParseSerializedFile(file, options, shape);
        }

        /// <summary>
        /// Contains information about one particular shape
        /// </summary>
        private class ShapeInfo {
            public ulong Start;
            public ulong End;
        }

        /// <summary>
        /// Essential informations from a .serialized file
        /// </summary>
        private class SerializedInfo {
            public int Identificator = 0;
            public int Version = 0;
            public int ShapeCount = 0;
            public List<ShapeInfo> ShapeInfos = new();
        }

        const long HeaderSize = sizeof(UInt16) * 2;

        /// <summary>
        /// Will parse the file and populate the SerializedInfo structure
        /// </summary>
        private static SerializedInfo ParseInfo(FileStream stream) {
            SerializedInfo info = new();

            BinaryReader reader = new(stream, Encoding.ASCII, true);
            info.Identificator = reader.ReadUInt16();
            info.Version = reader.ReadUInt16();

            if (info.Identificator != 0x041C) {
                Log.Error("Trying to load invalid .serialized file");
                return null;
            }

            if (info.Version < 3) {
                Log.Error($"Given .serialized file has an insufficient version number {info.Version}");
                return null;
            }

            stream.Seek(-sizeof(UInt32), SeekOrigin.End);
            info.ShapeCount = (int)reader.ReadUInt32();

            ulong[] shapeStarts = new ulong[info.ShapeCount];
            for (int i = 0; i < info.ShapeCount; ++i) {
                if (info.Version >= 4) {
                    stream.Seek(-(sizeof(UInt32) + sizeof(UInt64) * (info.ShapeCount - i)), SeekOrigin.End);
                    shapeStarts[i] = reader.ReadUInt64();
                } else {
                    stream.Seek(-(sizeof(UInt32) + sizeof(UInt32) * (info.ShapeCount - i)), SeekOrigin.End);
                    shapeStarts[i] = reader.ReadUInt32();
                }
            }

            for (int i = 0; i < shapeStarts.Length; ++i) {
                ulong end = i < shapeStarts.Length - 1 ? shapeStarts[i + 1] : (ulong)stream.Length;
                info.ShapeInfos.Add(new() { Start = shapeStarts[i], End = end });
            }

            stream.Seek(2 * sizeof(UInt16), SeekOrigin.Begin);
            return info;
        }

        /// <summary>
        /// Will parse the whole file, starting with the structure and following up with the data region
        /// </summary>
        private static Mesh ParseSerializedFile(FileStream stream, MeshLoadingOptions options, int shape) {
            SerializedInfo info = ParseInfo(stream);
            if (info == null) return null;

            if (info.ShapeCount == 0) {
                Log.Error("Reached end of stream without data");
                return null;
            }

            if (shape >= info.ShapeCount) {
                Log.Error("Invalid shape index given");
                return null;
            }

            ShapeInfo shapeInfo = info.ShapeInfos[shape];

            // Go to the start of the compressed data
            stream.Seek(HeaderSize + (long)shapeInfo.Start, SeekOrigin.Begin);

            using InflaterInputStream decompressionStream = new(stream);
            decompressionStream.IsStreamOwner = false;

            return ParseDeflatedData(decompressionStream, info, options);
        }

        private static Mesh ParseDeflatedData(InflaterInputStream stream, SerializedInfo info, MeshLoadingOptions options) {
            BinaryReader reader = new(stream, Encoding.ASCII, true);
            MeshFlags flags = (MeshFlags)reader.ReadInt32();

            // Ignore shape name
            if (info.Version >= 4) {
                while (reader.ReadByte() != 0) ;
            }

            ulong vertexCount = reader.ReadUInt64();
            ulong triCount = reader.ReadUInt64();

            if (vertexCount == 0 || triCount == 0) {
                Log.Error("Shape has no data");
                return null;
            }

            bool useNormals = !options.IgnoreNormals && (flags & MeshFlags.VertexNormals) == MeshFlags.VertexNormals;
            bool useTexCoords = !options.IgnoreTexCoords && (flags & MeshFlags.TexCoords) == MeshFlags.TexCoords;

            Mesh mesh = new();
            mesh.Vertices.Capacity = (int)vertexCount;
            if (useNormals) mesh.Normals.Capacity = (int)vertexCount;
            if (useTexCoords) mesh.TexCoords.Capacity = (int)vertexCount;

            // Vertices
            if ((flags & MeshFlags.Double) == MeshFlags.Double) {
                for (ulong i = 0; i < vertexCount; ++i) {
                    double x = reader.ReadDouble();
                    double y = reader.ReadDouble();
                    double z = reader.ReadDouble();
                    mesh.Vertices.Add(new() { X = (float)x, Y = (float)y, Z = (float)z });
                }
            } else {
                for (ulong i = 0; i < vertexCount; ++i) {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    mesh.Vertices.Add(new() { X = x, Y = y, Z = z });
                }
            }

            // Normals
            if ((flags & MeshFlags.VertexNormals) == MeshFlags.VertexNormals) {
                if ((flags & MeshFlags.Double) == MeshFlags.Double) {
                    for (ulong i = 0; i < vertexCount; ++i) {
                        double x = reader.ReadDouble();
                        double y = reader.ReadDouble();
                        double z = reader.ReadDouble();
                        if (useNormals) mesh.Normals.Add(new() { X = (float)x, Y = (float)y, Z = (float)z });
                    }
                } else {
                    for (ulong i = 0; i < vertexCount; ++i) {
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        float z = reader.ReadSingle();
                        if (useNormals) mesh.Normals.Add(new() { X = x, Y = y, Z = z });
                    }
                }
            }

            // Texcoords
            if ((flags & MeshFlags.TexCoords) == MeshFlags.TexCoords) {
                if ((flags & MeshFlags.Double) == MeshFlags.Double) {
                    for (ulong i = 0; i < vertexCount; ++i) {
                        double x = reader.ReadDouble();
                        double y = reader.ReadDouble();
                        if (useTexCoords) mesh.TexCoords.Add(new() { X = (float)x, Y = (float)y });
                    }
                } else {
                    for (ulong i = 0; i < vertexCount; ++i) {
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        if (useTexCoords) mesh.TexCoords.Add(new() { X = x, Y = y });
                    }
                }
            }

            // Vertex colors (ignored)
            if ((flags & MeshFlags.VertexColors) == MeshFlags.VertexColors) {
                if ((flags & MeshFlags.Double) == MeshFlags.Double) {
                    for (ulong i = 0; i < vertexCount * 3; ++i) {
                        reader.ReadDouble();
                    }
                } else {
                    for (ulong i = 0; i < vertexCount * 3; ++i) {
                        reader.ReadSingle();
                    }
                }
            }

            // Indices
            mesh.Indices.Capacity = (int)triCount * 3;
            if (vertexCount > 0xFFFFFFFF) {
                Log.Warning("Serialized reader can read 64bit indices but will cast them down to 32bit. Loss of information unavoidable");
                for (ulong i = 0; i < triCount * 3; ++i) {
                    ulong ind = reader.ReadUInt64();
                    if (ind >= vertexCount) {
                        Log.Error("Given mesh has invalid indices");
                        return null;
                    }
                    mesh.Indices.Add((int)ind);
                }
            } else {
                for (ulong i = 0; i < triCount * 3; ++i) {
                    uint ind = reader.ReadUInt32();
                    if (ind >= vertexCount) {
                        Log.Error("Given mesh has invalid indices");
                        return null;
                    }
                    mesh.Indices.Add((int)ind);
                }
            }

            // Completly ignore possible face normals

            return mesh;
        }
    }
}
