using System;
using Xunit;
using System.IO;
using System.Text;
using dotnet_compressor;
using System.Threading.Tasks;


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
        public async Task EncodingTest(string encodingName)
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
                retcode = await cmd.OnExecute(DefaultConsole.Instance, default);
                Assert.Equal(0, retcode);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.FileNameEncoding = encodingName;
                retcode = await decomp.OnExecute(DefaultConsole.Instance, default);
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
        public async Task RegexCompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(TarTest) + "Regex");
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.ReplaceFrom = "\\.txt$";
                cmd.ReplaceTo = ".md";
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                await decomp.OnExecute(new DummyConsole(), default);
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
        [InlineData("lzip", "tlz")]
        public async Task WithCompressionTest(string compressionFormat, string ext)
        {
            var testName = nameof(TarTest) + "Compression" + nameof(EncodingTest) + compressionFormat;
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.CompressionFormat = compressionFormat;
                cmd.OutputPath = Path.Combine(tmpDir, $"test.{ext}");
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.CompressionFormat = compressionFormat;
                await decomp.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.txt")));
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, Util.JapaneseFileName)));
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
        [Fact]
        public async Task RegexDecompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(TarTest) + "Regex");
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                cmd.Verbose = true;
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.ReplaceFrom = "\\.txt$";
                decomp.ReplaceTo = ".md";
                decomp.Verbose = true;
                await decomp.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.md")));
            }
            finally
            {
                Util.RemoveTestDir(nameof(TarTest) + "Regex");
            }
        }
    }
}
