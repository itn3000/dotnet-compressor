using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Writers.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.Crypto;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Mono.Posix;

namespace dotnet_compressor.Zip
{
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
    [Command("d", "decompress", "zipdecompress", Description = "decompress zip archive")]
    [HelpOption]
    class ZipDecompressCommand
    {
        [Option("-i|--input=<INPUT_FILE_PATH>", "input file path(default: stdin)", CommandOptionType.SingleValue)]
        public string InputPath { get; set; }
        [Option("-o|--output=<OUTPUT_DIRECTORY>", "output directory path(default: current directory)", CommandOptionType.SingleValue)]
        public string OutputDirectory { get; set; }
        [Option("--include=<INCLUDE_PATTERN>", "extracting file include pattern(default: **/*)", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("--exclude=<EXCLUDE_FILE_PATTERN>", "extracting file exclude pattern(default: none)", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-p|--passenv=<PASSWORD_ENVIRONMENT_NAME>", "zip password", CommandOptionType.SingleValue)]
        public string PassEnvironmentName { get; set; }
        [Option("-e|--encoding=<FILENAME_ENCODING>", "filename encoding in archive(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-l|--list", "output file list only then exit", CommandOptionType.NoValue)]
        public bool ListOnly { get; set; }
        public void OnExecute(IConsole console)
        {
            var outdir = !string.IsNullOrEmpty(OutputDirectory) ? OutputDirectory : Directory.GetCurrentDirectory();
            using (var istm = Util.OpenInputStream(InputPath))
            {
                var ropt = new SharpCompress.Readers.ReaderOptions();
                ropt.ArchiveEncoding = new ArchiveEncoding()
                {
                    Default = Util.GetEncodingFromName(FileNameEncoding)
                };
                if (!string.IsNullOrEmpty(PassEnvironmentName))
                {
                    ropt.Password = Environment.GetEnvironmentVariable(PassEnvironmentName);
                }
                using (var reader = SharpCompress.Readers.Zip.ZipReader.Open(istm, ropt))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (ListOnly)
                        {
                            console.WriteLine(reader.Entry.Key);
                            continue;
                        }
                        if (reader.Entry.IsDirectory)
                        {
                            var outdi = new DirectoryInfo(Path.Combine(outdir, reader.Entry.Key));
                            if (!outdi.Exists)
                            {
                                outdi.Create();
                            }
                        }
                        else
                        {
                            var outfi = new FileInfo(Path.Combine(outdir, reader.Entry.Key));
                            if (!outfi.Directory.Exists)
                            {
                                outfi.Directory.Create();
                            }
                            using (var ostm = File.Create(outfi.FullName))
                            using (var entrystm = reader.OpenEntryStream())
                            {
                                entrystm.CopyTo(ostm);
                            }
                            if (reader.Entry.LastModifiedTime.HasValue)
                            {
                                outfi.LastWriteTime = reader.Entry.LastModifiedTime.Value;
                            }
                        }
                    }
                }
            }
        }
    }
    [Command("c", "compress", "zipcompress", Description = "compress zip archive")]
    [HelpOption]
    class ZipCompressCommand
    {
        [Option("-b|--base <BASEPATH>", "input base path", CommandOptionType.SingleValue)]
        public string BasePath { get; set; }
        [Option("-o|--output <OUTPUT_ZIP>", "output zip path(default: stdout)", CommandOptionType.SingleValue)]
        public string OutputPath { get; set; }
        [Option("-i|--include <INCLUDE_PATTERN>", "file include pattern(default: all files and directories)", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("-x|--exclude <EXCLUDE_PATTERN>", "file exclude pattern(default: none)", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-p|--passenv <ENVIRONMENT_NAME>", "encryption password environment name(default: none)", CommandOptionType.SingleValue)]
        public string PassEnvironmentName { get; set; }
        [Option("-e|--encoding <ENCODING_NAME>", "filename encoding in archive(default: system default)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-E|--encryption", "flag for encryption, you must specify passenv option too(default: false)", CommandOptionType.NoValue)]
        public bool Encryption { get; set; }
        [Option("--case-sensitive", "flag for case sensitivity on includes and excludes option(default: false)", CommandOptionType.NoValue)]
        public bool CaseSensitive { get; set; }
        [Option("--level=<COMPRESSION_LEVEL>", "compression level(between 1 and 9)", CommandOptionType.SingleValue)]
        public byte CompressionLevel { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                ZipStrings.CodePage = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8).CodePage;
                var basePath = !string.IsNullOrEmpty(BasePath) ? BasePath : Directory.GetCurrentDirectory();
                using(var ostm = Util.OpenOutputStream(OutputPath, true))
                using(var zstm = new ZipOutputStream(ostm))
                {
                    if(CompressionLevel > 0)
                    {
                        zstm.SetLevel(CompressionLevel);
                    }
                    foreach(var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
                    {
                        var fi = new FileInfo(Path.Combine(basePath, path));
                        var entryName = ZipEntry.CleanName(stem);
                        var zentry = new ZipEntry(entryName);
                        console.Error.WriteLine($"{path} -> {entryName}");
                        zentry.DateTime = fi.LastWriteTime;
                        zentry.Size = fi.Length;
                        zstm.PutNextEntry(zentry);
                        using(var istm = fi.OpenRead())
                        {
                            istm.CopyTo(zstm);
                        }
                        zstm.CloseEntry();
                    }
                }
                // var wopt = new SharpCompress.Writers.Zip.ZipWriterOptions(CompressionType.Deflate);
                // wopt.ArchiveEncoding = new ArchiveEncoding()
                // {
                //     Default = Util.GetEncodingFromName(FileNameEncoding)
                // };
                // wopt.UseZip64 = UseZip64;
                // using (var dest = Util.OpenOutputStream(OutputPath, true))
                // using (var zw = new ZipWriter(dest, wopt))
                // {
                //     Includes = Includes ?? new string[] { "**/*" };
                //     Console.Error.WriteLine($"basepath = {basePath}, Includes = {string.Join("|", Includes)}, CaseSensitive={CaseSensitive}");
                //     foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
                //     {
                //         Console.Error.WriteLine($"compressing {path}, {stem}");
                //         var fi = new FileInfo(Path.Combine(basePath, path));
                //         var modTime = fi.LastWriteTime;
                //         using (var istm = File.OpenRead(fi.FullName))
                //         {
                //             var zwopt = new ZipWriterEntryOptions();
                //             zw.Write(stem, istm, modTime);
                //         }
                //     }
                // }
                return 0;
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed to creating zip archive:{e}");
                return 1;
            }
        }
    }
}