using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using SharpCompress.Archives;
using SharpCompress.Writers.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.Crypto;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.IO.Compression;
using Microsoft.Extensions.FileSystemGlobbing;

namespace dotnet_compressor.Zip
{
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
        [Option("-p|--password <PASSWORD>", "encryption password, cannot use with --passenv option(default: none)", CommandOptionType.SingleValue)]
        public string Password { get; set; }
        [Option("--passenv <ENVIRONMENT_NAME>", "encryption password environment name, cannot use with --pass option(default: none)", CommandOptionType.SingleValue)]
        public string PassEnvironmentName { get; set; }
        [Option("-e|--encoding=<FILENAME_ENCODING>", "filename encoding in archive(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-l|--list", "output file list only then exit", CommandOptionType.NoValue)]
        public bool ListOnly { get; set; }
        [Option("--replace-from=<REGEXP>", "replace filename regexp pattern", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; } = "";
        [Option("--replace-to=<REPLACE_TO>", "replace filename destination regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; } = "";
        [Option("--verbose", "verbose output(default: false)", CommandOptionType.NoValue)]
        public bool Verbose { get; set; }
        Matcher GetMatcher()
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
            return matcher;
        }
        void ExtractFileEntry(ZipInputStream zstm, ZipEntry entry, string outdir, IConsole console, byte[] buf)
        {
            var entryName = !string.IsNullOrEmpty(ReplaceFrom) && !string.IsNullOrEmpty(ReplaceTo) ?
                Regex.Replace(entry.Name, ReplaceFrom, ReplaceTo) : entry.Name;
            var fi = new FileInfo(Path.Combine(outdir, entryName));
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            if (Verbose)
            {
                console.Error.WriteLine($"extracting {entry.Name} to {fi.FullName}");
            }
            long totalread = 0;

            using (var ofstm = File.Create(fi.FullName))
            {
                if (entry.Size >= -1)
                {
                    while (totalread < entry.Size)
                    {
                        var bytesread = zstm.Read(buf, 0, (int)Math.Min(entry.Size - totalread, buf.Length));
                        ofstm.Write(buf, 0, bytesread);
                        totalread += bytesread;
                    }
                }
                else
                {
                    while (true)
                    {
                        var bytesread = zstm.Read(buf, 0, buf.Length);
                        ofstm.Write(buf, 0, bytesread);
                        if (bytesread < buf.Length)
                        {
                            break;
                        }
                    }
                }
            }
            if (!OperatingSystem.IsWindows() && entry.HostSystem == Constants.HostUnix)
            {
                if ((entry.ExternalFileAttributes & Constants.S_IFMT) == Constants.S_IFREG)
                {
                    var permission = (entry.ExternalFileAttributes >> 16) & (Constants.S_IFMT ^ 0xffff);
                    fi.UnixFileMode = (UnixFileMode)permission;
                }
            }
        }
        public void OnExecute(IConsole console)
        {
            var outdir = !string.IsNullOrEmpty(OutputDirectory) ? OutputDirectory : Directory.GetCurrentDirectory();
            var enc = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8);
            var matcher = GetMatcher();
            using (var istm = Util.OpenInputStream(InputPath))
            using (var zstm = new ZipInputStream(istm, StringCodec.FromEncoding(enc)))
            {
                zstm.Password = Util.GetPasswordString(Password, PassEnvironmentName);
                var buf = new byte[8192];
                while (true)
                {
                    var entry = zstm.GetNextEntry();
                    if (entry == null)
                    {
                        break;
                    }
                    if (!matcher.Match(entry.Name).HasMatches)
                    {
                        continue;
                    }
                    if (ListOnly)
                    {
                        console.WriteLine($"{entry.Name}");
                    }
                    else if (entry.IsDirectory)
                    {
                        var entryName = Util.ReplaceRegexString(entry.Name, ReplaceFrom, ReplaceTo);
                        Directory.CreateDirectory(Path.Combine(outdir, entryName));
                    }
                    else if (entry.IsFile)
                    {
                        ExtractFileEntry(zstm, entry, outdir, console, buf);
                    }
                }
            }
        }
    }
}