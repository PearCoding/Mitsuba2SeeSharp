using System;
using System.IO;
using System.Linq;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {

    public static class SceneMapper {
        public static SeeScene Map(Scene scene, Options options) {
            LoadContext ctx = new() { Options = options };
            ctx.Scene.name = string.Format("{0}_{1}_{2}_{3}", Path.GetFileNameWithoutExtension(options.Input), scene.MajorVersion, scene.MinorVersion, scene.PatchVersion);

            foreach (SceneObject child in scene.AnonymousChildren) {
                HandleRootElement(ref ctx, child);
            }

            foreach (System.Collections.Generic.KeyValuePair<string, SceneObject> pair in scene.NamedChildren) {
                HandleRootElement(ref ctx, pair.Value);
            }

            if (!options.KeepUnusedMaterials) {
                // Remove non-referenced materials
                foreach (System.Collections.Generic.KeyValuePair<string, int> pair in ctx.MaterialRefs.Where(p => p.Value == 0)) {
                    ctx.Scene.materials.RemoveAll(m => m.name == pair.Key);
                }
            }

            return ctx.Scene;
        }

        private static void HandleRootElement(ref LoadContext ctx, SceneObject child) {
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
                    EmitterMapper.Setup(child, ref ctx);// Only care about envmap
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

        private static void HandleSensor(ref LoadContext ctx, SceneObject sensor) {
            string suffix = ctx.Scene.cameras.Count == 0 ? "" : ctx.Scene.cameras.Count.ToString();

            // Setup camera transform
            SeeTransform seeTransform = MapperUtils.ExtractTransform(sensor, ctx.Options);

            // SeeSharp looks at -Z, Mitsuba is +Z
            seeTransform.FlipX();
            seeTransform.FlipZ();

            seeTransform.name = "__camera" + suffix;
            ctx.Scene.transforms.Add(seeTransform);

            // Setup actual camera
            SeeCamera seeCamera = new();
            seeCamera.name = "camera" + suffix;
            seeCamera.transform = "__camera" + suffix;
            seeCamera.type = sensor.PluginType;

            // Get fov information
            seeCamera.fov = MapperUtils.GetNumber(sensor, "fov", 60);
            string fovAxis = MapperUtils.GetString(sensor, "fov_axis", "x");

            // Extract film info for further computations
            int width = 1;
            int height = 1;
            foreach (SceneObject child in sensor.AnonymousChildren) {
                if (child.Type == ObjectType.Film) {
                    width = MapperUtils.GetInteger(child, "width", width);
                    height = MapperUtils.GetInteger(child, "height", height);
                }
            }

            float aspectRatio = height / (float)width;

            if (fovAxis == "smaller") fovAxis = width < height ? "x" : "y";
            else if (fovAxis == "larger") fovAxis = width < height ? "y" : "x";

            if (fovAxis == "x") {
                // Map horizontal to vertical
                float angle = seeCamera.fov * MathF.PI / 180;
                angle = 2 * MathF.Atan(aspectRatio * MathF.Tan(angle / 2));
                seeCamera.fov = angle * 180 / MathF.PI;
            }
            // TODO: diagonal?

            ctx.Scene.cameras.Add(seeCamera);
        }

        private static void HandleBsdf(ref LoadContext ctx, SceneObject bsdf) {
            SeeMaterial mat = MaterialMapper.Map(bsdf, ref ctx);
            if (mat != null) {
                ctx.Scene.materials.Add(mat);
                ctx.MaterialRefs.Add(mat.name, 0);
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
