using CommandLine;
using System.IO;

namespace Mitsuba2SeeSharp {
    public class Options {
        [Value(0, MetaName = "input file",
            HelpText = "Input file to be processed.",
            Required = true)]
        public string Input { get; set; }
        [Option('o', "output",
            HelpText = "Output file to generate")]
        public string Output { get; set; }


        public string ActualOutput => Output ?? Path.ChangeExtension(Input, ".json");

        [Option("keep-unused-materials",
            HelpText = "Do not omit unused materials",
            Default = false)]
        public bool KeepUnusedMaterials { get; set; }

        public string MeshDirectory { get; set; } = Path.Join("data", "meshes");
        public string ImageDirectory { get; set; } = Path.Join("data", "images");

        [Option('V', "verbose",
            HelpText = "Print out more information",
            Default = false)]
        public bool Verbose { get; set; }

        [Option("copy-images",
            HelpText = "Always copy images to image directory",
            Default = false)]
        public bool CopyImages { get; set; }
    }
}
