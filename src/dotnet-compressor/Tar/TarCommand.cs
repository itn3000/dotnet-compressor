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
                    yield return new MyDirectoryInfo(di);
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
    [Command("d", "decompress", "decompresstar", Description = "extracting tar archive")]
    [HelpOption]
    class TarDecompressCommand
    {
        [Option("-o|--output=<OUTPUT_DIRECTORY>", "output directory(create if not exists)", CommandOptionType.SingleValue)]
        public string OutputDirectory { get; set; }
        [Option("-i|--input=<INPUT_FILE_PATH>", "input file path(if not specified, using stdin)", CommandOptionType.SingleValue)]
        public string InputPath { get; set; }

        [Option("--include", "pattern of extracting files(default: \"**/*\")", CommandOptionType.MultipleValue)]
        public string[] Includes { get; set; }
        [Option("--exclude", "pattern of extracting files(default: none)", CommandOptionType.MultipleValue)]
        public string[] Excludes { get; set; }
        [Option("-e|--encoding=<ENCODING_NAME>", "filename encoding in tar archive(default: utf-8)", CommandOptionType.SingleValue)]
        public string FileNameEncoding { get; set; }
        [Option("-l|--list", "list files only", CommandOptionType.NoValue)]
        public bool ListOnly { get; set; }
        [Option("--replace-from=<REGEXP>", "replace filename regexp pattern", CommandOptionType.SingleValue)]
        public string ReplaceFrom { get; set; }
        [Option("--replace-to=<REPLACE_TO>", "replace filename destination regexp, backreference is allowed by '\\[number]'", CommandOptionType.SingleValue)]
        public string ReplaceTo { get; set; }
        [Option("-c|--compression-format=<COMPRESSION_FORMAT>", "decompress before tar extraction(possible values: gzip, bzip2, lzip)", CommandOptionType.SingleValue)]
        public string CompressionFormat { get; set; }
        public int OnExecute(IConsole console)
        {
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
                            if (!Directory.Exists(destdir))
                            {
                                Directory.CreateDirectory(destdir);
                            }
                        }
                        else
                        {
                            var destfi = new FileInfo(Path.Combine(outdir, entryKey));
                            console.Error.WriteLine($"extracting {entry.Name} to {destfi.FullName}");
                            if (!destfi.Directory.Exists)
                            {
                                destfi.Directory.Create();
                            }
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
    }
    class PermissionMapElement
    {
        public Regex Re { get; set; }
        public int Permission { get; set; }
        public int? Uid { get; set; }
        public int? Gid { get; set; }
    }
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
            // default is 0644
            var (permission, uid, gid) = GetUnixPermission(targetPath, 0x1a4);
            theader.Mode = permission;
            if (uid.HasValue)
            {
                theader.UserId = uid.Value;
            }
            if (gid.HasValue)
            {
                theader.GroupId = gid.Value;
            }
            var tentry = new TarEntry(theader);
            tentry.Name = targetPath;
            tstm.PutNextEntry(tentry);
            if (Verbose)
            {
                con.Error.WriteLine($"'{fi.FullName}' -> '{targetPath}'({Convert.ToString(permission, 8)})");
            }
            if (fi.Length != 0)
            {
                using (var fstm = fi.OpenRead())
                {
                    fstm.CopyTo(tstm);
                }
                tstm.CloseEntry();
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
            var entry = new TarEntry(header);
            tarOutputStream.PutNextEntry(entry);
            tarOutputStream.CloseEntry();
            if(Verbose)
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
    enum TarStreamDirection
    {
        Input,
        Output,
    }
    static class TarUtil
    {
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