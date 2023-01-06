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
        MyDirectoryInfo _Parent;
        public MyDirectoryInfo(DirectoryInfo di)
        {
            _Directory = di;
            _Parent = di.Parent != null ? new MyDirectoryInfo(di.Parent) : null;
        }
        public override string Name => _Directory.Name;

        public override string FullName => _Directory.FullName;

        public override DirectoryInfoBase ParentDirectory => _Parent;

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

        public override DirectoryInfoBase GetDirectory(string path)
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

    class PermissionMapElement
    {
        public Regex Re { get; set; }
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