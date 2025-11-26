using System;
using ConsoleAppFramework;
using dotnet_compressor.Tar;
using dotnet_compressor.Zip;
using dotnet_compressor.Zstd;
// using McMaster.Extensions.CommandLineUtils;


namespace dotnet_compressor
{
    // [Subcommand(typeof(Zip.ZipCommand))]
    // [Subcommand(typeof(Tar.TarCommand))]
    // [Subcommand(typeof(GZipCommand))]
    // [Subcommand(typeof(BZip2Command))]
    // [Subcommand(typeof(LZipCommand))]
    // [Subcommand(typeof(XzCommand))]
    // [VersionOptionFromMember(MemberName = "ApplicationVersion")]
    // class RootCommand
    // {
    //     public void OnExecute(CommandLineApplication<RootCommand> application)
    //     {
    //         Console.Error.WriteLine($"you must specify subcommand({ApplicationVersion})");
    //         Console.Error.WriteLine(application.GetHelpText());
    //     }
    //     public string ApplicationVersion => typeof(Program).Assembly.GetName().Version.ToString();
    // }
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var app = ConsoleApp.Create();
            app.Add<BZip2Command>();
            app.Add<GZipCommand>();
            app.Add<LZipCommand>();
            app.Add<TarCommand>();
            app.Add<ZstdCommand>();
            app.Add<ZipCommand>();
            app.Run(args);
            // var rc = CommandLineApplication.Execute<RootCommand>(args);
            // return rc;
        }
    }
}
