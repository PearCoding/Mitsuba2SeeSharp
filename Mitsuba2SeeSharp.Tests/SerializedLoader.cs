using NUnit.Framework;
using System.IO;

namespace Mitsuba2SeeSharp.Tests
{
    public class SerializedLoaderTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Load()
        {
            Mesh mesh = SerializedLoader.ParseFile(Path.Join(TestContext.CurrentContext.TestDirectory, "data", "sponza.serialized"));
            Assert.NotNull(mesh);
            Assert.AreEqual(200, mesh.Vertices.Count);
            Assert.AreEqual(192, mesh.FaceCount);
        }
    }
}