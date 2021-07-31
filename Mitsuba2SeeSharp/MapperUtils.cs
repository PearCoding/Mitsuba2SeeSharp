using System;
using System.Diagnostics;
using System.IO;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    public static class MapperUtils {
        public static SeeTransform ExtractTransform(SceneObject obj, Options options, string key = "to_world") {
            if (obj.Properties.ContainsKey(key)) {
                double[] transform = obj.Properties[key].GetTransform();
                SeeTransform seeTransform = new() { matrix = new() };
                Debug.Assert(transform.Length == seeTransform.matrix.elements.Length);

                for (int i = 0; i < transform.Length; ++i)
                    seeTransform.matrix.elements[i] = (float)transform[i];

                return seeTransform;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the given transform is identity
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static bool IsTransformIdentity(SeeTransform transform) {
            for (int i = 0; i < 4; ++i) {
                for (int j = 0; j < 4; ++j) {
                    float expected = i == j ? 1 : 0;
                    if (MathF.Abs(transform.matrix.elements[i * 4 + j] - expected) > 1e-5f) return false;
                }
            }
            return true;
        }

        // Create filename from given path to a file and the output
        // The file will be absolute
        public static string CreatePlyPath(string path, Options options) {
            string name = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(options.ActualOutput);
            string meshdir = Path.Join(directory, "meshes");

            Directory.CreateDirectory(meshdir);

            return Path.Join(meshdir, Path.ChangeExtension(name, ".ply"));
        }

        public static string ExtractFilename(SceneObject obj, Options options, string key = "filename") {
            if (obj.Properties.ContainsKey("filename"))
                return MakeItRelative(obj.Properties["filename"].GetString(), options);
            else
                return null;
        }

        public static string ExtractFilenameAbsolute(SceneObject obj, Options options, string key = "filename") {
            if (obj.Properties.ContainsKey("filename"))
                return MakeItAbsolute(obj.Properties["filename"].GetString(), options);
            else
                return null;
        }

        public static string MakeItRelative(string path, Options options) {
            if (!Path.IsPathRooted(path))
                return path;

            string directory = Path.GetDirectoryName(options.ActualOutput);
            if (directory == null || directory == "")
                return path;
            return Path.GetRelativePath(directory, path);
        }

        public static string MakeItAbsolute(string path, Options options) {
            if (Path.IsPathRooted(path))
                return path;

            string directory = Path.GetDirectoryName(options.ActualOutput);
            return Path.Join(directory, path);
        }

        public static string PathToUnix(string path) {
            return path.Replace("\\", "/");
        }
    }
}
