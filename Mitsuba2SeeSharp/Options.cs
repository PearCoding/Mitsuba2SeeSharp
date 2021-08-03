using CommandLine;
using System;

namespace Mitsuba2SeeSharp {
    public class Options {
        [Value(0, MetaName = "input file",
            HelpText = "Input file to be processed.",
            Required = true)]
        public string Input { get; set; }
        [Option('o', "output",
            HelpText = "Output file to generate")]
        public string Output { get; set; }


        public string ActualOutput => Output ?? System.IO.Path.ChangeExtension(Input, ".json");

        [Option("keep-unused-materials",
            HelpText = "Do not omit unused materials",
            Default = false)]
        public bool KeepUnusedMaterials { get; set; }
        public string MeshDirectory { get; set; } = "meshes";

        [Option('V', "verbose",
            HelpText = "Print out more information",
            Default = false)]
        public bool Verbose { get; set; }
    }
}
