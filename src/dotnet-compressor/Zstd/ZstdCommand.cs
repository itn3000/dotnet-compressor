using McMaster.Extensions.CommandLineUtils;

namespace dotnet_compressor.Zstd;

[Command("zstandard", "zstd", Description = "zstandard command")]
[Subcommand(typeof(ZstdCompressCommand))]
[Subcommand(typeof(ZstdDeompressCommand))]
public class ZstdCommand
{
        public void OnExecute(CommandLineApplication<ZstdCommand> application, IConsole con)
        {
            con.Error.WriteLine("you must specify compress or decompress subcommand");
            con.Error.WriteLine(application.GetHelpText());
        }

}