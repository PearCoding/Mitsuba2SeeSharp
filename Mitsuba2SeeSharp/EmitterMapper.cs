using TinyParserMitsuba;

using SimpleImageIO;
using System.Linq;

namespace Mitsuba2SeeSharp {
    public static class EmitterMapper {
        public static void Setup(SceneObject emitter, ref LoadContext ctx) {
            if (emitter.PluginType == "envmap") {
                if (ctx.Scene.background != null) {
                    Log.Error("Scene already has a background associated with");
                    return;
                }

                string filename = MapperUtils.ExtractFilenameAbsolute(emitter, ctx);

                // Flip Y-Coordinate due to different conventions
                var layers = ImageBase.LoadLayersFromFile(filename);
                foreach (var layer in layers) {
                    layer.Value.FlipHorizontal();
                }

                string filename2 = ctx.RequestImagePath(filename);
                ImageBase.WriteLayeredExr(filename2, layers.Select(p => (p.Key, p.Value)).ToArray());

                ctx.Scene.background = new() { type = "image", filename = ctx.PrepareFilename(filename2) };
            } else {
                Log.Error("Currently no support for " + emitter.PluginType + " emitters");
            }
        }
    }
}
