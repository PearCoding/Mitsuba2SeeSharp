using System.Diagnostics;
using System.IO;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp
{
    public static class SceneMapper
    {
        public static SeeScene Map(Scene scene, Options options)
        {
            SeeScene mappedScene = new();
            mappedScene.name = string.Format("{0}_{1}_{2}_{3}", Path.GetFileNameWithoutExtension(options.Input), scene.MajorVersion, scene.MinorVersion, scene.PatchVersion);

            foreach (var child in scene.AnonymousChildren)
            {
                switch (child.Type)
                {
                    case ObjectType.Sensor:
                        HandleSensor(ref mappedScene, child, options);
                        break;
                    case ObjectType.Bsdf:
                        HandleBsdf(ref mappedScene, child, options);
                        break;
                    case ObjectType.Shape:
                        HandleShape(ref mappedScene, child, options);
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

            return mappedScene;
        }

        private static void HandleSensor(ref SeeScene scene, SceneObject sensor, Options options)
        {
            string suffix = scene.cameras.Count == 0 ? "" : scene.cameras.Count.ToString();

            // Setup camera transform
            var seeTransform = MapperUtils.ExtractTransform(sensor, options);
            seeTransform.name = "__camera" + suffix;
            scene.transforms.Add(seeTransform);

            // Setup actual camera
            SeeCamera seeCamera = new();
            seeCamera.name = "camera" + suffix;
            seeCamera.transform = "__camera" + suffix;
            seeCamera.type = sensor.PluginType;

            sensor.Properties.TryGetValue("fov", out Property fovProperty);
            if (fovProperty != null && fovProperty.Type == PropertyType.Number)
            {
                seeCamera.fov = (float)fovProperty.GetNumber();
            }
            else
            {
                Log.Warning("Camera missing fov parameter. Setting it to 60");
                seeCamera.fov = 60;
            }

            scene.cameras.Add(seeCamera);
        }

        private static void HandleBsdf(ref SeeScene scene, SceneObject bsdf, Options options)
        {
            SeeMaterial mat = MaterialMapper.Map(bsdf, options);
            if (mat != null) scene.materials.Add(mat);
        }

        private static void HandleShape(ref SeeScene scene, SceneObject shape, Options options)
        {
            SeeMesh mesh = ShapeMapper.Map(shape, options);
            if (mesh != null)
            {
                mesh.name = string.Format("mesh_{0}", scene.objects.Count); // Mitsuba does not require names for shapes 
                scene.objects.Add(mesh);
            }
        }
    }
}
