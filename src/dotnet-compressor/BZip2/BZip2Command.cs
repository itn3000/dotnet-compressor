using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using ICSharpCode.SharpZipLib.BZip2;

namespace dotnet_compressor
{
    [Command("bz2", "bzip2", Description = "manupilating bzip2 data")]
    [Subcommand(typeof(BZip2CompressCommand), typeof(BZip2DecompressCommand))]
    [HelpOption]
    class BZip2Command
    {
        public int OnExecute(CommandLineApplication<BZip2Command> application, IConsole console)
        {
            console.Error.WriteLine(application.GetHelpText());
            return 0;
        }
    }
    [Command("c", "bz2compress", Description = "compressing data as bzip2 format")]
    [HelpOption]
    class BZip2CompressCommand
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
                    using(var ozstm = new BZip2OutputStream(ostm))
                    {
                        istm.CopyTo(ozstm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed bzip2 compression:{e}");
                return 1;
            }
            return 0;
        }
    }
    [Command("d", "bz2decompress", Description = "decompressing data as bzip2 format")]
    [HelpOption]
    class BZip2DecompressCommand
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
                    using(var izstm = new BZip2InputStream(istm))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed bzip2 decompression:{e}");
                return 1;
            }
            return 0;
        }
    }
}