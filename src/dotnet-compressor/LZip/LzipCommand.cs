using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors;

namespace dotnet_compressor
{
    [Command("lzip", Description = "manupilating lzip data")]
    [Subcommand(typeof(LZipCompressCommand), typeof(LZipDecompressCommand))]
    [HelpOption]
    class LZipCommand
    {
        public int OnExecute(CommandLineApplication<LZipCommand> application, IConsole console)
        {
            console.Error.WriteLine(application.GetHelpText());
            return 0;
        }
    }
    [Command("c", "lzipcompress", Description = "compressing data as lzip format")]
    [HelpOption]
    class LZipCompressCommand
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
                    using(var ozstm = new LZipStream(ostm, CompressionMode.Compress))
                    {
                        istm.CopyTo(ozstm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed lzip compression:{e}");
                return 1;
            }
            return 0;
        }
    }
    [Command("d", "lzipdecompress", Description = "decompressing data as lzip format")]
    [HelpOption]
    class LZipDecompressCommand
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
                    using(var izstm = new LZipStream(istm, CompressionMode.Decompress))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed lzip decompression:{e}");
                return 1;
            }
            return 0;
        }
    }
}