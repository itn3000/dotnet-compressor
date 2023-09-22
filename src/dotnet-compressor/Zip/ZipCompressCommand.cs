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
        [Option("-p|--password <PASSWORD>", "encryption password, cannot use with --passenv option(default: none)", CommandOptionType.SingleValue)]
        public string Password { get; set; }
        [Option("--passenv <ENVIRONMENT_NAME>", "encryption password environment name, cannot use with --pass option(default: none)", CommandOptionType.SingleValue)]
        public string PassEnvironmentName { get; set; }
        [Option("-e|--encoding <ENCODING_NAME>", "filename encoding in archive(default: system default)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-E|--encryption", "flag for encryption, you must specify passenv option too(default: false)", CommandOptionType.NoValue)]
        public bool Encryption { get; set; }
        [Option("--case-sensitive", "flag for case sensitivity on includes and excludes option(default: false)", CommandOptionType.NoValue)]
        public bool CaseSensitive { get; set; }
        [Option("--level=<COMPRESSION_LEVEL>", "compression level(between 0 and 9)", CommandOptionType.SingleValue)]
        public string CompressionLevelString { get; set; }
        int CompressionLevel => !string.IsNullOrEmpty(CompressionLevelString) ? int.Parse(CompressionLevelString) : -1;
        [Option("--replace-from=<REGEXP>", "replace filename source regexp", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; }
        [Option("--replace-to=<REPLACE_TO>", "replace filename dest regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; }
        [Option("-r|--retry", "retry count(default: 5)", CommandOptionType.SingleValue)]
        public string RetryNumString { get; set; }
        [Option("--stop-on-error", "stop compression on error in adding file entry(default: false)", CommandOptionType.NoValue)]
        public bool StopOnError { get; set; } = false;
        [Option("--verbose", "verbose output(default: false)", CommandOptionType.NoValue)]
        public bool Verbose { get; set; }
        uint GetRetryNum()
        {
            if (!string.IsNullOrEmpty(RetryNumString) && uint.TryParse(RetryNumString, out var retryNum))
            {
                return retryNum;
            }
            else
            {
                return 5;
            }
        }
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
        public int OnExecute(IConsole console)
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
                        Exception exception = null;
                        var RetryNum = GetRetryNum();
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
        string GetPasswordString()
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