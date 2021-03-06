using System.IO;
using System.Linq;
using System.Numerics;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    public static class ShapeMapper {
        public static SeeMesh Map(SceneObject shape, ref LoadContext ctx) {
            SeeMesh mesh = new() { type = "ply" };
            if (shape.PluginType == "rectangle") {
                bool flip_normals = MapperUtils.GetBool(shape, "flip_normals", false);

                Mesh actualMesh = PrimitiveLoader.CreateRectangle();
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                if (flip_normals)
                    actualMesh.FlipNormals();

                string newpath = ctx.RequestPlyPath($"__rectangle_{ctx.Scene.objects.Count}.ply");
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = ctx.PrepareFilename(newpath);
            } else if (shape.PluginType == "sphere") {
                Vector3 center = MapperUtils.GetVector(shape, "center", new(0, 0, 0));
                float radius = MapperUtils.GetNumber(shape, "radius", 1);

                Mesh actualMesh = PrimitiveLoader.CreateSphere(center, radius);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                string newpath = ctx.RequestPlyPath($"__sphere_{ctx.Scene.objects.Count}.ply");
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = ctx.PrepareFilename(newpath);
            } else if (shape.PluginType == "serialized") {
                int shapeIndex = MapperUtils.GetInteger(shape, "shape_index", 0);

                bool face_normals = MapperUtils.GetBool(shape, "face_normals", false);
                MeshLoadingOptions options = new() { IgnoreNormals = face_normals };

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx);
                Mesh actualMesh = SerializedLoader.ParseFile(filename, options, shapeIndex);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                // Simple test to prevent useless geometry
                if (actualMesh.SurfaceArea <= 1e-6f) {
                    Log.Error("Given shape has no surface. Ignoring it");
                    return null;
                }

                if (face_normals) actualMesh.ComputeFaceNormals();

                string newpath = ctx.RequestPlyPath(filename);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = ctx.PrepareFilename(newpath);
            } else if (shape.PluginType == "obj") {
                int shapeIndex = MapperUtils.GetInteger(shape, "shape_index", 0);

                bool flip_tex_coords = MapperUtils.GetBool(shape, "flip_tex_coords", true);
                bool face_normals = MapperUtils.GetBool(shape, "face_normals", false);
                MeshLoadingOptions options = new() { IgnoreNormals = face_normals };

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx);
                Mesh actualMesh = ObjLoader.ParseFile(filename, options, shapeIndex);
                if (actualMesh == null)
                    return null;

                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                if (transform != null) actualMesh.ApplyTransform(transform);

                // Simple test to prevent useless geometry
                if (actualMesh.SurfaceArea <= 1e-6f) {
                    Log.Error("Given shape has no surface. Ignoring it");
                    return null;
                }

                if (flip_tex_coords) actualMesh.FlipTexUp();

                if (face_normals) actualMesh.ComputeFaceNormals();

                string newpath = ctx.RequestPlyPath(filename);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = ctx.PrepareFilename(newpath);
            } else if (shape.PluginType == "ply") {
                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);

                bool face_normals = MapperUtils.GetBool(shape, "face_normals", false);
                MeshLoadingOptions options = new() { IgnoreNormals = face_normals };

                if (face_normals || (transform != null && !transform.IsIdentity)) {
                    string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx);
                    Mesh actualMesh = PlyLoader.ParseFile(filename, options);
                    if (actualMesh == null)
                        return null;

                    // Simple test to prevent useless geometry
                    if (actualMesh.SurfaceArea <= 1e-6f) {
                        Log.Error("Given shape has no surface. Ignoring it");
                        return null;
                    }

                    if (face_normals) actualMesh.ComputeFaceNormals();

                    string newpath = ctx.RequestPlyPath(filename);
                    File.WriteAllBytes(newpath, actualMesh.ToPly());
                    mesh.relativePath = ctx.PrepareFilename(newpath);
                } else {
                    // Use file directly
                    mesh.relativePath = ctx.PrepareFilename(MapperUtils.ExtractFilename(shape, ctx));
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
