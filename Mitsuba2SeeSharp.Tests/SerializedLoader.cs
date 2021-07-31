using NUnit.Framework;
using System.IO;

namespace Mitsuba2SeeSharp.Tests {
    public class SerializedLoaderTests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void Load() {
            Mesh mesh = SerializedLoader.ParseFile(Path.Join(TestContext.CurrentContext.TestDirectory, "data", "bidir.serialized"));
            Assert.NotNull(mesh);
            Assert.AreEqual(1083, mesh.Vertices.Count);
            Assert.AreEqual(2162, mesh.FaceCount);
        }
    }
}