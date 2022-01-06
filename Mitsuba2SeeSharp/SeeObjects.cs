using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

#pragma warning disable IDE1006 // Disable naming warnings as we try to match the json file
namespace Mitsuba2SeeSharp {
    public class SeeVector {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class SeeMatrix {
        // The Matrix4x4 has the translation on the last row,
        // we define the translation in the last column. So both are the transpose of each other
        public float[] elements { get; } = new float[4 * 4];

        public Matrix4x4 ToNative() {
            return new(
                elements[0], elements[1], elements[2], elements[3],
                elements[4], elements[5], elements[6], elements[7],
                elements[8], elements[9], elements[10], elements[11],
                elements[12], elements[13], elements[14], elements[15]);
        }

        public static SeeMatrix FromNative(Matrix4x4 mat) {
            SeeMatrix mat2 = new();

            mat2.elements[0] = mat.M11; mat2.elements[1] = mat.M12; mat2.elements[2] = mat.M13; mat2.elements[3] = mat.M14;
            mat2.elements[4] = mat.M21; mat2.elements[5] = mat.M22; mat2.elements[6] = mat.M23; mat2.elements[7] = mat.M24;
            mat2.elements[8] = mat.M31; mat2.elements[9] = mat.M32; mat2.elements[10] = mat.M33; mat2.elements[11] = mat.M34;
            mat2.elements[12] = mat.M41; mat2.elements[13] = mat.M42; mat2.elements[14] = mat.M43; mat2.elements[15] = mat.M44;

            return mat2;
        }

        public void Transpose() {
            FromNative(Matrix4x4.Transpose(ToNative())).elements.CopyTo(elements, 0);
        }
    }

    public class SeeRGB {
        public SeeVector value { get; set; }

        public static SeeRGB Gray(float v) { return new() { value = new() { x = v, y = v, z = v } }; }
        public static SeeRGB White { get => Gray(1); }
        public static SeeRGB Black { get => Gray(0); }
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

        [JsonIgnore]
        public bool IsIdentity => matrix.ToNative().IsIdentity;

        public void Invert() {
            var mat = matrix.ToNative();
            Matrix4x4 invmat;
            if (Matrix4x4.Invert(mat, out invmat))
                matrix = SeeMatrix.FromNative(invmat);
            else
                matrix = SeeMatrix.FromNative(Matrix4x4.Identity);
        }

        public void FlipX() {
            var mat = matrix.ToNative();
            mat.M11 *= -1;
            mat.M12 *= -1;
            mat.M13 *= -1;
            matrix = SeeMatrix.FromNative(mat);
        }

        public void FlipZ() {
            var mat = matrix.ToNative();
            mat.M31 *= -1;
            mat.M32 *= -1;
            mat.M33 *= -1;
            matrix = SeeMatrix.FromNative(mat);
        }

        public Vector3 Transform(Vector3 v) {
            return Vector3.Transform(v, matrix.ToNative());
        }

        public void TransformList(ref List<Vector3> list) {
            var mat = matrix.ToNative();
            for (int i = 0; i < list.Count; ++i)
                list[i] = Vector3.Transform(list[i], mat);
        }

        public Vector3 TransformNormal(Vector3 v) {
            return Vector3.TransformNormal(v, matrix.ToNative());
        }

        public void TransformNormalList(ref List<Vector3> list) {
            var mat = matrix.ToNative();
            for (int i = 0; i < list.Count; ++i)
                list[i] = Vector3.TransformNormal(list[i], mat);
        }
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
        public SeeRGB emission { get; set; }
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
        public float diffuseTransmittance { get; set; } = 0;
        public float IOR { get; set; } = 1.45f;
        public float metallic { get; set; } = 0;
        public float specularTint { get; set; } = 1.0f;
        public float specularTransmittance { get; set; } = 0;

        public SeeMaterial Copy() {
            return (SeeMaterial)this.MemberwiseClone();
        }
    }

    public class SeeScene {
        public string name { get; set; }
        public List<SeeTransform> transforms { get; } = new();
        public List<SeeCamera> cameras { get; } = new();
        public SeeBackground background { get; set; }
        public List<SeeMaterial> materials { get; } = new();
        public List<SeeMesh> objects { get; } = new();

        public SeeMaterial GetMaterial(string name) {
            return materials.Find(m => m.name == name);
        }
    }
}
