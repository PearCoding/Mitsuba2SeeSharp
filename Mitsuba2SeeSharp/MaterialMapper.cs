using TinyParserMitsuba;
using System;
using System.Linq;

namespace Mitsuba2SeeSharp {
    public static class MaterialMapper {
        public static SeeMaterial Map(SceneObject bsdf, ref LoadContext ctx, string overrideID = "") {
            string id = overrideID == "" ? bsdf.ID : overrideID;
            if (id == "") id = string.Format("__material_{0}", ctx.Scene.materials.Count);

            SeeMaterial mat = null;
            if (bsdf.PluginType == "twosided") {
                // Silently ignore
                return Map(bsdf.AnonymousChildren[0], ref ctx, id);
            } else if (bsdf.PluginType == "bumpmap" || bsdf.PluginType == "normalmap"
                    || bsdf.PluginType == "blendbsdf" || bsdf.PluginType == "mixturebsdf"
                    || bsdf.PluginType == "mask") {
                foreach (var child in bsdf.AnonymousChildren) {
                    if (child.Type == ObjectType.Bsdf) {
                        var special_mat = HandleMaskSpecialCase(bsdf, child, ctx);
                        if (special_mat != null) {
                            mat = special_mat;
                            mat.name = id;
                            return mat;
                        } else {
                            Log.Warning("Currently no support for " + bsdf.PluginType + " bsdfs. Using first inner bsdf instead");
                            return Map(child, ref ctx, id);
                        }
                    }
                }

                Log.Error("Currently no support for " + bsdf.PluginType + " bsdfs");
            } else if (bsdf.PluginType == "diffuse" || bsdf.PluginType == "roughdiffuse") {
                mat = new() { name = id, type = "diffuse" };
                mat.baseColor = ExtractCT(bsdf, "reflectance", ctx);
                if (bsdf.PluginType == "roughdiffuse")
                    (mat.roughness, mat.anisotropic) = ExtractRoughness(bsdf, 0.5f);
                else
                    mat.roughness = 1;
            } else if (bsdf.PluginType == "dielectric" || bsdf.PluginType == "thindielectric" || bsdf.PluginType == "roughdielectric") {
                float alpha_def = bsdf.PluginType == "roughdielectric" ? 0.5f : 0.0f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = SeeColorOrTexture.White;
                mat.IOR = ExtractIOR(bsdf, "int_ior", 1.5046f);

                (mat.roughness, mat.anisotropic) = ExtractRoughness(bsdf, alpha_def);

                mat.specularTint = 1;
                mat.specularTransmittance = MapperUtils.GetNumber(bsdf, "specular_transmittance", 1.0f);

                mat.thin = bsdf.PluginType == "thindielectric";
            } else if (bsdf.PluginType == "conductor" || bsdf.PluginType == "roughconductor") {
                float alpha_def = bsdf.PluginType == "roughconductor" ? 0.5f : 0.0f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "specular_reflectance", ctx, 1);

                if (bsdf.Properties.ContainsKey("material")) {
                    // TODO: No support for kappa?
                    (float eta, _) = ExtractConductor(bsdf, "material");
                    mat.IOR = eta;
                    // The following case might happen due the none material
                    if (mat.IOR == 0) mat.IOR = 1.00001f;
                } else {
                    mat.IOR = ExtractIOR(bsdf, "eta", 4.9f);// TODO: Not really the same as IOR
                }
                (mat.roughness, mat.anisotropic) = ExtractRoughness(bsdf, alpha_def);
                mat.metallic = 1;
            } else if (bsdf.PluginType == "plastic" || bsdf.PluginType == "roughplastic") {
                float alpha_def = bsdf.PluginType == "roughplastic" ? 0.5f : 0.0f;

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "diffuse_reflectance", ctx);
                mat.IOR = MapperUtils.GetNumber(bsdf, "int_ior", 1.49f);
                (mat.roughness, mat.anisotropic) = ExtractRoughness(bsdf, alpha_def);
                mat.metallic = 0;
            } else if (bsdf.PluginType == "phong") {

                mat = new() { name = id, type = "generic" };
                mat.baseColor = ExtractCT(bsdf, "diffuse_reflectance", ctx);
                mat.IOR = 1.49f;
                mat.metallic = 0;

                float exponent = MapperUtils.GetNumber(bsdf, "exponent", 30);
                mat.roughness = MathF.Exp(-exponent);// Only an approximation
            } else {
                Log.Error("Currently no support for " + bsdf.PluginType + " bsdfs");
            }

