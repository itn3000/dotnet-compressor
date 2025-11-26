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
using System.Threading.Tasks;
using System.Threading;

namespace dotnet_compressor.Zip
{
    class ZipCompressCommand
    {
        public string? BasePath { get; set; }
        public string? OutputPath { get; set; }
        public string[]? Includes { get; set; }
        public string[]? Excludes { get; set; }
        public string? Password { get; set; }
        public string? PassEnvironmentName { get; set; }
        public string? FileNameEncoding { get; set; }
        public bool Encryption { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public int CompressionLevel { get; set; } = -1;
        public string? ReplaceFrom { get; set; }
        public string? ReplaceTo { get; set; }
        public int RetryNum { get; set; } = 5;
        public bool StopOnError { get; set; } = false;
        public bool Verbose { get; set; } = false;
        void AddFileEntry(ZipOutputStream zstm, string stem, string path, IConsole console, FileInfo fi, bool isUtf8)
        {
            var entryName = ZipEntry.CleanName(Util.ReplaceRegexString(stem, ReplaceFrom, ReplaceTo));
            var zentry = new ZipEntry(entryName);
            if (Verbose)
            {
                console.Error.WriteLine($"{path} -> {entryName}");
            }
            if(isUtf8)
            {
                zentry.IsUnicodeText = true;
            }
            zentry.DateTime = fi.LastWriteTime;
            if (!OperatingSystem.IsWindows())
            {
                zentry.ExternalFileAttributes = ((int)fi.UnixFileMode | Constants.S_IFREG) << 16;
                zentry.HostSystem = Constants.HostUnix;
            }
            using (var istm = fi.OpenRead())
            {
                zentry.Size = istm.Length;
                zstm.PutNextEntry(zentry);
                istm.CopyTo(zstm);
            }
            zstm.CloseEntry();
        }
        public async Task<int> OnExecute(IConsole console, CancellationToken token)
        {
            try
            {
                var enc = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8);
                var isUnicode = enc.WebName.Equals(Encoding.UTF8.WebName, StringComparison.OrdinalIgnoreCase);
                var basePath = !string.IsNullOrEmpty(BasePath) ? BasePath : Directory.GetCurrentDirectory();
                using (var ostm = Util.OpenOutputStream(OutputPath, true))
                using (var zstm = new ZipOutputStream(ostm, StringCodec.FromEncoding(enc)))
                {
                    {
                        var pass = GetPasswordString();
                        if (!string.IsNullOrEmpty(pass))
                        {
                            zstm.Password = pass;
                        }
                    }
                    if (CompressionLevel >= 0)
                    {
                        zstm.SetLevel(CompressionLevel);
                    }

                    foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, !CaseSensitive))
                    {
                        token.ThrowIfCancellationRequested();
                        Exception? exception = null;
                        for (int i = 0; i < RetryNum; i++)
                        {
                            try
                            {
                                exception = null;
                                var fi = new FileInfo(Path.Combine(basePath, path));
                                if (fi.Exists)
                                {
                                    AddFileEntry(zstm, stem, path, console, fi, isUnicode);
                                }
                                else
                                {
                                    if (Verbose)
                                    {
                                        console.Error.WriteLine($"{fi.FullName} does not exist, skipped");
                                    }
                                }
                                break;
                            }
                            catch (Exception e)
                            {
                                console.Error.WriteLine($"failed to add entry({path}, {i}): {e}");
                                exception = e;
                            }
                        }
                        if (exception != null)
                        {
                            if (StopOnError)
                            {
                                throw new Exception("error num exceed, stopped", exception);
                            }
                            else
                            {
                                console.Error.WriteLine($"entry {path} skipped");
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                console.Error.WriteLine($"failed to creating zip archive:{e}");
                return 1;
            }
        }
        string? GetPasswordString()
        {
            if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(PassEnvironmentName))
            {
                throw new Exception("cannot use both '--password' and '--passenv' option");
            }
            if (!string.IsNullOrEmpty(Password))
            {
                return Password;
            }
            else if (!string.IsNullOrEmpty(PassEnvironmentName))
            {
                return Environment.GetEnvironmentVariable(PassEnvironmentName);
            }
            else
            {
                return null;
            }
        }
    }
}