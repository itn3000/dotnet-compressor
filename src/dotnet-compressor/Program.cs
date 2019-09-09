using System;
using McMaster.Extensions.CommandLineUtils;

namespace dotnet_compressor
{
    [Subcommand(typeof(Zip.ZipCommand))]
    [Subcommand(typeof(Tar.TarCommand))]
    class RootCommand
    {
        public void OnExecute(CommandLineApplication<RootCommand> application)
        {
            Console.Error.WriteLine($"you must specify subcommand");
            Console.Error.WriteLine(application.GetHelpText());
        }
    }
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var rc = CommandLineApplication.Execute<RootCommand>(args);
                return rc;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"compression command error:{e}");
                return -1;
            }
        }
    }
}
