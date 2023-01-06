using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using SharpCompress.Compressors.LZMA;

namespace dotnet_compressor.Tar
{
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
        void ExtractFileEntry(TarInputStream tstm, string outdir, string entryKey, IConsole console, TarEntry entry)
        {
            var destfi = new FileInfo(Path.Combine(outdir, entryKey));
            console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}({entry.TarHeader.TypeFlag})");
            if (!destfi.Directory.Exists)
            {
                destfi.Directory.Create();
            }
            if (entry.TarHeader.TypeFlag == (int)TarTypeFlag.Symlink)
            {
                var linkTarget = entry.TarHeader.LinkName;
                destfi.CreateAsSymbolicLink(linkTarget);
            }
            else
            {
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
            }
            destfi.LastWriteTime = entry.ModTime;
            if (!OperatingSystem.IsWindows())
            {
                destfi.UnixFileMode = (UnixFileMode)(entry.TarHeader.Mode & 0xfff);
            }
        }
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
                            if ((entry.TarHeader.Mode & TarUtil.S_IFLNK) != 0)
                            {
                                Directory.CreateSymbolicLink(destdir, entry.TarHeader.LinkName);
                            }
                            else
                            {
                                if (!Directory.Exists(destdir))
                                {
                                    Directory.CreateDirectory(destdir);
                                }
                            }
                        }
                        else
                        {
                            OutputTarEntryToFile(tstm, entry, entryKey, outdir, console);
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
        void OutputTarEntryToFile(TarInputStream tstm, TarEntry entry, string entryKey, string outdir, IConsole console)
        {
            var destfi = new FileInfo(Path.Combine(outdir, entryKey));
            console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}({entry.TarHeader.TypeFlag})");
            if (!destfi.Directory.Exists)
            {
                destfi.Directory.Create();
            }
            if (entry.TarHeader.TypeFlag == (int)TarTypeFlag.Symlink)
            {
                File.CreateSymbolicLink(destfi.FullName, entry.TarHeader.LinkName);
            }
            else
            {
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
                if (!OperatingSystem.IsWindows())
                {
                    destfi.UnixFileMode = (UnixFileMode)entry.TarHeader.Mode;
                }
            }
        }
    }
}