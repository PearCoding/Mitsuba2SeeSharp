using CommandLine;
using System;
using System.IO;
using System.Text.Json;
using TinyParserMitsuba;

namespace Mitsuba2SeeSharp {
    static class Program {
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts => Run(opts));
        }

        static void Run(Options opts) {
            SceneLoader loader = new();

            DirectoryInfo parent = Directory.GetParent(opts.Input);
            if (parent != null) {
                loader.AddLookupDir(parent.FullName);
            }

            Scene scene = loader.LoadFromFile(opts.Input);

            if (scene == null || loader.HasError) {
                Console.Error.WriteLine("ERROR: " + loader.Error);
                return;
            }

            SeeScene seeScene = SceneMapper.Map(scene, opts);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            jsonOptions.Converters.Add(new MatrixConverter());
            jsonOptions.Converters.Add(new RGBConverter());
            jsonOptions.Converters.Add(new ColorOrTextureConverter());
            jsonOptions.Converters.Add(new MaterialConverter());

            string outputString = JsonSerializer.Serialize(seeScene, jsonOptions);

            Console.WriteLine(outputString);
            File.WriteAllText(opts.ActualOutput, outputString);
        }
    }
}
