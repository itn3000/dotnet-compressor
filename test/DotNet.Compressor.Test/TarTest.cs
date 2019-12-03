using System;
using Xunit;
using System.IO;


namespace DotNet.Compressor.Test
{
    public class TarTest
    {
        [Theory]
        [InlineData("sjis")]
        [InlineData("utf-8")]
        [InlineData("65001")]
        public void EncodingTest(string encodingName)
        {
            var testName = nameof(TarTest) + "Regex" + nameof(EncodingTest) + encodingName;
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var cmd = new dotnet_compressor.Tar.TarCompressCommand();
                cmd.BaseDirectory = sampleDir;
                cmd.FileNameEncoding = encodingName;
                cmd.OutputPath = Path.Combine(tmpDir, "test.tar");
                cmd.OnExecute(new DummyConsole());
                Assert.True(File.Exists(cmd.OutputPath));
                var decomp = new dotnet_compressor.Tar.TarDecompressCommand();
                decomp.InputPath = cmd.OutputPath;
                decomp.OutputDirectory = Path.Combine(tmpDir, "decomp");
                decomp.FileNameEncoding = encodingName;
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
