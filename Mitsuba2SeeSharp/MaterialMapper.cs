using TinyParserMitsuba;
using System;

namespace Mitsuba2SeeSharp {
    public static class MaterialMapper {
        public static SeeMaterial Map(SceneObject bsdf, ref LoadContext ctx, string overrideID = "") {
            string id = overrideID == "" ? bsdf.ID : overrideID;
            if (id == "") id = string.Format("__material_{0}", ctx.Scene.materials.Count);

            SeeMaterial mat = null;
            if (bsdf.PluginType == "twosided") {
                // Silently ignore
                return Map(bsdf.AnonymousChildren[0], ref ctx, id);
            } else if (bsdf.PluginType == "diffuse") {
                mat = new() { name = id, type = "diffuse" };
                mat.baseColor = ExtractCT(bsdf, "reflectance", ctx.Options);
                mat.roughness = 1;
            } else if (bsdf.PluginType == "dielectric" || bsdf.PluginType == "thindielectric" || bsdf.PluginType == "roughdielectric") {
                float alpha_def = bsdf.PluginType == "roughdielectric" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = SeeColorOrTexture.White;
                mat.IOR = ExtractNumber(bsdf, "int_ior", 1.5046f);
                mat.roughness = MathF.Sqrt(ExtractNumber(bsdf, "alpha", alpha_def));
                mat.specularTint = 1;
                mat.specularTransmittance = ExtractNumber(bsdf, "specular_transmittance", 1.0f);

                mat.thin = bsdf.PluginType == "thindielectric";
            } else if (bsdf.PluginType == "conductor" || bsdf.PluginType == "roughconductor") {
                float alpha_def = bsdf.PluginType == "roughconductor" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "specular_reflectance", ctx.Options);
                mat.IOR = ExtractNumber(bsdf, "eta", 4.9f);
                mat.roughness = MathF.Sqrt(ExtractNumber(bsdf, "alpha", alpha_def));
                mat.metallic = 1;
            } else if (bsdf.PluginType == "plastic" || bsdf.PluginType == "roughplastic") {
                float alpha_def = bsdf.PluginType == "roughplastic" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "diffuse_reflectance", ctx.Options);
                mat.IOR = ExtractNumber(bsdf, "int_ior", 1.49f);
                mat.roughness = MathF.Sqrt(ExtractNumber(bsdf, "alpha", alpha_def));
                mat.metallic = 0; // TODO: Get anisotropy
            } else {
                Log.Error("Currently no support for " + bsdf.PluginType + " type of bsdfs");
            }

            return mat;
        }

        private static SeeColorOrTexture ExtractCT(SceneObject obj, string key, Options options) {
            SeeColorOrTexture ct = new();
            if (obj.Properties.ContainsKey(key)) {
                ct.type = "rgb";

                Property prop = obj.Properties[key];
                ct.value = MapperUtils.ExtractColor(prop);
                if (ct.value == null)
                    return null;
            } else if (obj.NamedChildren.ContainsKey(key))// Texture
              {
                ct.type = "image";

                SceneObject tex = obj.NamedChildren[key];
                string filename = ExtractTexture(tex);
                if (filename == null || filename == "")
                    return null;

                ct.filename = MapperUtils.MakeItRelative(filename, options);
            } else {
                Log.Error("Invalid color property given");
                ct.type = "rgb";
                ct.value = new() { x = 0, y = 0, z = 0 };
            }

            return ct;
        }

        private static string ExtractTexture(SceneObject obj) {
            if (obj.Type != ObjectType.Texture) {
                Log.Error("No color or texture given for color property");
                return null;
            }
            if (obj.PluginType != "bitmap") {
                Log.Error("Currently only textures of type bitmap are supported");
                return null;
            }

            Property filename = obj.Properties["filename"];
            return filename.GetString();
        }

        private static float ExtractNumber(SceneObject obj, string key, float def) {
            if (obj.Properties.ContainsKey(key)) {
                Property prop = obj.Properties[key];
                return (float)prop.GetNumber((double)def);
            }
            return def;
        }

        public static SeeMaterial CreateBlack(string id) {
            SeeMaterial mat = new() { name = id, type = "diffuse" };
            mat.baseColor = SeeColorOrTexture.Black;
            mat.roughness = 1;
            return mat;
        }
    }
}
