using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using SharpCompress.Compressors.LZMA;

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
        [Option("--replace-from=<REGEXP>", "replace filename regexp pattern", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; }
        [Option("--replace-to=<REPLACE_TO>", "replace filename destination regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; }
        [Option("-c|--compression-format=<COMPRESSION_FORMAT>", "decompress before tar extraction(possible values: gzip, bzip2, lzip)", CommandOptionType.SingleValue)]
        public string CompressionFormat { get; set; }
        public int OnExecute(IConsole console)
        {
            try
            {
                var enc = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8);
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
                using (var istm = TarUtil.GetCompressionStream(Util.OpenInputStream(InputPath), CompressionFormat, TarStreamDirection.Input))
                using (var tstm = new TarInputStream(istm, enc))
                {
                    while (true)
                    {
                        var entry = tstm.GetNextEntry();
                        if (entry == null)
                        {
                            break;
                        }
                        var m = matcher.Match(entry.Name);
                        if (!m.HasMatches)
                        {
                            continue;
                        }
                        if (ListOnly)
                        {
                            console.WriteLine($"{entry.Name}");
                            continue;
                        }
                        var entryKey = Util.ReplaceRegexString(entry.Name, ReplaceFrom, ReplaceTo);
                        if (entry.IsDirectory)
                        {
                            var destdir = Path.Combine(outdir, entryKey);
                            if (!Directory.Exists(destdir))
                            {
                                Directory.CreateDirectory(destdir);
                            }
                        }
                        else
                        {
                            var destfi = new FileInfo(Path.Combine(outdir, entryKey));
                            console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}");
                            if (!destfi.Directory.Exists)
                            {
                                destfi.Directory.Create();
                            }
                            using (var deststm = File.Create(destfi.FullName))
                            {
                                if (entry.Size != 0)
                                {
                                    var buf = new byte[4096];
                                    while (true)
                                    {
                                        var bytesread = tstm.Read(buf, 0, buf.Length);
                                        if (bytesread == 0)
                                        {
                                            break;
                                        }
                                        deststm.Write(buf, 0, bytesread);
                                    }
                                }
                            }
                            destfi.LastWriteTime = entry.ModTime;
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
        [Option("--replace-from=<REGEXP>", "replace filename regexp pattern", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; }
        [Option("--replace-to=<REPLACE_TO>", "replace filename destination regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; }
        [Option("-c|--compression-format=<COMPRESSION_FORMAT>", "compress after tar archiving(possible values: gzip, bzip2, lzip)", CommandOptionType.SingleValue)]
        public string CompressionFormat { get; set; }
        public int OnExecute(IConsole con)
        {
            try
            {
                var enc = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8);
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
                    using (var ofstm = Util.OpenOutputStream(OutputPath, true))
                    using (var ostm = TarUtil.GetCompressionStream(ofstm, CompressionFormat, TarStreamDirection.Output))
                    using (var tstm = new TarOutputStream(ostm, enc))
                    {
                        foreach (var fileInfo in result.Files)
                        {
                            var fi = new FileInfo(Path.Combine(di.FullName, fileInfo.Path));
                            var theader = new TarHeader();
                            theader.ModTime = fi.LastWriteTime.ToUniversalTime();
                            var targetPath = Util.ReplaceRegexString(fileInfo.Stem, ReplaceFrom, ReplaceTo);
                            theader.Name = targetPath;
                            theader.Size = fi.Length;
                            var tentry = new TarEntry(theader);
                            tentry.Name = targetPath;
                            tstm.PutNextEntry(tentry);
                            con.Error.WriteLine($"'{fi.FullName}' -> '{targetPath}'");
                            if (fi.Length != 0)
                            {
                                using (var fstm = fi.OpenRead())
                                {
                                    fstm.CopyTo(tstm);
                                }
                                tstm.CloseEntry();
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
    }
    enum TarStreamDirection
    {
        Input,
        Output,
    }
    static class TarUtil
    {
        static public Stream GetCompressionStream(Stream stm, string compressionFormat, TarStreamDirection direction)
        {
            if (string.IsNullOrEmpty(compressionFormat))
            {
                return stm;
            }
            if (compressionFormat.Equals("gzip", StringComparison.OrdinalIgnoreCase))
            {
                if (direction == TarStreamDirection.Input)
                {
                    return new GZipInputStream(stm);
                }
                else
                {
                    return new GZipOutputStream(stm);
                }
            }
            else if (compressionFormat.Equals("bzip2", StringComparison.OrdinalIgnoreCase))
            {
                if (direction == TarStreamDirection.Input)
                {
                    return new BZip2InputStream(stm);
                }
                else
                {
                    return new BZip2OutputStream(stm);
                }
            }
            else if(compressionFormat.Equals("lzip", StringComparison.OrdinalIgnoreCase))
            {
                if(direction == TarStreamDirection.Input)
                {
                    return new LZipStream(stm, SharpCompress.Compressors.CompressionMode.Decompress);
                }
                else
                {
                    return new LZipStream(stm, SharpCompress.Compressors.CompressionMode.Compress);
                }
            }
            else
            {
                throw new Exception($"unknown format: {compressionFormat}");
            }
        }
    }
}