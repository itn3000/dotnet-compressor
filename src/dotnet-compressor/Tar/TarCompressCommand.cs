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
    [Command("c", "compress", "tarcompress", Description = "creating tar archive")]
    [HelpOption]
    class TarCompressCommand
    {
        [Option("-b|--base-directory=<BASE_DIRECTORY>", "extract base directory(if not specified, using current directory)", CommandOptionType.SingleValue)]
        public string BaseDirectory { get; set; }
        [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(if not specified, using stdout)", CommandOptionType.SingleValue)]
        public string OutputPath { get; set; }
        [Option("-i|--include=<INCLUDE_PATTERN>", "include file patterns(default: \"**/*\")", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("-x|--exclude=<EXCLUDE_PATTERN>", "exclude file patterns(default: none", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-e|--encoding=<ENCODING_NAME>", "file encoding name(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("--replace-from=<REGEXP>", "replace filename regexp pattern", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; }
        [Option("--replace-to=<REPLACE_TO>", "replace filename destination regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; }
        [Option("-c|--compression-format=<COMPRESSION_FORMAT>", "compress after tar archiving(possible values: gzip, bzip2, lzip)", CommandOptionType.SingleValue)]
        public string CompressionFormat { get; set; }
        [Option("-pm|--permission-map=<MAP_ELEMENT>", "entry permission mapping(format is '[regex]=[permission number(octal)]:[uid(in decimal, optional)]:[gid(in decimal, optional)]', default: 644(file),755(directory)", CommandOptionType.MultipleValue)]
        public string[] PermissionStrings { get; set; }
        [Option("-pf|--permission-file=<MAP_FILE>", "entry permission mapping(format is same as '--permission-map' option, one mapping per line)", CommandOptionType.SingleValue)]
        public string PermissionMapFile { get; set; }
        List<PermissionMapElement> _PermissionMap = new List<PermissionMapElement>();
        [Option("-r|--retry", "retry count(default: 5)", CommandOptionType.SingleValue)]
        public string RetryNumString { get; set; }
        [Option("--stop-on-error", "stop on compression error in adding file entry(default: false)", CommandOptionType.NoValue)]
        public bool StopOnError { get; set; } = false;
        [Option("--verbose", "verbose output(default: false)", CommandOptionType.NoValue)]
        public bool Verbose { get; set; }
        (int permission, int? uid, int? gid) GetUnixPermission(string entryName, int defaultValue)
        {
            foreach (var perm in _PermissionMap)
            {
                if (perm.Re != null)
                {
                    var m = perm.Re.Match(entryName);
                    if (m.Success)
                    {
                        return (perm.Permission, perm.Uid, perm.Gid);
                    }
                }
            }
            return (defaultValue, null, null);
        }
        PermissionMapElement ParsePermissionMapElement(string s)
        {
            var idx = s.LastIndexOf('=');
            if (idx != -1)
            {
                var re = new Regex(s.Substring(0, idx));
                var values = s.Substring(idx + 1).Split(':');
                var permission = Convert.ToInt32(values[0], 8);
                int? uid = null;
                if (values.Length >= 2)
                {
                    uid = Convert.ToInt32(values[1], 8);
                }
                int? gid = null;
                if (values.Length >= 3)
                {
                    gid = Convert.ToInt32(values[2], 8);
                }
                return new PermissionMapElement()
                {
                    Re = re,
                    Permission = permission,
                    Uid = uid,
                    Gid = gid,
                };
            }
            else
            {
                return new PermissionMapElement()
                {
                    // 0644
                    Permission = 0x1a4,
                    Uid = null,
                    Gid = null,
                };
            }
        }
        void InitializePermissionMap()
        {
            var tmpList = new List<PermissionMapElement>();
            if (PermissionStrings != null && PermissionStrings.Length != 0)
            {
                foreach (var perm in PermissionStrings)
                {
                    tmpList.Add(ParsePermissionMapElement(perm));
                }
            }
            if (!string.IsNullOrEmpty(PermissionMapFile))
            {
                using (var fstm = File.OpenRead(PermissionMapFile))
                using (var treader = new StreamReader(fstm))
                {
                    while (true)
                    {
                        var l = treader.ReadLine();
                        tmpList.Add(ParsePermissionMapElement(l));
                    }
                }
            }
            tmpList.Reverse();
            _PermissionMap = tmpList;
        }
        void AddFileEntry(TarOutputStream tstm, IConsole con, FilePatternMatch fileInfo, FileInfo fi)
        {
            var theader = new TarHeader();
            theader.ModTime = fi.LastWriteTime.ToUniversalTime();
            var targetPath = Util.ReplaceRegexString(fileInfo.Stem, ReplaceFrom, ReplaceTo);
            theader.Name = targetPath;
            theader.Size = fi.Length;
            var (permission, uid, gid) = OperatingSystem.IsWindows() ? GetUnixPermission(targetPath, 0x1a4) : GetUnixPermission(targetPath, (int)fi.UnixFileMode);
            // default is 0644
            // var (permission, uid, gid) = GetUnixPermission(targetPath, 0x1a4);
            theader.Mode = permission;
            if (uid.HasValue)
            {
                theader.UserId = uid.Value;
            }
            if (gid.HasValue)
            {
                theader.GroupId = gid.Value;
            }
            if (Verbose)
            {
                con.Error.WriteLine($"'{fi.FullName}' -> '{targetPath}'({Convert.ToString(permission, 8)})");
            }
            if ((fi.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                theader.Size = 0;
                theader.LinkName = fi.LinkTarget;
                theader.Mode = 0x1ff;
                theader.TypeFlag = (int)TarTypeFlag.Symlink;
                var tentry = new TarEntry(theader);
                tentry.Name = targetPath;
                tstm.PutNextEntry(tentry);
                tstm.CloseEntry();
            }
            else
            {
                theader.TypeFlag = (int)TarTypeFlag.Regular;
                var tentry = new TarEntry(theader);
                tentry.Name = targetPath;
                tstm.PutNextEntry(tentry);
                if (fi.Length != 0)
                {
                    using (var fstm = fi.OpenRead())
                    {
                        fstm.CopyTo(tstm);
                    }
                    tstm.CloseEntry();
                }
            }
        }
        uint GetRetryNum()
        {
            if (!string.IsNullOrEmpty(RetryNumString) && !uint.TryParse(RetryNumString, out var retryNum))
            {
                return retryNum;
            }
            else
            {
                return 5;
            }
        }
        void AddDirectoryEntry(TarOutputStream tarOutputStream, DirectoryInfo directory, FilePatternMatch m, IConsole console)
        {
            var header = new TarHeader();
            var targetPath = Util.ReplaceRegexString(m.Stem, ReplaceFrom, ReplaceTo) + "/";
            header.Name = targetPath;
            header.ModTime = directory.LastWriteTimeUtc;
            var (permission, uid, gid) = GetUnixPermission(targetPath, 0x1ed);
            header.Mode = permission;
            header.TypeFlag = (byte)'5';
            if (uid.HasValue)
            {
                header.UserId = uid.Value;
            }
            if (gid.HasValue)
            {
                header.GroupId = gid.Value;
            }
            if((directory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                header.LinkName = directory.LinkTarget;
                header.Mode = 0x1ff;
                header.TypeFlag = (int)TarTypeFlag.Symlink;
                header.Name = header.Name.TrimEnd('/');
                console.Error.WriteLine($"{header.Name} -> {header.LinkName}");
            }
            else
            {
                header.TypeFlag = (int)TarTypeFlag.Directory;
            }
            var entry = new TarEntry(header);
            tarOutputStream.PutNextEntry(entry);
            tarOutputStream.CloseEntry();
            if (Verbose)
            {
                console.Error.WriteLine($"'{directory.FullName}' -> '{targetPath}'({Convert.ToString(permission, 8)})");
            }
        }
        public int OnExecute(IConsole con)
        {
            try
            {
                var RetryNum = GetRetryNum();
                InitializePermissionMap();
                var enc = Util.GetEncodingFromName(FileNameEncoding, Encoding.UTF8);
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                if (Excludes != null && Excludes.Length != 0)
                {
                    matcher.AddExcludePatterns(Excludes);
                }
                if (Includes != null && Includes.Length != 0)
                {
                    matcher.AddIncludePatterns(Includes);
                }
                else
                {
                    matcher.AddInclude("**/*");
                }
                var di = new DirectoryInfo(string.IsNullOrEmpty(BaseDirectory) ? Directory.GetCurrentDirectory() : BaseDirectory);
                var result = matcher.Execute(new MyDirectoryInfo(di));
                using (var ofstm = Util.OpenOutputStream(OutputPath, true))
                using (var ostm = TarUtil.GetCompressionStream(ofstm, CompressionFormat, TarStreamDirection.Output))
                using (var tstm = new TarOutputStream(ostm, enc))
                {
                    foreach (var fileInfo in result.Files)
                    {
                        var filePath = Path.Combine(di.FullName, fileInfo.Path);
                        if ((File.GetAttributes(filePath) & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            AddDirectoryEntry(tstm, new DirectoryInfo(filePath), fileInfo, con);
                        }
                        else
                        {
                            Exception exception = null;
                            for (int i = 0; i < RetryNum; i++)
                            {
                                exception = null;
                                var fi = new FileInfo(Path.Combine(di.FullName, fileInfo.Path));
                                if (fi.Exists)
                                {
                                    try
                                    {
                                        AddFileEntry(tstm, con, fileInfo, fi);
                                    }
                                    catch (Exception e)
                                    {
                                        exception = e;
                                        if(Verbose)
                                        {
                                            con.Error.WriteLine($"{fi.FullName}: {e}");
                                        }
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (Verbose)
                                    {
                                        con.Error.WriteLine($"{fi.FullName} does not exist, skipped");
                                    }
                                }
                                if (exception != null)
                                {
                                    con.Error.WriteLine($"add file entry error({i}, {fi.FullName}), retry: {exception}");
                                }
                                else
                                {
                                    con.Error.WriteLine("break");
                                    break;
                                }
                            }
                            if (StopOnError)
                            {
                                throw new Exception("retry num exceed", exception);
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                con.Error.WriteLine($"failed to creating tar archive:{e}");
                return 1;
            }
        }
    }
}