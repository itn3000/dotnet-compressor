using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archives;
using SharpCompress.Writers.Tar;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using SharpCompress.Common.Tar;
using SharpCompress.Compressors;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace dotnet_compressor.Tar
{
    [Command("tar")]
    [Subcommand(typeof(TarDecompressCommand))]
    [Subcommand(typeof(TarCompressCommand))]
    [HelpOption]
    class TarCommand
    {
        public void OnExecute(CommandLineApplication<TarCommand> application, IConsole con)
        {
            con.Error.WriteLine("you must specify compress or decompress subcommand");
            con.Error.WriteLine(application.GetHelpText());
        }
    }
    [Command("d", "decompress", "decompresstar", Description = "extracting tar archive")]
    [HelpOption]
    class TarDecompressCommand
    {
        [Option("-o|--output=<OUTPUT_DIRECTORY>", "output directory(create if not exists)", CommandOptionType.SingleValue)]
        public string OutputDirectory { get; set; }
        [Option("-i|--input=<INPUT_FILE_PATH>", "input file path(if not specified, using stdin)", CommandOptionType.SingleValue)]
        public string InputPath { get; set; }

        [Option("--include", "pattern of extracting files(default: \"**/*\")", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("--exclude", "pattern of extracting files(default: none)", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-e|--encoding=<ENCODING_NAME>", "filename encoding in tar archive(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-l|--list", "list files only", CommandOptionType.NoValue)]
        public bool ListOnly { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                using (var istm = Util.OpenInputStream(InputPath))
                {
                    var tropt = new ReaderOptions();
                    tropt.ArchiveEncoding = new ArchiveEncoding()
                    {
                        Default = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8)
                    };
                    using (var tarreader = TarReader.Open(istm, tropt))
                    {
                        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                        if (Includes != null && Includes.Length != 0)
                        {
                            matcher.AddIncludePatterns(Includes);
                        }
                        else
                        {
                            matcher.AddInclude("**/*");
                        }
                        if (Excludes != null && Excludes.Length != 0)
                        {
                            matcher.AddExcludePatterns(Excludes);
                        }
                        var outdir = string.IsNullOrEmpty(OutputDirectory) ? Directory.GetCurrentDirectory() : OutputDirectory;
                        while (tarreader.MoveToNextEntry())
                        {
                            var m = matcher.Match(tarreader.Entry.Key);
                            if (!m.HasMatches)
                            {
                                console.Error.WriteLine($"no match, skip:{tarreader.Entry.Key}");
                                continue;
                            }
                            if (ListOnly)
                            {
                                console.WriteLine($"{tarreader.Entry.Key}");
                                continue;
                            }
                            if (tarreader.Entry.IsDirectory)
                            {
                                var destdir = Path.Combine(outdir, tarreader.Entry.Key);
                                if (!Directory.Exists(destdir))
                                {
                                    Directory.CreateDirectory(destdir);
                                }
                            }
                            else
                            {
                                var destfi = new FileInfo(Path.Combine(outdir, tarreader.Entry.Key));
                                console.Error.WriteLine($"extracting {tarreader.Entry.Key}");
                                if (!destfi.Directory.Exists)
                                {
                                    destfi.Directory.Create();
                                }
                                using (var deststm = File.Create(destfi.FullName))
                                using (var entrystm = tarreader.OpenEntryStream())
                                {
                                    entrystm.CopyTo(deststm);
                                }
                                destfi.LastWriteTime = tarreader.Entry.LastModifiedTime.GetValueOrDefault(DateTime.Now);
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed to decompressing tar archive:{e}");
                return 1;
            }
        }
    }
    [Command("c", "compress", "tarcompress", Description = "creating tar archive")]
    [HelpOption]
    class TarCompressCommand
    {
        [Option("-b|--base-directory=<BASE_DIRECTORY>", "extract base directory(if not specified, using current directory)", CommandOptionType.SingleValue)]
        public string BaseDirectory { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(if not specified, using stdout)", CommandOptionType.SingleValue)]
        public string OutputPath { get; set; }
        [Option("-i|--include=<INCLUDE_PATTERN>", "include file patterns(default: \"**/*\")", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("-x|--exclude=<EXCLUDE_PATTERN>", "exclude file patterns(default: none", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-e|--encoding=<ENCODING_NAME>", "file encoding name(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("--prefix=<PREFIX>", "path prefix, you must end with path separator if you want to add directory prefix", CommandOptionType.SingleValue)]
        public string Prefix { get; set; }
        public int OnExecute(IConsole con)
        {
            try
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                if (Excludes != null && Excludes.Length != 0)
                {
                    matcher.AddExcludePatterns(Excludes);
                }
                if (Includes != null && Includes.Length != 0)
                {
                    matcher.AddIncludePatterns(Includes);
                }
                else
                {
                    matcher.AddInclude("**/*");
                }
                var di = new DirectoryInfo(string.IsNullOrEmpty(BaseDirectory) ? Directory.GetCurrentDirectory() : BaseDirectory);
                var result = matcher.Execute(new DirectoryInfoWrapper(di));
                if (result.HasMatches)
                {
                    using (var ostm = Util.OpenOutputStream(OutputPath, true))
                    {
                        var twopt = new TarWriterOptions(SharpCompress.Common.CompressionType.None, true);
                        twopt.ArchiveEncoding = new SharpCompress.Common.ArchiveEncoding()
                        {
                            Default = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8)
                        };
                        using (var tar = new SharpCompress.Writers.Tar.TarWriter(ostm, twopt))
                        {
                            foreach (var fileInfo in result.Files)
                            {
                                var fi = new FileInfo(Path.Combine(di.FullName, fileInfo.Path));
                                var targetPath = string.IsNullOrEmpty(Prefix) ? fileInfo.Stem : Prefix + fileInfo.Stem;
                                con.Error.WriteLine($"'{fi.FullName}' -> '{targetPath}'");
                                tar.Write(targetPath, fi);
                            }
                        }
                    }
                    return 0;
                }
                else
                {
                    con.Error.WriteLine($"no file matched");
                    return 2;
                }
            }
            catch (Exception e)
            {
                con.Error.WriteLine($"failed to creating tar archive:{e}");
                return 1;
            }
        }
        void BySharpZipLib()
        {
            using(var stm = new MemoryStream())
            using(var ar = new ICSharpCode.SharpZipLib.Tar.TarInputStream(stm))
            {
                do
                {
                    var entry = ar.GetNextEntry();
                    if(entry != null)
                    {
                    }
                }while(true);
            }
        }
    }
}