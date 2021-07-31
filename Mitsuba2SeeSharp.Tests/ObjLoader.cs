using NUnit.Framework;
using System.IO;

namespace Mitsuba2SeeSharp.Tests {
    public class ObjLoaderTests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void Load() {
            Mesh mesh = ObjLoader.ParseFile(Path.Join(TestContext.CurrentContext.TestDirectory, "data", "cbox_smallbox.obj"));
            Assert.NotNull(mesh);
            Assert.AreEqual(24, mesh.Vertices.Count);
            Assert.AreEqual(12, mesh.FaceCount);
        }
    }
}