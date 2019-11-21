using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using SharpCompress.Compressors.Xz;

namespace dotnet_compressor
{
    [Command("xz", Description = "manupilating xz data(decompressing only)")]
    [Subcommand(typeof(XzDecompressCommand))]
    [HelpOption]
    class XzCommand
    {
        public int OnExecute(CommandLineApplication<XzCommand> application, IConsole console)
        {
            console.Error.WriteLine(application.GetHelpText());
            return 0;
        }
    }
    [Command("d", "xzdecompress", Description = "decompressing data as xz format")]
    [HelpOption]
    class XzDecompressCommand
    {
        [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input)", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output)", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                using(var istm = Util.OpenInputStream(InputFile))
                using(var ostm = Util.OpenOutputStream(OutputFile, true))
                {
                    using(var izstm = new XZStream(istm))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed xz decompression:{e}");
                return 1;
            }
            return 0;
        }
    }
}