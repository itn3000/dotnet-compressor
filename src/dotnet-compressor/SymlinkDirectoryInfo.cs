using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dotnet_compressor
{
    class SymlinkFileInfo : FileInfoBase
    {
        readonly bool _FollowSymlink;
        readonly string _Name;
        readonly string _FullName;
        public SymlinkFileInfo(string name, string fullName, bool followSymlink)
        {
            _Name = name;
            _FullName = fullName;
        }
        public override string Name => _Name;

        public override string FullName => _FullName;

        public override DirectoryInfoBase ParentDirectory => new SymlinkDirectoryInfo(new DirectoryInfo(Path.GetDirectoryName(_FullName)), _FollowSymlink);
    }
    class SymlinkDirectoryInfo : DirectoryInfoBase
    {
        DirectoryInfo _Directory;
        bool _FollowSymlink = false;
        public SymlinkDirectoryInfo(DirectoryInfo directory, bool followSymblink)
        {
            _Directory = directory;
            _FollowSymlink = followSymblink;
        }

        public override string Name => _Directory.Name;

        public override string FullName => _Directory.FullName;

        public override DirectoryInfoBase ParentDirectory => new SymlinkDirectoryInfo(_Directory.Parent, _FollowSymlink);

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            foreach(var fsi in _Directory.EnumerateFileSystemInfos())
            {
                if(!_FollowSymlink)
                {
                    if(Util.IsSymlink(fsi, out var isDirectory))
                    {
                        yield return new SymlinkFileInfo(fsi.Name, fsi.FullName, _FollowSymlink);
                    }
                    else
                    {
                        if(isDirectory)
                        {
                            yield return new SymlinkDirectoryInfo(new DirectoryInfo(fsi.FullName), _FollowSymlink);
                        }
                        else
                        {
                            yield return new SymlinkFileInfo(fsi.Name, fsi.FullName, _FollowSymlink);
                        }
                    }
                }
                else
                {
                    if((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        yield return new SymlinkDirectoryInfo(new DirectoryInfo(fsi.FullName), _FollowSymlink);
                    }
                    else
                    {
                        yield return new SymlinkFileInfo(fsi.Name, fsi.FullName, _FollowSymlink);
                    }
                }
            }
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            return new SymlinkDirectoryInfo(new DirectoryInfo(Path.Combine(_Directory.FullName, path)), _FollowSymlink);
        }

        public override FileInfoBase GetFile(string path)
        {
            var fi = new FileInfo(Path.Combine(_Directory.FullName, path));
            return new SymlinkFileInfo(fi.Name, fi.FullName, _FollowSymlink);
        }
    }
}