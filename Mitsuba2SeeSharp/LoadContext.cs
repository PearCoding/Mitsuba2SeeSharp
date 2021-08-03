using System.Collections.Generic;
using System.IO;

namespace Mitsuba2SeeSharp {
    public class LoadContext {
        public Options Options;
        public SeeScene Scene = new();
        public Dictionary<string, int> MaterialRefs = new(); // Reference counting
        public List<string> MeshFiles = new();

        /// <summary>
        /// Create filename from given path to a file and the output
        /// The file will be absolute
        /// </summary>
        public string RequestPlyPath(string path, Options options) {
            string name = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(options.ActualOutput);
            string meshdir = Path.Join(directory, options.MeshDirectory);

            Directory.CreateDirectory(meshdir);

            int counter = 2;
            string newpath = Path.Join(meshdir, Path.ChangeExtension(name, ".ply"));
            while (MeshFiles.Contains(newpath)) {
                newpath = Path.Join(meshdir, Path.GetFileNameWithoutExtension(name) + $"_{counter++}.ply");
            }

            MeshFiles.Add(newpath);

            return newpath;
        }
    }
}