            return mat;
        }

        private static SeeColorOrTexture ExtractCT(SceneObject obj, string key, LoadContext ctx, float def = 0) {
            SeeColorOrTexture ct = new();
            if (obj.Properties.ContainsKey(key)) {
                ct.type = "rgb";

                Property prop = obj.Properties[key];
                ct.value = MapperUtils.ExtractColor(prop);
                if (ct.value != null)
                    return ct;
            } else if (obj.NamedChildren.ContainsKey(key)) {
                ct.type = "image";// Texture

                SceneObject tex = obj.NamedChildren[key];
                string filename = ExtractTexture(tex);
                if (filename != null && filename != "") {
                    if (ctx.Options.CopyImages)
                        ct.filename = ctx.CopyImage(ctx.MakeItAbsolute(filename));
                    else
                        ct.filename = ctx.PrepareFilename(filename);
                    return ct;
                }
            }

            ct.type = "rgb";
            ct.value = new() { x = def, y = def, z = def };
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

        private static float ExtractIOR(SceneObject obj, string key, float def) {
            if (obj.Properties.ContainsKey(key)) {
                Property prop = obj.Properties[key];
                if (prop.Type == PropertyType.String) {
                    string lookup = prop.GetString();
                    switch (lookup.ToLower()) {
                        case "vacuum": return 1.0f;
                        case "bromine": return 1.661f;
                        case "helium": return 1.00004f;
                        case "water ice": return 1.31f;
                        case "hydrogen": return 1.00013f;
                        case "fused quartz": return 1.458f;
                        case "air": return 1.00028f;
                        case "pyrex": return 1.47f;
                        case "carbon dioxide": return 1.00045f;
                        case "acrylic glass": return 1.49f;
                        case "water": return 1.333f;
                        case "polypropylene": return 1.49f;
                        case "acetone": return 1.36f;
                        case "bk7": return 1.5046f;
                        case "ethanol": return 1.361f;
                        case "sodium chloride": return 1.544f;
                        case "carbon tetrachloride": return 1.461f;
                        case "amber": return 1.55f;
                        case "glycerol": return 1.4729f;
                        case "pet": return 1.575f;
                        case "benzene": return 1.501f;
                        case "diamond": return 2.419f;
                        case "silicone oil": return 1.52045f;
                        default:
                            Log.Error($"Unknown IOR '{lookup}'");
                            return def;
                    }

                } else {
                    return (float)prop.GetNumber(def);
                }
            }
            return def;
        }

        private static (float, float) ExtractConductor(SceneObject obj, string key, string def = "cu") {
            string lookup = def;
            if (obj.Properties.ContainsKey(key)) {
                Property prop = obj.Properties[key];
                if (prop.Type == PropertyType.String)
                    lookup = prop.GetString();
            }

            switch (lookup.ToLower()) {
                // Approximative, extracted from .spd files around 540nm
                // Keep in mind these do not contain color information in our case
                case "ag": return (0.129f, 3.250f);
                case "al": return (1.150f, 6.550f);
                case "au": return (0.402f, 2.540f);
                case "cr": return (2.980f, 4.450f);
                case "cu": return (1.040f, 2.583f);
                case "none": return (0, 1);
                default:
                    Log.Error($"Unknown conductor material '{lookup}'");
                    goto case "cu";
            }
        }

        private static (float, float) ExtractRoughness(SceneObject obj, float def) {
            if (obj.Properties.ContainsKey("alpha")) {
                float roughness = MathF.Sqrt(MapperUtils.GetNumber(obj, "alpha", def));
                return (roughness, 0);
            } else {
                float roughness_x = MathF.Sqrt(MapperUtils.GetNumber(obj, "alpha_u", def));
                float roughness_y = MathF.Sqrt(MapperUtils.GetNumber(obj, "alpha_v", roughness_x * roughness_x));

                if (roughness_x == 0 && roughness_y == 0)
                    return (0, 0);

                float anisotropy = MathF.Abs(roughness_x - roughness_y) / MathF.Max(roughness_x, roughness_y);
                return (MathF.Max(roughness_x, roughness_y), anisotropy);
            }
        }

        public static SeeMaterial CreateBlack(string id) {
            SeeMaterial mat = new() { name = id, type = "diffuse" };
            mat.baseColor = SeeColorOrTexture.Black;
            mat.roughness = 1;
            return mat;
        }

        public static SeeMaterial HandleMaskSpecialCase(SceneObject parent, SceneObject child, LoadContext ctx) {
            if (parent.PluginType == "mask") {
                if (child.PluginType == "diffuse") {
                    SeeMaterial mat = new() { type = "generic" };
                    mat.baseColor = ExtractCT(child, "reflectance", ctx);
                    mat.roughness = 1;
                    mat.thin = true;
                    mat.IOR = 1.001f; // SeeSharp does not allow IOR to be 1
                    if (parent.Properties.ContainsKey("opacity")) {
                        var prop = parent.Properties["opacity"];
                        if (prop.Type == PropertyType.Number)
                            mat.specularTransmittance = (float)prop.GetNumber();
                        else if (prop.Type == PropertyType.Color)
                            mat.specularTransmittance = (float)prop.GetColor().Average();
                        // TODO: Spectral?
                        else
                            return null;// Got texture, give up
                    } else {
                        mat.specularTransmittance = 0.5f;
                    }

                    return mat;
                } else if (child.PluginType == "twosided") {
                    // Skip twosided
                    return HandleMaskSpecialCase(parent, child.AnonymousChildren[0], ctx);
                }
            }

            return null;
        }
    }
}
