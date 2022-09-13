using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;

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
        [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input)", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output)", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        [Option("-l|--level=<COMPRESSION_LEVEL>", "compression level(from 0 to 9, higher is more reducible)", CommandOptionType.SingleValue)]
        public string CompressionLevelString { get; set; }
        int Level => !string.IsNullOrEmpty(CompressionLevelString) && int.TryParse(CompressionLevelString, out var x) ? x : -1;
        public int OnExecute(IConsole console)
        {
            try
            {
                using (var istm = Util.OpenInputStream(InputFile))
                using (var ostm = Util.OpenOutputStream(OutputFile, true))
                {
                    using (var ozstm = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(ostm))
                    {
                        if (Level >= 0)
                        {
                            ozstm.SetLevel(Level);
                        }
                        istm.CopyTo(ozstm);
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
        [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input)", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output)", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                using (var istm = Util.OpenInputStream(InputFile))
                using (var ostm = Util.OpenOutputStream(OutputFile, true))
                {
                    using (var izstm = new GZipInputStream(istm))
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