using System.IO;
using System.Linq;
using System.Numerics;
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

                string newpath = ctx.RequestPlyPath($"__rectangle_{ctx.Scene.objects.Count}.ply", ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "sphere") {
                Vector3 center = MapperUtils.GetVector(shape, "center", new(0, 0, 0));
                float radius = MapperUtils.GetNumber(shape, "radius", 1);

                Mesh actualMesh = PrimitiveLoader.CreateSphere(center, radius);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                string newpath = ctx.RequestPlyPath($"__sphere_{ctx.Scene.objects.Count}.ply", ctx.Options);
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

                string newpath = ctx.RequestPlyPath(filename, ctx.Options);
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

                string newpath = ctx.RequestPlyPath(filename, ctx.Options);
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

                    string newpath = ctx.RequestPlyPath(filename, ctx.Options);
                    File.WriteAllBytes(newpath, actualMesh.ToPly());
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
                } else {
                    // Use file directly
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.ExtractFilename(shape, ctx.Options));
                }
            } else {
                Log.Error("Currently no support for " + shape.PluginType + " shapes");
                return null;
            }

            // Handle material assosciation
            bool handleInnerBsdf(SceneObject child, ref LoadContext ctx2) {
                if (child.Type == ObjectType.Bsdf) {
                    string id = child.ID;
                    if (id == "" || !ctx2.MaterialRefs.ContainsKey(id)) {
                        SeeMaterial mat = MaterialMapper.Map(child, ref ctx2);
                        if (mat != null) {
                            ctx2.Scene.materials.Add(mat);
                            ctx2.MaterialRefs.Add(mat.name, 1);
                            mesh.material = mat.name;
                        }
                    } else {
                        mesh.material = child.ID;
                        ctx2.MaterialRefs[mesh.material] += 1;
                    }
                    return true;
                }
                return false;
            }

            foreach (SceneObject child in shape.AnonymousChildren) {
                if (handleInnerBsdf(child, ref ctx))
                    break;
            }

            if (mesh.material == null || mesh.material == "") {
                foreach (var pair in shape.NamedChildren) {
                    if (handleInnerBsdf(pair.Value, ref ctx))
                        break;
                }
            }

            // Make sure all shapes have some kind of material
            if (mesh.material == null || mesh.material == "") {
                Log.Warning("Given shape has no bsdf. Setting it to black as default");
                SeeMaterial mat = MaterialMapper.CreateBlack($"__black__{ctx.MaterialRefs.Count}");
                ctx.Scene.materials.Add(mat);
                ctx.MaterialRefs.Add(mat.name, 1);
                mesh.material = mat.name;
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
