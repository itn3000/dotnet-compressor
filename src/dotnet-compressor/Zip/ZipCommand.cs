using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;

namespace dotnet_compressor.Zip
{
    static class Constants
    {
        // from stat.st_mode
        public const int S_IFMT = 0xf000;
        public const int S_IFREG = 0x8000;
        // from https://support.pkware.com/home/pkzip/developer-tools/appnote/
        public const int HostMSDOS = 0;
        public const int HostWinNTFS = 10;
        public const int HostUnix = 3;
    }
    class ZipCommand
    {
        /// <summary>
        /// compress zip archive
        /// </summary>
        /// <param name="baseDirectory">-b|--base, input base path(default: current directory)</param>
        /// <param name="output">-o, output zip path(default: stdout)</param>
        /// <param name="include">-i, file include pattern(default: all files and directories)</param>
        /// <param name="exclude">-x, file exclude pattern(default: none)</param>
        /// <param name="password">-p, encryption password, cannot use with --passenv option(default: none)</param>
        /// <param name="passenv">encryption password environment name, cannot use with --pass option(default: none)</param>
        /// <param name="encoding">-e, filename encoding in archive(default: system default)</param>
        /// <param name="encryption">-E, flag for encryption, you must specify passenv option too(default: false)</param>
        /// <param name="caseSensitive">flag for case sensitivity on includes and excludes option(default: false)</param>
        /// <param name="level">-l, compression level(between 0 and 9)</param>
        /// <param name="replaceFrom">replace filename source regexp</param>
        /// <param name="replaceTo">replace filename dest regexp, backreference is allowed by '\[number]'</param>
        /// <param name="retryCount">retry count(default: 5)</param>
        /// <param name="stopOnError">stop compression on error in adding file entry(default: false)</param>
        /// <param name="verbose">verbose output(default: false)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Command("zip compress|zip c")]
        public async Task<int> Compress(string? baseDirectory = null,
            string? output = null,
            string[]? include = null,
            string[]? exclude = null,
            string? password = null,
            string? passenv = null,
            string? encoding = null,
            bool encryption = false,
            bool caseSensitive = false,
            int level = -1,
            string? replaceFrom = null,
            string? replaceTo = null,
            int retryCount = 5,
            bool stopOnError = false,
            bool verbose = false,
            CancellationToken token = default
            )
        {
            return await new ZipCompressCommand()
            {
                BasePath = baseDirectory,
                CaseSensitive = caseSensitive,
                CompressionLevel = level,
                Encryption = encryption,
                Excludes = exclude,
                FileNameEncoding = encoding,
                Includes = include,
                OutputPath = output,
                PassEnvironmentName = passenv,
                Password = password,
                ReplaceFrom = replaceFrom,
                ReplaceTo = replaceTo,
                RetryNum = retryCount,
                StopOnError = stopOnError,
                Verbose = verbose
            }.OnExecute(DefaultConsole.Instance, token);
        }
        /// <summary>
        /// decompress zip archive
        /// </summary>
        /// <param name="input">-i, input file path(default: stdin)</param>
        /// <param name="output">-o, output directory path(default: current directory)</param>
        /// <param name="include">extracting file include pattern(default: **/*)</param>
        /// <param name="exclude">extracting file exclude pattern(default: none)</param>
        /// <param name="password">-p, encryption password, cannot use with --passenv option(default: none)</param>
        /// <param name="passenv">encryption password environment name, cannot use with --pass option(default: none)</param>
        /// <param name="encoding">-e, filename encoding in archive(default: utf-8)</param>
        /// <param name="list">-l, output file list only then exit</param>
        /// <param name="replaceFrom">replace filename regexp pattern</param>
        /// <param name="replaceTo">replace filename destination regexp, backreference is allowed by '\\[number]'</param>
        /// <param name="verbose">-v, verbose output(default: false)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Command("zip decompress|zip d")]
        public async Task<int> Decompress(string? input = null,
            string? output = null,
            string[]? include = null,
            string[]? exclude = null,
            string? password = null,
            string? passenv = null,
            string? encoding = null,
            bool list = false,
            string? replaceFrom = null,
            string? replaceTo = null,
            bool verbose = false,
            CancellationToken token = default
            )
        {
            return await new ZipDecompressCommand()
            {
                Excludes = exclude,
                FileNameEncoding = encoding,
                Includes = include,
                InputPath = input,
                ListOnly = list,
                OutputDirectory = output,
                PassEnvironmentName = passenv,
                Password = password,
                ReplaceFrom = replaceFrom,
                ReplaceTo = replaceTo,
                Verbose = verbose,
            }.OnExecute(DefaultConsole.Instance, token);
        }

    }
}