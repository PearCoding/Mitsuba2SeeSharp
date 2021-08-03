using System.IO;
using System.Linq;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    public static class ShapeMapper {
        public static SeeMesh Map(SceneObject shape, ref LoadContext ctx) {
            SeeMesh mesh = new() { type = "ply" };
            if (shape.PluginType == "rectangle") {
                bool flip_normals = false;
                if (shape.Properties.ContainsKey("flip_normals")) {
                    flip_normals = shape.Properties["flip_normals"].GetBool();
                }

                Mesh actualMesh = PrimitiveLoader.CreateRectangle();
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                if (flip_normals)
                    actualMesh.FlipNormals();

                string newpath = MapperUtils.CreatePlyPath($"__rectangle_{ctx.Scene.objects.Count}.ply", ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "serialized") {
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape")) {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                Mesh actualMesh = SerializedLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                // Simple test to prevent useless geometry
                if (actualMesh.SurfaceArea <= 1e-6f) {
                    Log.Error("Given shape has no surface. Ignoring it");
                    return null;
                }

                actualMesh.FlipTexUp();

                string newpath = MapperUtils.CreatePlyPath(filename, ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "obj") {
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape")) {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                Mesh actualMesh = ObjLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                // Simple test to prevent useless geometry
                if (actualMesh.SurfaceArea <= 1e-6f) {
                    Log.Error("Given shape has no surface. Ignoring it");
                    return null;
                }

                actualMesh.FlipTexUp();

                string newpath = MapperUtils.CreatePlyPath(filename, ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "ply") {
                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);

                if (transform != null && !transform.IsIdentity) {
                    string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                    Mesh actualMesh = PlyLoader.ParseFile(filename);
                    if (actualMesh == null)
                        return null;

                    // Simple test to prevent useless geometry
                    if (actualMesh.SurfaceArea <= 1e-6f) {
                        Log.Error("Given shape has no surface. Ignoring it");
                        return null;
                    }

                    actualMesh.FlipTexUp();

                    string newpath = MapperUtils.CreatePlyPath(filename, ctx.Options);
                    File.WriteAllBytes(newpath, actualMesh.ToPly());
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
                } else {
                    // Use file directly
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.ExtractFilename(shape, ctx.Options));
                }
            } else {
                Log.Error("Currently no support for " + shape.PluginType + " type of shapes");
            }

            // Had errors, give up
            if (mesh == null)
                return null;

            // Handle material assosciation
            foreach (SceneObject child in shape.AnonymousChildren) {
                if (child.Type == ObjectType.Bsdf) {
                    string id = child.ID;
                    if (id == "" || !ctx.MaterialRefs.ContainsKey(id)) {
                        SeeMaterial mat = MaterialMapper.Map(child, ref ctx);
                        if (mat != null) {
                            ctx.Scene.materials.Add(mat);
                            ctx.MaterialRefs.Add(mat.name, 1);
                            mesh.material = mat.name;
                        }
                    } else {
                        mesh.material = child.ID;
                        ctx.MaterialRefs[mesh.material] += 1;
                    }
                    break;
                }
            }

            // Handle emitter association
            foreach (SceneObject child in shape.AnonymousChildren) {
                if (child.Type == ObjectType.Emitter) {
                    if (child.PluginType != "area") {
                        Log.Error("Currently no support for " + child.PluginType + " emitter type for shapes");
                        continue;
                    }

                    // Get emission color
                    SeeRGB emission = SeeRGB.Black;
                    if (child.Properties.ContainsKey("radiance")) {
                        var prop = child.Properties["radiance"];
                        var value = MapperUtils.ExtractColor(prop, true);

                        emission = new SeeRGB() { value = value ?? (new() { x = 0, y = 0, z = 0 }) };
                    }

                    mesh.emission = emission;

                    break;
                }
            }

            return mesh;
        }
    }
}
