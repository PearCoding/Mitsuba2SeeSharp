using System.IO;
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
                actualMesh.ApplyTransform(transform);
                if (flip_normals)
                    actualMesh.FlipNormals();

                string newpath = MapperUtils.CreatePlyPath($"__rectangle_{ctx.Scene.objects.Count}.ply", ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "serialized") {
                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape")) {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                Mesh actualMesh = SerializedLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                actualMesh.ApplyTransform(transform);

                string newpath = MapperUtils.CreatePlyPath(filename, ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "obj") {
                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape")) {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                Mesh actualMesh = ObjLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                actualMesh.ApplyTransform(transform);

                string newpath = MapperUtils.CreatePlyPath(filename, ctx.Options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, ctx.Options));
            } else if (shape.PluginType == "ply") {
                SeeTransform transform = MapperUtils.ExtractTransform(shape, ctx.Options);

                if (!MapperUtils.IsTransformIdentity(transform)) {
                    string filename = MapperUtils.ExtractFilenameAbsolute(shape, ctx.Options);
                    Mesh actualMesh = PlyLoader.ParseFile(filename);
                    if (actualMesh == null)
                        return null;

                    actualMesh.ApplyTransform(transform);

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
                    if (id == "" || !ctx.MaterialNames.Contains(id)) {
                        SeeMaterial mat = MaterialMapper.Map(child, ref ctx);
                        if (mat != null) {
                            ctx.Scene.materials.Add(mat);
                            ctx.MaterialNames.Add(mat.name);
                            mesh.material = mat.name;
                        }
                    } else {
                        mesh.material = child.ID;
                    }
                    break;
                }
            }

            return mesh;
        }
    }
}
