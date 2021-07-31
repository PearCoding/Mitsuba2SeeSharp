using System.Collections.Generic;

#pragma warning disable IDE1006 // Disable naming warnings as we try to match the json file
namespace Mitsuba2SeeSharp {
    public class SeeVector {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class SeeMatrix {
        public float[] elements { get; } = new float[4 * 4];
    }

    public class SeeRGB {
        public SeeVector value { get; set; }
    }

    public class SeeColorOrTexture {
        public string type { get; set; }
        public SeeVector value { get; set; }
        public string filename { get; set; }

        public static SeeColorOrTexture White { get => new() { type = "rgb", value = new() { x = 1, y = 1, z = 1 }, filename = null }; }
        public static SeeColorOrTexture Black { get => new() { type = "rgb", value = new() { x = 0, y = 0, z = 0 }, filename = null }; }
    }

    public class SeeTransform {
        public string name { get; set; }
        //public SeeVector scale { get; set; }
        //public SeeVector rotation { get; set; }
        //public SeeVector position { get; set; }
        public SeeMatrix matrix { get; set; }
    }

    public class SeeCamera {
        public string name { get; set; }
        public string type { get; set; }
        public float fov { get; set; }
        public string transform { get; set; }
    }

    public class SeeBackground {
        public string type { get; set; }
        public string filename { get; set; }
    }

    public class SeeMesh {
        // We expose all objects as ply for SeeSharp
        public string name { get; set; }
        public string type { get; set; }
        public string relativePath { get; set; }
        public string material { get; set; }
    }

    public class SeeMaterial {
        public string name { get; set; }
        public string type { get; set; }

        public SeeRGB emission { get; set; }

        public SeeColorOrTexture baseColor { get; set; }
        public bool thin { get; set; } = false;

        // Generic only
        public float roughness { get; set; } = 0.5f;
        public float anisotropic { get; set; } = 0;
        public float diffuseTransmittance { get; set; } = 1;
        public float IOR { get; set; } = 1;
        public float metallic { get; set; } = 0;
        public float specularTint { get; set; } = 0;
        public float specularTransmittance { get; set; } = 0;
    }

    public class SeeScene {
        public string name { get; set; }
        public List<SeeTransform> transforms { get; } = new();
        public List<SeeCamera> cameras { get; } = new();
        public SeeBackground background { get; set; }
        public List<SeeMaterial> materials { get; } = new();
        public List<SeeMesh> objects { get; } = new();
    }
}
