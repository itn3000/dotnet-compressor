using McMaster.Extensions.CommandLineUtils;

namespace dotnet_compressor.Zip
{
    static class Constants
    {
        // from stat.st_mode
        public const int S_IFMT = 0xf000;
        public const int S_IFREG = 0x8000;
        // from https://support.pkware.com/home/pkzip/developer-tools/appnote/
        public const int HostMSDOS = 0;
        public const int HostWinNTFS = 10;
        public const int HostUnix = 3;
    }
    [Command("zip")]
    [Subcommand(typeof(ZipCompressCommand))]
    [Subcommand(typeof(ZipDecompressCommand))]
    [HelpOption(Description = "zip compress or decompress")]
    class ZipCommand
    {
        public void OnExecute(CommandLineApplication<ZipCommand> application, IConsole con)
        {
            con.Error.WriteLine("you must specify compress or decompress subcommand");
            con.Error.WriteLine(application.GetHelpText());
        }
    }
}