using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Writers.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.Crypto;
using System.IO;

namespace dotnet_compressor.Zip
{
    [Command("zip")]
    [HelpOption(Description = "zip compress or decompress")]
    class ZipCommand
    {
        public void OnExecute()
        {

        }
    }
    [HelpOption]
    class ZipCompressCommand
    {
        public string BasePath { get; set; }
        public string OutputPath { get; set; }
        public string[] Includes { get; set; }
        public string[] Excludes { get; set; }
        public string PassEnvironmentName { get; set; }
        public string FileNameEncoding { get; set; }
        public bool Encryption { get; set; }
        public bool IgnoreCase { get; set; }
        public bool UseZip64 { get; set; }
        public void OnExecute()
        {
            var basePath = !string.IsNullOrEmpty(BasePath) ? BasePath : Directory.GetCurrentDirectory();
            var wopt = new SharpCompress.Writers.Zip.ZipWriterOptions(CompressionType.Deflate);
            wopt.ArchiveEncoding = new ArchiveEncoding()
            {
                Default = Util.GetEncodingFromName(FileNameEncoding)
            };
            wopt.UseZip64 = UseZip64;
            using (var dest = Util.OpenOutputStream(OutputPath, true))
            using (var zw = new ZipWriter(dest, wopt))
            {
                foreach (var (path, stem) in Util.GetFileList(basePath, Includes, Excludes, IgnoreCase))
                {
                    var modTime = new FileInfo(path).LastWriteTime;
                    using(var istm = File.OpenRead(path))
                    {
                        var zwopt = new ZipWriterEntryOptions();
                        zw.Write(stem, istm, modTime);
                    }
                }
            }
        }
    }
}