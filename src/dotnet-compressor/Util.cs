using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace dotnet_compressor
{
    static class Util
    {
        public static Encoding GetEncodingFromName(string name, Encoding defaultEncoding = null)
        {
            if (defaultEncoding == null)
            {
                defaultEncoding = Encoding.Default;
            }
            if (int.TryParse(name, out var cp))
            {
                return Encoding.GetEncoding(cp);
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    return Encoding.GetEncoding(name);
                }
                else
                {
                    return defaultEncoding;
                }
            }
        }
        public static Stream OpenOutputStream(string filePath, bool createNew)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Console.OpenStandardOutput();
            }
            else
            {
                if (createNew)
                {
                    return File.Create(filePath);
                }
                else
                {
                    return File.Open(filePath, FileMode.Append);
                }
            }
        }
        public static Stream OpenInputStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Console.OpenStandardInput();
            }
            else
            {
                return File.OpenRead(filePath);
            }
        }
        public static IEnumerable<(string Path, string Stem)> GetFileList(string basedir, string[] includes, string[] excludes, bool ignoreCase)
        {
            var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher(ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            if (excludes != null)
            {
                foreach (var exclude in excludes)
                {
                    matcher.AddExclude(exclude);
                }
            }
            if (includes != null && includes.Length != 0)
            {
                foreach (var include in includes)
                {
                    matcher.AddInclude(include);
                }
            }
            else
            {
                matcher.AddInclude("**/*");
            }
            var di = new DirectoryInfo(basedir);
            var diwrapper = new DirectoryInfoWrapper(di);
            var result = matcher.Execute(diwrapper);
            if (!result.HasMatches)
            {
                return Array.Empty<(string, string)>();
            }
            else
            {
                return result.Files.Select(x => (x.Path, x.Stem));
            }
        }
        public static string GetPasswordString(string password, string passEnvironmentName)
        {
            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(passEnvironmentName))
            {
                throw new Exception("cannot use both '--password' and '--passenv' option");
            }
            if (!string.IsNullOrEmpty(password))
            {
                return password;
            }
            else if (!string.IsNullOrEmpty(passEnvironmentName))
            {
                return Environment.GetEnvironmentVariable(passEnvironmentName);
            }
            else
            {
                return null;
            }
        }
        public static string ReplaceRegexString(string input, string pattern, string replaceto)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(replaceto))
            {
                return input;
            }
            return Regex.Replace(input, pattern, replaceto);
        }
        public static bool IsSymlink(FileSystemInfo fsi, out bool isDirectory)
        {
            if ((fsi.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                isDirectory = (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                return true;
            }
            isDirectory = (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            return false;
        }
        public static void CreateSymlink(string linkName, string targetName)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var relpath = Path.GetRelativePath(linkName, targetName);
                var flag = Directory.Exists(relpath) ? NativeApi.SYMBOLIC_LINK_FLAG_DIRECTORY : 0;
                if (!NativeApi.CreateSymbolicLinkW(linkName, targetName, flag))
                {
                    var er = NativeApi.GetLastError();
                    throw new InvalidOperationException($"failed to create symbolic link({er}): {targetName} - {linkName}");
                }
            }
            else
            {
                var ret = Mono.Unix.Native.Syscall.symlink(targetName, linkName);
                if(ret != 0)
                {
                    throw new InvalidOperationException($"failed to create symbolic link({ret}): {targetName} - {linkName}");
                }
            }
        }
    }
}