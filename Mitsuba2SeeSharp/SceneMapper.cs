using System.Collections.Generic;
using System.IO;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    public class LoadContext {
        public Options Options;
        public SeeScene Scene = new();
        public HashSet<string> MaterialNames = new();
    }

    public static class SceneMapper {
        public static SeeScene Map(Scene scene, Options options) {
            LoadContext ctx = new() { Options = options };
            ctx.Scene.name = string.Format("{0}_{1}_{2}_{3}", Path.GetFileNameWithoutExtension(options.Input), scene.MajorVersion, scene.MinorVersion, scene.PatchVersion);

            foreach (SceneObject child in scene.AnonymousChildren) {
                switch (child.Type) {
                    case ObjectType.Sensor:
                        HandleSensor(ref ctx, child);
                        break;
                    case ObjectType.Bsdf:
                        HandleBsdf(ref ctx, child);
                        break;
                    case ObjectType.Shape:
                        HandleShape(ref ctx, child);
                        break;
                    case ObjectType.Emitter:
                        // TODO
                        break;
                    case ObjectType.Texture:
                    case ObjectType.Integrator:
                        // Silently ignore
                        break;
                    default:
                        Log.Warning("No support for " + child.Type.ToString() + " in root");
                        break;
                }
            }

            return ctx.Scene;
        }

        private static void HandleSensor(ref LoadContext ctx, SceneObject sensor) {
            string suffix = ctx.Scene.cameras.Count == 0 ? "" : ctx.Scene.cameras.Count.ToString();

            // Setup camera transform
            SeeTransform seeTransform = MapperUtils.ExtractTransform(sensor, ctx.Options);
            seeTransform.name = "__camera" + suffix;
            ctx.Scene.transforms.Add(seeTransform);

            // Setup actual camera
            SeeCamera seeCamera = new();
            seeCamera.name = "camera" + suffix;
            seeCamera.transform = "__camera" + suffix;
            seeCamera.type = sensor.PluginType;

            sensor.Properties.TryGetValue("fov", out Property fovProperty);
            if (fovProperty != null && fovProperty.Type == PropertyType.Number) {
                seeCamera.fov = (float)fovProperty.GetNumber();
            } else {
                Log.Warning("Camera missing fov parameter. Setting it to 60");
                seeCamera.fov = 60;
            }

            ctx.Scene.cameras.Add(seeCamera);
        }

        private static void HandleBsdf(ref LoadContext ctx, SceneObject bsdf) {
            SeeMaterial mat = MaterialMapper.Map(bsdf, ref ctx);
            if (mat != null) {
                ctx.Scene.materials.Add(mat);
                ctx.MaterialNames.Add(mat.name);
            }
        }

        private static void HandleShape(ref LoadContext ctx, SceneObject shape) {
            SeeMesh mesh = ShapeMapper.Map(shape, ref ctx);
            if (mesh != null) {
                mesh.name = string.Format("mesh_{0}", ctx.Scene.objects.Count); // Mitsuba does not require names for shapes 
                ctx.Scene.objects.Add(mesh);
            }
        }
    }
}
