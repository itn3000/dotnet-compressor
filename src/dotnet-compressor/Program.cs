using System;
using McMaster.Extensions.CommandLineUtils;


namespace dotnet_compressor
{
    [Subcommand(typeof(Zip.ZipCommand))]
    [Subcommand(typeof(Tar.TarCommand))]
    [Subcommand(typeof(GZipCommand))]
    [Subcommand(typeof(BZip2Command))]
    [Subcommand(typeof(LZipCommand))]
    [Subcommand(typeof(XzCommand))]
    [VersionOptionFromMember(MemberName = "ApplicationVersion")]
    class RootCommand
    {
        public void OnExecute(CommandLineApplication<RootCommand> application)
        {
            Console.Error.WriteLine($"you must specify subcommand({ApplicationVersion})");
            Console.Error.WriteLine(application.GetHelpText());
        }
        public string ApplicationVersion => typeof(Program).Assembly.GetName().Version.ToString();
    }
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
