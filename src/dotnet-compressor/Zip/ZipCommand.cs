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
        public string InputPath { get; set; }
        public string OutputDirectory { get; set; }
        public string[] Includes { get; set; }
        public string[] Excludes { get; set; }
        public string PassEnvironmentName { get; set; }
        public string FileNameEncoding { get; set; }
        public bool Encryption { get; set; }
        public void OnExecute()
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
        [Option("--zip64", "use zip64 format explicitly(default: auto)", CommandOptionType.NoValue)]
        public bool UseZip64 { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                var basePath = !string.IsNullOrEmpty(BasePath) ? BasePath : Directory.GetCurrentDirectory();
                using (var dest = Util.OpenOutputStream(OutputPath, true))
                // using (var zstm = new ZipOutputStream(dest))
                {
                    var zf = new ZipFile(dest);
                    if (Encryption)
                    {
                        zf.Password = Environment.GetEnvironmentVariable(PassEnvironmentName);
                    }
                    foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
                    {
                        var fi = new FileInfo(Path.Combine(basePath, path));
                        var zentry = zf.EntryFactory.MakeFileEntry(fi.Name, stem, true);
                        zentry.CompressionMethod = CompressionMethod.Deflated;
                        zentry.DateTime = fi.LastWriteTime;
                        zf.BeginUpdate();
                        zf.Add(fi.FullName, stem);
                        zf.CommitUpdate();
                        // zstm.PutNextEntry(zentry);
                        // console.Error.WriteLine($"{path} -> {stem}({zentry.Name})");
                        // using (var istm = fi.OpenRead())
                        // {
                        //     istm.CopyTo(zstm);
                        // }
                    }
                    zf.Close();
                    // zstm.SetLevel(5);
                    // Includes = Includes ?? new string[] { "**/*" };
                    // Console.Error.WriteLine($"basepath = {basePath}, Includes = {string.Join("|", Includes)}, CaseSensitive={CaseSensitive}");
                    // ZipStrings.CodePage = Util.GetEncodingFromName(FileNameEncoding).WindowsCodePage;
                    // if (Encryption)
                    // {
                    //     zstm.Password = Environment.GetEnvironmentVariable(PassEnvironmentName);
                    // }
                    // foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
                    // {
                    //     var fi = new FileInfo(Path.Combine(basePath, path));
                    //     var zentry = new ZipEntry(stem.Replace('/', '\\'));
                    //     zentry.CompressionMethod = CompressionMethod.Deflated;
                    //     zentry.DateTime = fi.LastWriteTime;
                    //     zstm.PutNextEntry(zentry);
                    //     console.Error.WriteLine($"{path} -> {stem}({zentry.Name})");
                    //     using (var istm = fi.OpenRead())
                    //     {
                    //         istm.CopyTo(zstm);
                    //     }
                    // }
                }
                return 0;
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed to creating zip archive:{e}");
                return 1;
            }
            // try
            // {
            //     var basePath = !string.IsNullOrEmpty(BasePath) ? BasePath : Directory.GetCurrentDirectory();
            //     var wopt = new SharpCompress.Writers.Zip.ZipWriterOptions(CompressionType.Deflate);
            //     wopt.ArchiveEncoding = new ArchiveEncoding()
            //     {
            //         Default = Util.GetEncodingFromName(FileNameEncoding)
            //     };
            //     wopt.UseZip64 = UseZip64;
            //     using (var dest = Util.OpenOutputStream(OutputPath, true))
            //     using (var zw = new ZipWriter(dest, wopt))
            //     {
            //         Includes = Includes ?? new string[] { "**/*" };
            //         Console.Error.WriteLine($"basepath = {basePath}, Includes = {string.Join("|", Includes)}, CaseSensitive={CaseSensitive}");
            //         foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
            //         {
            //             Console.Error.WriteLine($"compressing {path}");
            //             var fi = new FileInfo(Path.Combine(basePath, path));
            //             var modTime = fi.LastWriteTime;
            //             using (var istm = File.OpenRead(fi.FullName))
            //             {
            //                 var zwopt = new ZipWriterEntryOptions();
            //                 zw.Write(path, istm, modTime);
            //             }
            //         }
            //     }
            //     return 0;
            // }
            // catch (Exception e)
            // {
            //     console.Error.WriteLine($"failed to creating zip archive:{e}");
            //     return 1;
            // }
        }
    }
}