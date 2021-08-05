using SimpleImageIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mitsuba2SeeSharp {
    public class LoadContext {
        public Options Options;
        public SeeScene Scene = new();
        public Dictionary<string, int> MaterialRefs = new(); // Reference counting
        public List<string> MeshFiles = new();
        public List<string> ImageFiles = new();

        /// <summary>
        /// Create filename from given path to a file and the output
        /// The file will be absolute
        /// </summary>
        public string RequestPlyPath(string path) {
            string name = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(Options.ActualOutput);
            string meshdir = Path.Join(directory, Options.MeshDirectory);

            Directory.CreateDirectory(meshdir);

            int counter = 2;
            string newpath = Path.Join(meshdir, Path.ChangeExtension(name, ".ply"));
            while (MeshFiles.Contains(newpath)) {
                newpath = Path.Join(meshdir, Path.GetFileNameWithoutExtension(name) + $"_{counter++}.ply");
            }

            MeshFiles.Add(newpath);

            return newpath;
        }

        /// <summary>
        /// Create filename from given path to a file and the output
        /// The file will be absolute
        /// </summary>
        public string RequestImagePath(string path) {
            string name = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(Options.ActualOutput);
            string imgdir = Path.Join(directory, Options.ImageDirectory);

            Directory.CreateDirectory(imgdir);

            int counter = 2;
            string newpath = Path.Join(imgdir, name);
            while (ImageFiles.Contains(newpath)) {
                newpath = Path.Join(imgdir, Path.GetFileNameWithoutExtension(name) + $"_{counter++}" + Path.GetExtension(name));
            }

            ImageFiles.Add(newpath);

            return newpath;
        }

        public string MakeItRelative(string path) {
            if (!Path.IsPathRooted(path))
                return path;

            string directory = Path.GetDirectoryName(Options.ActualOutput);
            if (directory == null || directory == "")
                return path;
            return Path.GetRelativePath(directory, path);
        }

        public string MakeItAbsolute(string path) {
            if (Path.IsPathRooted(path))
                return path;

            string directory = Path.GetDirectoryName(Options.ActualOutput);
            return Path.Join(directory, path);
        }

        public static string PathToUnix(string path) {
            return path.Replace("\\", "/");
        }

        public string PrepareFilename(string path) {
            return PathToUnix(MakeItRelative(path));
        }

        public string CopyImage(string filename) {
            var layers = ImageBase.LoadLayersFromFile(filename);
            string filename2 = RequestImagePath(filename);
            ImageBase.WriteLayeredExr(filename2, layers.Select(p => (p.Key, p.Value)).ToArray());

            return PrepareFilename(filename2);
        }
    }
}
