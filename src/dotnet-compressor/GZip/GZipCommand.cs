using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using System.IO.Compression;

namespace dotnet_compressor
{
    [Command("gz", "gzip", Description = "manupilating gzip data")]
    [Subcommand(typeof(GZipCompressCommand), typeof(GZipDecompressCommand))]
    [HelpOption]
    class GZipCommand
    {
        public int OnExecute(CommandLineApplication<GZipCommand> application, IConsole console)
        {
            console.Error.WriteLine(application.GetHelpText());
            return 0;
        }
    }
    [Command("c", "gzcompress", Description = "compressing data as gzip format")]
    [HelpOption]
    class GZipCompressCommand
    {
        [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        [Option("-l|--level=<COMPRESSION_LEVEL>", "compression level(from 0 to 2, higher is more reducible", CommandOptionType.SingleValue)]
        public string CompressionLevelString { get; set; }
        int Level => !string.IsNullOrEmpty(CompressionLevelString) && int.TryParse(CompressionLevelString, out var x) ? x : 0;
        public int OnExecute(IConsole console)
        {
            try
            {
                using(var istm = Util.OpenInputStream(InputFile))
                using(var ostm = Util.OpenOutputStream(OutputFile, true))
                {
                    var level = CompressionLevel.Optimal;
                    switch(Level)
                    {
                        case 0:
                            level = CompressionLevel.NoCompression;
                            break;
                        case 1:
                            level = CompressionLevel.Fastest;
                            break;
                        case 2:
                            level = CompressionLevel.Optimal;
                            break;
                    }
                    using(var izstm = new GZipStream(istm, level))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed gzip compression:{e}");
                return 1;
            }
            return 0;
        }
    }
    [Command("d", "gzdecompress", Description = "decompressing data as gzip format")]
    [HelpOption]
    class GZipDecompressCommand
    {
        [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                using(var istm = Util.OpenInputStream(InputFile))
                using(var ostm = Util.OpenOutputStream(OutputFile, true))
                {
                    using(var izstm = new GZipStream(istm, CompressionMode.Decompress))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed gzip decompression:{e}");
                return 1;
            }
            return 0;
        }
    }
}