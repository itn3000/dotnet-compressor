using System;
using Xunit;
using System.IO;
using System.Text;


namespace DotNet.Compressor.Test
{
    public class TarTestFixture : IDisposable
    {
        public TarTestFixture()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public void Dispose()
        {
        }
    }
    public class TarTest: IClassFixture<TarTestFixture>
    {
        [Theory]
        [InlineData("shift-jis")]
        [InlineData("utf-8")]
        [InlineData("65001")]
        public void EncodingTest(string encodingName)
        {
            var testName = nameof(TarTest) + "Encoding" + nameof(EncodingTest) + encodingName;
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                int retcode;
                cmd.BaseDirectory = sampleDir;
                cmd.FileNameEncoding = encodingName;
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                retcode = cmd.OnExecute(new DummyConsole());
                Assert.Equal(0, retcode);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.FileNameEncoding = encodingName;
                retcode = decomp.OnExecute(new DummyConsole());
                Assert.Equal(0, retcode);
                var samplefi = new FileInfo(Path.Combine(sampleDir, "abc.txt"));
                var decompfi = new FileInfo(Path.Combine(decomp.OutputDirectory, "abc.txt"));
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.txt")));
                Assert.True(decompfi.Exists);
                Assert.Equal(samplefi.Length, decompfi.Length);
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, Util.JapaneseFileName)));
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
        [Fact]
        public void RegexCompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(TarTest) + "Regex");
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.ReplaceFrom = "\\.txt$";
                cmd.ReplaceTo = ".md";
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                cmd.OnExecute(new DummyConsole());
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.md")));
            }
            finally
            {
                Util.RemoveTestDir(nameof(TarTest) + "Regex");
            }
        }
        [Theory]
        [InlineData("gzip", "tgz")]
        [InlineData("bzip2", "tbz")]
        public void WithCompressionTest(string compressionFormat, string ext)
        {
            var testName = nameof(TarTest) + "Compression" + nameof(EncodingTest) + compressionFormat;
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.CompressionFormat = compressionFormat;
                cmd.OutputPath = Path.Combine(tmpDir, $"test.{ext}");
                cmd.OnExecute(new DummyConsole());
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.CompressionFormat = compressionFormat;
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.txt")));
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, Util.JapaneseFileName)));
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
        [Fact]
        public void RegexDecompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(TarTest) + "Regex");
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                cmd.OnExecute(new DummyConsole());
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.ReplaceFrom = "\\.txt$";
                decomp.ReplaceTo = ".md";
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.md")));
            }
            finally
            {
                Util.RemoveTestDir(nameof(TarTest) + "Regex");
            }
        }
    }
}
