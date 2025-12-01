using System;
using Xunit;
using System.IO;
using System.Threading.Tasks;


namespace DotNet.Compressor.Test
{
    public class ZipTest
    {
        public ZipTest()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
        [Theory]
        [InlineData("sjis")]
        [InlineData("utf-8")]
        [InlineData("65001")]
        public async Task EncodingTest(string encodingName)
        {
            var testName = nameof(ZipTest) + "Regex" + nameof(EncodingTest) + encodingName;
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var cmd = new dotnet_compressor.Zip.ZipCompressCommand();
                cmd.BasePath = sampleDir;
                cmd.Encryption = false;
                cmd.FileNameEncoding = encodingName;
                cmd.OutputPath = Path.Combine(tmpDir, "test.zip");
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Zip.ZipDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.FileNameEncoding = encodingName;
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
        public async Task RegexCompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(ZipTest) + "Regex");
                var cmd = new dotnet_compressor.Zip.ZipCompressCommand();
                cmd.BasePath = sampleDir;
                cmd.Encryption = false;
                cmd.ReplaceFrom = "\\.txt$";
                cmd.ReplaceTo = ".md";
                cmd.OutputPath = Path.Combine(tmpDir, "test.zip");
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Zip.ZipDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                await decomp.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(Path.Combine(decomp.OutputDirectory, "abc.md")));
            }
            finally
            {
                Util.RemoveTestDir(nameof(ZipTest) + "Regex");
            }
        }
        [Fact]
        public async Task RegexDecompressionTest()
        {
            try
            {
                var (tmpDir, sampleDir) = Util.CreateTestDir(nameof(ZipTest) + "Regex");
                var cmd = new dotnet_compressor.Zip.ZipCompressCommand();
                cmd.BasePath = sampleDir;
                cmd.Encryption = false;
                cmd.OutputPath = Path.Combine(tmpDir, "test.zip");
                cmd.Verbose = true;
                await cmd.OnExecute(new DummyConsole(), default);
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Zip.ZipDecompressCommand();
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
                Util.RemoveTestDir(nameof(ZipTest) + "Regex");
            }
        }
    }
}
