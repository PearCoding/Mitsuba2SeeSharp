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

        public string InputDir => Path.GetDirectoryName(Options.Input);
        public string OutputDir => Path.GetDirectoryName(Options.ActualOutput);


        /// <summary>
        /// Create filename from given path to a file and the output
        /// The file will be absolute
        /// </summary>
        public string RequestPlyPath(string path) {
            string name = Path.GetFileName(path);
            string directory = OutputDir;
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
            string directory = OutputDir;
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

        public string MakeItRelative(string path, string base_path) {
            // if (!Path.IsPathRooted(path))
            //     return path;
            return Path.GetRelativePath(base_path, path);
        }

        public string MakeItRelativeInput(string path) {
            string directory = InputDir;
            if (directory == null || directory == "")
                return path;
            return MakeItRelative(path, directory);
        }

        public string MakeItRelativeOutput(string path) {
            string directory = OutputDir;
            if (directory == null || directory == "")
                return path;
            return MakeItRelative(path, directory);
        }

        public string MakeItAbsolute(string path, string base_path) {
            if (Path.IsPathRooted(path))
                return path;
            return Path.Join(base_path, path);
        }

        public string MakeItAbsoluteInput(string path) {
            return MakeItAbsolute(path, InputDir);
        }

        public string MakeItAbsoluteOutput(string path) {
            return MakeItAbsolute(path, OutputDir);
        }

        public static string PathToUnix(string path) {
            return path.Replace("\\", "/");
        }

        public string PrepareFilename(string path) {
            return PathToUnix(MakeItRelativeOutput(path));
        }

        public string CopyImage(string filename) {
            if (Path.GetExtension(filename) == ".exr") {
                var layers = ImageBase.LoadLayersFromFile(filename);
                if (layers.Count == 0) {
                    Log.Error($"Could not load {filename}");
                    return "";
                }

                string filename2 = RequestImagePath(filename);
                ImageBase.WriteLayeredExr(filename2, layers.Select(p => (p.Key, p.Value)).ToArray());

                return PrepareFilename(filename2);
            } else {
                string filename2 = RequestImagePath(filename);
                File.Copy(filename, filename2, true);
                return PrepareFilename(filename2);
            }
        }
    }
}
