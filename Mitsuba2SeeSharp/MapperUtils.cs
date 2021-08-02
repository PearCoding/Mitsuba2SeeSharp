using System;
using System.Diagnostics;
using System.IO;
using TinyParserMitsuba;
using System.Linq;

namespace Mitsuba2SeeSharp {
    public static class MapperUtils {
        public static SeeTransform ExtractTransform(SceneObject obj, Options options, string key = "to_world") {
            if (obj.Properties.ContainsKey(key)) {
                double[] transform = obj.Properties[key].GetTransform();
                SeeTransform seeTransform = new() { matrix = new() };
                Debug.Assert(transform.Length == seeTransform.matrix.elements.Length);

                for (int i = 0; i < transform.Length; ++i)
                    seeTransform.matrix.elements[i] = (float)transform[i];
                seeTransform.matrix.Transpose(); // Mitsuba matrices are the transpose of SeeSharp ones

                return seeTransform;
            } else {
                return null;
            }
        }

        public static SeeVector ExtractColor(Property prop, bool emissive = false) {
            if (prop.Type == PropertyType.Color) {
                double[] val = prop.GetColor();
                return new() { x = (float)val[0], y = (float)val[0], z = (float)val[0] };
            } else if (prop.Type == PropertyType.Spectrum) {
                var spectrum = prop.GetSpectrum();
                if (spectrum.IsUniform) {
                    return SeeRGB.Gray((float)spectrum.Weights[0]).value;
                } else {
                    Log.Warning("Full spectra is not implemented. Mapping to sRGB");
                    float[] wvls = spectrum.Wavelengths.Select(a => (float)a).ToArray();
                    float[] weights = spectrum.Weights.Select(a => (float)a).ToArray();

                    (float x, float y, float z) = SpectralMapper.Eval(wvls, weights, emissive);
                    (float r, float g, float b) = SpectralMapper.MapRGB(x, y, z);
                    return new() { x = r, y = g, z = b };
                }
            } else {// TODO: Blackbody
                Log.Error("Given color property has type " + prop.Type.ToString() + " which is currently not supported");
                return null;
            }
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
