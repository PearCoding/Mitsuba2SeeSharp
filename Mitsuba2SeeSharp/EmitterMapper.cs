using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    public static class EmitterMapper {
        public static void Setup(SceneObject emitter, ref LoadContext ctx) {
            if (emitter.PluginType == "envmap") {
                if (ctx.Scene.background != null) {
                    Log.Error("Scene already has a background associated with");
                    return;
                }

                string filename = MapperUtils.ExtractFilename(emitter, ctx.Options);
                ctx.Scene.background = new() { type = "image", filename = filename };
            } else {
                Log.Error("Currently no support for " + emitter.PluginType + " type of shapes");
            }
        }
    }
}
