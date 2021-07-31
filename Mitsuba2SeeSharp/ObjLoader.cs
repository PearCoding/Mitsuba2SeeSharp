using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Mitsuba2SeeSharp {
    /// <summary>
    /// Wavefront obj loader mesh
    /// </summary>
    public static class ObjLoader {
        /// <summary>
        /// Index used for hashing
        /// </summary>
        private class Index : IEquatable<Index> {
            public int Vertex { get; }
            public int Normal { get; }
            public int TexCoord { get; }

            public Index(int v, int n, int t) {
                Vertex = v;
                Normal = n;
                TexCoord = t;
            }
            public Index(FaceVertex fv) {
                Vertex = fv.VertexIndex - 1;
                Normal = fv.NormalIndex - 1;
                TexCoord = fv.TextureIndex - 1;
            }

            public override int GetHashCode() {
                return Vertex.GetHashCode() ^ Normal.GetHashCode() ^ TexCoord.GetHashCode();
            }

            public override bool Equals(object obj) {
                if (obj == null || GetType() != obj.GetType()) {
                    return false;
                }
                return Equals((Index)obj);
            }

            public bool Equals(Index other) {
                return other.Vertex.Equals(Vertex) && other.Normal.Equals(Normal) && other.TexCoord.Equals(TexCoord);
            }
        }

        /// <summary>
        /// Loads .obj file
        /// </summary>
        public static Mesh ParseFile(string filename, int shape = 0) {
            ObjLoaderFactory objLoaderFactory = new ObjLoaderFactory();
            IObjLoader objLoader = objLoaderFactory.Create(new MaterialNullStreamProvider());
            using FileStream file = File.OpenRead(filename);
            LoadResult result = objLoader.Load(file);

            if (shape >= result.Groups.Count) {
                Log.Error("Given shape index is not available");
                return null;
            }

            Group group = result.Groups[shape];

            bool hasNormals = result.Normals.Count != 0;
            bool hasTexcoords = result.Textures.Count != 0;

            // Linearize index
            Dictionary<Index, int> mapper = new();
            Dictionary<Index, Vector3> vertices = new();
            Dictionary<Index, Vector3> normals = new();
            Dictionary<Index, Vector2> texcoords = new();
            foreach (Face face in group.Faces) {
                for (int i = 0; i < face.Count; ++i) {
                    Index index = new(face[i]);
                    if (!mapper.TryAdd(index, mapper.Count))
                        continue;// Already registered

                    global::ObjLoader.Loader.Data.VertexData.Vertex vert = result.Vertices[index.Vertex];
                    vertices.Add(index, new(vert.X, vert.Y, vert.Z));

                    if (hasNormals) {
                        global::ObjLoader.Loader.Data.VertexData.Normal norm = result.Normals[index.Normal];
                        normals.Add(index, new(norm.X, norm.Y, norm.Z));
                    }

                    if (hasTexcoords) {
                        global::ObjLoader.Loader.Data.VertexData.Texture tex = result.Textures[index.TexCoord];
                        texcoords.Add(index, new(tex.X, tex.Y));
                    }
                }
            }

            if (vertices.Count == 0) {
                Log.Error("Mesh has no vertices");
                return null;
            }

            // Create mesh skeleton
            Mesh mesh = new();
            mesh.Vertices = vertices.Values.ToList();
            if (hasNormals) mesh.Normals = normals.Values.ToList();
            if (hasTexcoords) mesh.TexCoords = texcoords.Values.ToList();

            bool warned = false;

            // Triangulate and reset indices
            foreach (Face face in group.Faces) {
                // Get actual indices
                int[] indices = new int[face.Count];
                for (int i = 0; i < face.Count; ++i) {
                    Index mindex = new Index(face[i]);
                    indices[i] = mapper[mindex];
                }

                // Triangulate if necessary
                if (indices.Length == 3) {
                    mesh.Indices.AddRange(indices);
                } else if (indices.Length < 3) {
                    if (!warned) {
                        Log.Warning("Mesh contains invalid faces");
                        warned = true;
                    }
                } else // Convex triangulation
                  {
                    int pin = indices[0];
                    for (int i = 2; i < indices.Length; ++i) {
                        mesh.Indices.AddRange(new int[3] { pin, indices[i - 1], indices[i] });
                    }
                }
            }

            return mesh;
        }

    }
}
