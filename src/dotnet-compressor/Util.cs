using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Linq;

namespace dotnet_compressor
{
    static class Util
    {
        public static Encoding GetEncodingFromName(string name, Encoding defaultEncoding = null)
        {
            if(defaultEncoding == null)
            {
                defaultEncoding = Encoding.Default;
            }
            if(int.TryParse(name, out var cp))
            {
                return Encoding.GetEncoding(cp);
            }
            else
            {
                return Encoding.GetEncoding(name);
            }
        }
        public static Stream OpenOutputStream(string filePath, bool createNew)
        {
            if(string.IsNullOrEmpty(filePath))
            {
                return Console.OpenStandardOutput();
            }
            else
            {
                if(createNew)
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
            if(string.IsNullOrEmpty(filePath))
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
            if(excludes != null)
            {
            foreach(var exclude in excludes)
            {
                matcher.AddExclude(exclude);
            }
            }
            if(includes != null)
            {
            foreach(var include in includes)
            {
                matcher.AddInclude(include);
            }
            }
            var di = new DirectoryInfo(basedir);
            var diwrapper = new DirectoryInfoWrapper(di);
            var result = matcher.Execute(diwrapper);
            if(result.HasMatches)
            {
                return Array.Empty<(string, string)>();
            }
            else
            {
                return result.Files.Select(x => (x.Path, x.Stem));
            }
        }
    }
}