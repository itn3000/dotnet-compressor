using System.Text;
using System.IO;
using dotnet_compressor;
using Xunit;

namespace DotNet.Compressor.Test
{
    public class TestCodecBZip2
    {
        [Fact]
        public void Codec()
        {
            var testName = nameof(TestCodecBZip2) + nameof(Codec) + "BZip2";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new BZip2CompressCommand();
                comp.InputFile = Path.Combine(sampleDir, "abc.txt");
                comp.OutputFile = Path.Combine(tmpDir, "comp.bin");
                comp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(comp.OutputFile));
                var decomp = new BZip2DecompressCommand();
                decomp.InputFile = comp.OutputFile;
                decomp.OutputFile = Path.Combine(tmpDir, "decomp.bin");
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(decomp.OutputFile));
                var originalBytes = File.ReadAllBytes(comp.InputFile);
                var compBytes = File.ReadAllBytes(comp.OutputFile);
                var decBytes = File.ReadAllBytes(decomp.OutputFile);
                Assert.Equal(originalBytes, decBytes);
                Assert.NotEqual(decBytes, compBytes);
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
    }
    public class TestCodecGZip
    {
        [Fact]
        public void Codec()
        {
            var testName = nameof(TestCodecGZip) + nameof(Codec) + "GZip";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new GZipCompressCommand();
                comp.InputFile = Path.Combine(sampleDir, "abc.txt");
                comp.OutputFile = Path.Combine(tmpDir, "comp.bin");
                comp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(comp.OutputFile));
                var decomp = new GZipDecompressCommand();
                decomp.InputFile = comp.OutputFile;
                decomp.OutputFile = Path.Combine(tmpDir, "decomp.bin");
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(decomp.OutputFile));
                var originalBytes = File.ReadAllBytes(comp.InputFile);
                var compBytes = File.ReadAllBytes(comp.OutputFile);
                var decBytes = File.ReadAllBytes(decomp.OutputFile);
                Assert.Equal(originalBytes, decBytes);
                Assert.NotEqual(decBytes, compBytes);
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
    }
    public class TestCodecLZip
    {
        [Fact]
        public void Codec()
        {
            var testName = nameof(TestCodecLZip) + nameof(Codec) + "LZip";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new LZipCompressCommand();
                comp.InputFile = Path.Combine(sampleDir, "abc.txt");
                comp.OutputFile = Path.Combine(tmpDir, "comp.bin");
                comp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(comp.OutputFile));
                var decomp = new LZipDecompressCommand();
                decomp.InputFile = comp.OutputFile;
                decomp.OutputFile = Path.Combine(tmpDir, "decomp.bin");
                decomp.OnExecute(new DummyConsole());
                Assert.True(File.Exists(decomp.OutputFile));
                var originalBytes = File.ReadAllBytes(comp.InputFile);
                var compBytes = File.ReadAllBytes(comp.OutputFile);
                var decBytes = File.ReadAllBytes(decomp.OutputFile);
                Assert.Equal(originalBytes, decBytes);
                Assert.NotEqual(decBytes, compBytes);
            }
            finally
            {
                Util.RemoveTestDir(testName);
            }
        }
    }
}