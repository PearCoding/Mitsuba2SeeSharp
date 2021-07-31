using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mitsuba2SeeSharp
{
    public class MatrixConverter : JsonConverter<SeeMatrix>
    {
        public override SeeMatrix Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // No need
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SeeMatrix value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.elements.Length; ++i)
                writer.WriteNumberValue(value.elements[i]);
            writer.WriteEndArray();
        }
    }

    public class RGBConverter : JsonConverter<SeeVector>
    {
        public override SeeVector Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // No need
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SeeVector value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }
    }

    public class ColorOrTextureConverter : JsonConverter<SeeColorOrTexture>
    {
        public override SeeColorOrTexture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // No need
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SeeColorOrTexture value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.type);
            if (value.type == "rgb")
            {
                writer.WritePropertyName("value");
                writer.WriteStartArray();
                writer.WriteNumberValue(value.value.x);
                writer.WriteNumberValue(value.value.y);
                writer.WriteNumberValue(value.value.z);
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteString("filename", value.filename);
            }
            writer.WriteEndObject();
        }
    }

    public class MaterialConverter : JsonConverter<SeeMaterial>
    {
        public override SeeMaterial Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // No need
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SeeMaterial value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.type);
            writer.WriteString("name", value.name);
            writer.WritePropertyName("baseColor");
            JsonSerializer.Serialize(writer, value.baseColor, options);

            writer.WriteBoolean("thin", value.thin);

            if (value.type == "generic")
            {
                writer.WriteNumber("roughness", value.roughness);
                writer.WriteNumber("anisotropic", value.anisotropic);
                writer.WriteNumber("diffuseTransmittance", value.diffuseTransmittance);
                writer.WriteNumber("IOR", value.IOR);
                writer.WriteNumber("metallic", value.metallic);
                writer.WriteNumber("specularTint", value.specularTint);
                writer.WriteNumber("specularTransmittance", value.specularTransmittance);
            }

            if (value.emission != null)
            {
                writer.WritePropertyName("emission");
                JsonSerializer.Serialize(writer, value.emission, options);
            }

            writer.WriteEndObject();
        }
    }
}
