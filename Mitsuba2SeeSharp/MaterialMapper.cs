using System.IO;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp
{
    public static class MaterialMapper
    {
        public static SeeMaterial Map(SceneObject bsdf, Options options, string overrideID = "")
        {
            string id = overrideID == "" ? bsdf.ID : overrideID;
            if (id == "")
            {
                Log.Error("Given " + bsdf.PluginType + " has no id");
                return null;
            }

            SeeMaterial mat = null;
            if (bsdf.PluginType == "twosided")
            {
                // Silently ignore
                return Map(bsdf.AnonymousChildren[0], options, id);
            }
            else if (bsdf.PluginType == "diffuse")
            {
                mat = new() { name = id, type = "diffuse" };
                mat.baseColor = ExtractCT(bsdf, "reflectance", options);
                mat.roughness = 1;
            }
            else if (bsdf.PluginType == "dielectric" || bsdf.PluginType == "thindielectric" || bsdf.PluginType == "roughdielectric")
            {
                float alpha_def = bsdf.PluginType == "roughdielectric" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = SeeColorOrTexture.White;
                mat.IOR = ExtractNumber(bsdf, "int_ior", 1.5046f);
                mat.roughness = ExtractNumber(bsdf, "alpha", alpha_def);
                mat.specularTint = 1;
                mat.specularTransmittance = ExtractNumber(bsdf, "specular_transmittance", 1.0f);

                mat.thin = bsdf.PluginType == "thindielectric";
            }
            else if (bsdf.PluginType == "conductor" || bsdf.PluginType == "roughconductor")
            {
                float alpha_def = bsdf.PluginType == "roughconductor" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "specular_reflectance", options);
                mat.IOR = ExtractNumber(bsdf, "eta", 4.9f);
                mat.roughness = ExtractNumber(bsdf, "alpha", alpha_def);
                mat.metallic = 1;
            }
            else if (bsdf.PluginType == "plastic" || bsdf.PluginType == "roughplastic")
            {
                float alpha_def = bsdf.PluginType == "roughplastic" ? 0.5f : 0.5f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "diffuse_reflectance", options);
                mat.IOR = ExtractNumber(bsdf, "int_ior", 1.49f);
                mat.roughness = ExtractNumber(bsdf, "alpha", alpha_def);
                mat.metallic = 0;
            }
            else
            {
                Log.Error("Currently no support for " + bsdf.PluginType + " type of bsdfs");
            }

            return mat;
        }

        private static SeeColorOrTexture ExtractCT(SceneObject obj, string key, Options options)
        {
            SeeColorOrTexture ct = new();
            if (obj.Properties.ContainsKey(key))
            {
                ct.type = "rgb";

                Property prop = obj.Properties[key];
                ct.value = ExtractColor(prop);
                if (ct.value == null)
                    return null;
            }
            else if (obj.NamedChildren.ContainsKey(key))// Texture
            {
                ct.type = "texture";

                SceneObject tex = obj.NamedChildren[key];
                string filename = ExtractTexture(tex);
                if (filename == null || filename == "")
                    return null;

                ct.filename = MapperUtils.MakeItRelative(filename, options);
            }

            return ct;
        }

        private static SeeVector ExtractColor(Property prop)
        {
            if (prop.Type == PropertyType.Color)
            {
                var val = prop.GetColor();
                return new() { x = (float)val[0], y = (float)val[0], z = (float)val[0] };
            }
            else
            {
                Log.Error("Given color property has type " + prop.Type.ToString() + " which is currently not supported");
                return null;
            }
        }

        private static string ExtractTexture(SceneObject obj)
        {
            if (obj.Type != ObjectType.Texture)
            {
                Log.Error("No color or texture given for color property");
                return null;
            }
            if (obj.PluginType != "bitmap")
            {
                Log.Error("Currently only textures of type bitmap are supported");
                return null;
            }

            Property filename = obj.Properties["filename"];
            return filename.GetString();
        }

        private static float ExtractNumber(SceneObject obj, string key, float def)
        {
            if (obj.Properties.ContainsKey(key))
            {
                Property prop = obj.Properties[key];
                return (float)prop.GetNumber((double)def);
            }
            return def;
        }
    }
}
