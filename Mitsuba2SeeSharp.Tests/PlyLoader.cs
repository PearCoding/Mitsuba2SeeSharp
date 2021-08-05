using NUnit.Framework;
using System.IO;

namespace Mitsuba2SeeSharp.Tests {
    public class PlyLoaderTests {
        [SetUp]
        public void Setup() {
            Mesh mesh = new();
            mesh.Vertices.Add(new(0, 0, 0));
            mesh.Vertices.Add(new(0, 1, 0));
            mesh.Vertices.Add(new(0, 0, 1));
            mesh.Vertices.Add(new(0, 1, 1));

            mesh.Indices.AddRange(new[] { 0, 1, 2, 0, 2, 3 });

            File.WriteAllBytes("test.ply", mesh.ToPly());
        }

        [Test]
        public void Load() {
            Mesh mesh = PlyLoader.ParseFile("test.ply", new());
            Assert.NotNull(mesh);
            Assert.AreEqual(4, mesh.Vertices.Count);
            Assert.AreEqual(2, mesh.FaceCount);
        }
    }
}