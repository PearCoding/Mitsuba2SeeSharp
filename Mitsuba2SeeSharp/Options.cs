using CommandLine;

namespace Mitsuba2SeeSharp
{
    public class Options
    {
        [Value(0, MetaName = "input file",
            HelpText = "Input file to be processed.",
            Required = true)]
        public string Input { get; set; }
        [Option('o', "output",
            HelpText = "Output file to generate")]
        public string Output { get; set; }


        public string ActualOutput => Output ?? System.IO.Path.ChangeExtension(Input, ".json");
    };
}
