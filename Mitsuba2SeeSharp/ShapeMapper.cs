using System.IO;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp
{
    public static class ShapeMapper
    {
        public static SeeMesh Map(SceneObject shape, Options options)
        {
            SeeMesh mesh = null;
            if (shape.PluginType == "rectangle")
            {
                // TODO
            }
            else if (shape.PluginType == "serialized")
            {
                mesh = new() { type = "ply" };

                SeeTransform transform = MapperUtils.ExtractTransform(shape, options);
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape"))
                {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, options);
                Mesh actualMesh = SerializedLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                actualMesh.ApplyTransform(transform);

                string newpath = MapperUtils.CreatePlyPath(filename, options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, options));
            }
            else if (shape.PluginType == "obj")
            {
                mesh = new() { type = "ply" };

                SeeTransform transform = MapperUtils.ExtractTransform(shape, options);
                int shapeIndex = 0;
                if (shape.Properties.ContainsKey("shape"))
                {
                    shapeIndex = (int)shape.Properties["shape"].GetInteger();
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(shape, options);
                Mesh actualMesh = ObjLoader.ParseFile(filename, shapeIndex);
                if (actualMesh == null)
                    return null;

                actualMesh.ApplyTransform(transform);

                string newpath = MapperUtils.CreatePlyPath(filename, options);
                File.WriteAllBytes(newpath, actualMesh.ToPly());
                mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, options));
            }
            else if (shape.PluginType == "ply")
            {
                mesh = new() { type = "ply" };

                SeeTransform transform = MapperUtils.ExtractTransform(shape, options);

                if (!MapperUtils.IsTransformIdentity(transform))
                {
                    string filename = MapperUtils.ExtractFilenameAbsolute(shape, options);
                    Mesh actualMesh = PlyLoader.ParseFile(filename);
                    if (actualMesh == null)
                        return null;

                    actualMesh.ApplyTransform(transform);

                    string newpath = MapperUtils.CreatePlyPath(filename, options);
                    File.WriteAllBytes(newpath, actualMesh.ToPly());
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.MakeItRelative(newpath, options));
                }
                else
                {
                    // Use file directly
                    mesh.relativePath = MapperUtils.PathToUnix(MapperUtils.ExtractFilename(shape, options));
                }
            }
            else
            {
                Log.Error("Currently no support for " + shape.PluginType + " type of shapes");
            }

            if (mesh == null)
                return null;

            // Handle material assosciation
            foreach (var child in shape.AnonymousChildren)
            {
                if (child.Type == ObjectType.Bsdf)
                {
                    mesh.material = child.ID;
                    break;
                }
            }

            return mesh;
        }
    }
}
