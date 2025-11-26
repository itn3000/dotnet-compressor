using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using SharpCompress.Compressors.LZMA;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_compressor.Tar
{
    class TarDecompressCommand
    {
        public string? OutputDirectory { get; set; }
        public string? InputPath { get; set; }

        public string[]? Includes { get; set; }
        public string[]? Excludes { get; set; }
        public string? FileNameEncoding { get; set; }
        public bool ListOnly { get; set; } = false;
        public string? ReplaceFrom { get; set; }
        public string? ReplaceTo { get; set; }
        public string? CompressionFormat { get; set; }
        public bool Verbose { get; set; } = false;
        void ExtractFileEntry(TarInputStream tstm, string outdir, string entryKey, IConsole console, TarEntry entry)
        {
            var destfi = new FileInfo(Path.Combine(outdir, entryKey));
            console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}({entry.TarHeader.TypeFlag})");
            if (destfi.Directory != null && !destfi.Directory.Exists)
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
        public async Task<int> OnExecute(IConsole console, CancellationToken token)
        {
            await Task.Yield();
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
            if(Verbose)
            {
                console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}({entry.TarHeader.TypeFlag})");
            }
            if (destfi.Directory != null && !destfi.Directory.Exists)
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