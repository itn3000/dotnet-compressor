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
using System.Threading.Tasks;
using System.Threading;
using ConsoleAppFramework;

namespace dotnet_compressor.Tar
{
    class MyFileInfo : FileInfoBase
    {
        string _Name;
        string _FullName;
        DirectoryInfoBase _Parent;
        public MyFileInfo(string name, string fullName, DirectoryInfoBase parent)
        {
            _Name = name;
            _FullName = fullName;
            _Parent = parent;
        }
        public override string Name => _Name;

        public override string FullName => _FullName;

        public override DirectoryInfoBase ParentDirectory => _Parent;
    }
    class MyDirectoryInfo : DirectoryInfoBase
    {
        DirectoryInfo _Directory;
        MyDirectoryInfo? _Parent;
        public MyDirectoryInfo(DirectoryInfo? di)
        {
            ArgumentNullException.ThrowIfNull(di);
            _Directory = di;
            _Parent = di.Parent != null ? new MyDirectoryInfo(di?.Parent) : null;
        }
        public override string Name => _Directory.Name;

        public override string FullName => _Directory.FullName;

        public override DirectoryInfoBase? ParentDirectory => _Parent;

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            foreach (var fsi in _Directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                if (fsi is DirectoryInfo di)
                {
                    yield return new MyFileInfo(di.Name, di.FullName, new MyDirectoryInfo(_Directory));
                    if ((di.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        yield return new MyDirectoryInfo(di);
                    }
                }
                else if (fsi is FileInfo fi)
                {
                    yield return new MyFileInfo(fsi.Name, fsi.FullName, new MyDirectoryInfo(_Directory));
                }
            }
        }

        public override DirectoryInfoBase? GetDirectory(string path)
        {
            if (path.Equals("..", StringComparison.Ordinal))
            {
                return new MyDirectoryInfo(_Directory.Parent);
            }
            else
            {
                var retval = _Directory.GetDirectories(path);
                if (retval != null)
                {
                    if (retval.Length == 1)
                    {
                        return new MyDirectoryInfo(retval[0]);
                    }
                    else if (retval.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        throw new InvalidOperationException($"more than one subdirectories are found under {_Directory.FullName} with name {path}");
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public override FileInfoBase GetFile(string path)
        {
            throw new NotImplementedException();
        }
    }
    class TarCommand
    {
        /// <summary>
        /// creating tar archive
        /// </summary>
        /// <param name="baseDirectory">-b, compress base directory(if not specified, using current directory)</param>
        /// <param name="output">-o, output file path(if not specified, using stdout)</param>
        /// <param name="include">-i, include file patterns(default: \"**/*\")</param>
        /// <param name="exclude">-x, exclude file patterns(default: none)</param>
        /// <param name="encoding">-e, file encoding name(default: utf-8)</param>
        /// <param name="replaceFrom">replace filename regexp pattern</param>
        /// <param name="replaceTo">replace filename destination regexp, backreference is allowed by '\[number]'</param>
        /// <param name="compressionFormat">-c, compress after tar archiving(possible values: gzip, bzip2, lzip)</param>
        /// <param name="permissionMap">--pm, entry permission mapping(format is '[regex]=[permission number(octal)]:[uid(in decimal, optional)]:[gid(in decimal, optional)]', default: 644(file),755(directory)</param>
        /// <param name="permissionFile">--pf, entry permission mapping(format is same as '--permission-map' option, one mapping per line)</param>
        /// <param name="retryCount">--retry, retry count(default: 5)</param>
        /// <param name="stopOnError">stop on compression error in adding file entry(default: false)</param>
        /// <param name="verbose">verbose output(default: false)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Command("tar compress|tar c")]
        public async Task<int> Compress(string? baseDirectory = null, 
            string? output = null, 
            string[]? include = null, 
            string[]? exclude = null, 
            string? encoding = null,
            string? replaceFrom = null,
            string? replaceTo = null,
            string? compressionFormat = null,
            string[]? permissionMap = null,
            string? permissionFile = null,
            int retryCount = 5,
            bool stopOnError = false,
            bool verbose = false,
            CancellationToken token = default
            )
        {
            return await new TarCompressCommand()
            {
                BaseDirectory = baseDirectory,
                CompressionFormat = compressionFormat,
                OutputPath = output,
                Excludes = exclude,
                FileNameEncoding = encoding,
                Includes = include,
                PermissionMapFile = permissionFile,
                PermissionStrings = permissionMap,
                ReplaceFrom = replaceFrom,
                ReplaceTo = replaceTo,
                RetryNum = retryCount,
                StopOnError = stopOnError,
                Verbose = verbose
            }.OnExecute(DefaultConsole.Instance, token);
        }
        /// <summary>
        /// extracting tar archive
        /// </summary>
        /// <param name="output">-o, output directory(create if not exists)</param>
        /// <param name="input">-i, input file path(if not specified, using stdin)</param>
        /// <param name="include">pattern of extracting files(default: "**/*")</param>
        /// <param name="exclude">pattern of extracting files(default: none)</param>
        /// <param name="encoding">-e, filename encoding in tar archive(default: utf-8)</param>
        /// <param name="list">-l, list files only</param>
        /// <param name="replaceFrom">replace filename destination regexp, backreference is allowed by '\[number]'</param>
        /// <param name="replaceTo"></param>
        /// <param name="compressionFormat">-c, decompress before tar extraction(possible values: gzip, bzip2, lzip)</param>
        /// <param name="verbose">-v, verbose output(default: false)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Command("tar decompress|tar d")]
        public async Task<int> Decompress(string? output = null,
            string? input = null,
            string[]? include = null,
            string[]? exclude = null,
            string? encoding = null,
            bool list = false,
            string? replaceFrom = null,
            string? replaceTo = null,
            string? compressionFormat = null,
            bool verbose = false,
            CancellationToken token = default)
        {
            return await new TarDecompressCommand()
            {
                CompressionFormat = compressionFormat,
                Excludes = exclude,
                FileNameEncoding = encoding,
                Includes = include,
                InputPath = input,
                ListOnly = list,
                OutputDirectory = output,
                ReplaceFrom = replaceFrom,
                ReplaceTo = replaceTo,
                Verbose = verbose
            }.OnExecute(DefaultConsole.Instance, token);
        }
    }

    class PermissionMapElement
    {
        public Regex? Re { get; set; }
        public int Permission { get; set; }
        public int? Uid { get; set; }
        public int? Gid { get; set; }
    }
    enum TarStreamDirection
    {
        Input,
        Output,
    }
    enum TarTypeFlag
    {
        Regular = (int)'0',
        Symlink = (int)'2',
        Directory = (int)'5',
    }
    static class TarUtil
    {
        public const int S_IFLNK = 0xa000;
        static public Stream GetCompressionStream(Stream stm, string? compressionFormat, TarStreamDirection direction)
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
            else if (compressionFormat.Equals("lzip", StringComparison.OrdinalIgnoreCase))
            {
                if (direction == TarStreamDirection.Input)
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