using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Collections.Generic;
using System.IO;

namespace dotnet_compressor
{
    class SymlinkFileSystemInfo : FileSystemInfoBase
    {
        readonly bool _FollowSymlink;
        readonly string _Name;
        readonly string _FullName;
        public SymlinkFileSystemInfo(string name, string fullName, bool followSymlink)
        {
            _Name = name;
            _FullName = fullName;
        }
        public override string Name => _Name;

        public override string FullName => _FullName;

        public override DirectoryInfoBase ParentDirectory => new SymlinkDirectoryInfoWrapper(new DirectoryInfo(Path.GetDirectoryName(_FullName)), _FollowSymlink);
    }
    class SymlinkDirectoryInfoWrapper : DirectoryInfoBase
    {
        DirectoryInfo _Directory;
        bool _FollowSymlink = false;
        public SymlinkDirectoryInfoWrapper(DirectoryInfo directory, bool followSymblink)
        {
            _Directory = directory;
            _FollowSymlink = followSymblink;
        }

        public override string Name => _Directory.Name;

        public override string FullName => _Directory.FullName;

        public override DirectoryInfoBase ParentDirectory => new SymlinkDirectoryInfoWrapper(_Directory.Parent, _FollowSymlink);

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {

            throw new System.NotImplementedException();
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            throw new System.NotImplementedException();
        }

        public override FileInfoBase GetFile(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}