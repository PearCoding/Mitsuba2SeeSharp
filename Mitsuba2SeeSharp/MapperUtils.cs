using System;
using System.Diagnostics;
using System.IO;
using TinyParserMitsuba;
using System.Linq;
using System.Numerics;

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

        internal static Vector3 GetVector(SceneObject obj, string key, Vector3 def) {
            if (obj.Properties.ContainsKey(key)) {
                if (obj.Properties[key].Type == PropertyType.Vector) {
                    var arr = obj.Properties[key].GetVector();
                    return new((float)arr[0], (float)arr[1], (float)arr[2]);
                }
            }

            return def;
        }

        internal static bool GetBool(SceneObject obj, string key, bool def) {
            if (obj.Properties.ContainsKey(key)) {
                return obj.Properties[key].GetBool(def);
            }

            return def;
        }

        internal static int GetInteger(SceneObject obj, string key, int def) {
            if (obj.Properties.ContainsKey(key)) {
                return (int)obj.Properties[key].GetInteger(def);
            }

            return def;
        }

        internal static float GetNumber(SceneObject obj, string key, float def) {
            if (obj.Properties.ContainsKey(key)) {
                return (float)obj.Properties[key].GetNumber(def);
            }

            return def;
        }

        internal static string GetString(SceneObject obj, string key, string def) {
            if (obj.Properties.ContainsKey(key)) {
                return obj.Properties[key].GetString(def);
            }

            return def;
        }

        public static string ExtractFilename(SceneObject obj, LoadContext ctx, string key = "filename") {
            if (obj.Properties.ContainsKey("filename"))
                return ctx.MakeItRelative(obj.Properties["filename"].GetString());
            else
                return null;
        }

        public static string ExtractFilenameAbsolute(SceneObject obj, LoadContext ctx, string key = "filename") {
            if (obj.Properties.ContainsKey("filename"))
                return ctx.MakeItAbsolute(obj.Properties["filename"].GetString());
            else
                return null;
        }
    }
}
