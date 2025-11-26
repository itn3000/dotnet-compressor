using System.Text;
using System.IO;
using dotnet_compressor;
using Xunit;
using System.Threading.Tasks;

namespace DotNet.Compressor.Test
{
    public class TestCodecBZip2
    {
        [Fact]
        public async Task Codec()
        {
            var testName = nameof(TestCodecBZip2) + nameof(Codec) + "BZip2";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new BZip2Command();
                var compressInput = Path.Combine(sampleDir, "abc.txt");
                var compressOutput = Path.Combine(tmpDir, "comp.bin");
                await comp.Compress(input: compressInput, output: compressOutput, token: default);
                Assert.True(File.Exists(compressOutput));
                var decomp = new BZip2Command();
                var decompressionInput = compressOutput;
                var decompressionOutput = Path.Combine(tmpDir, "decomp.bin");
                await decomp.Decompress(input: decompressionInput, output: decompressionOutput, token: default);
                Assert.True(File.Exists(decompressionOutput));
                var originalBytes = File.ReadAllBytes(compressInput);
                var compBytes = File.ReadAllBytes(compressOutput);
                var decBytes = File.ReadAllBytes(decompressionOutput);
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
        public async Task Codec()
        {
            var testName = nameof(TestCodecGZip) + nameof(Codec) + "GZip";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new GZipCommand();
                var compressInput = Path.Combine(sampleDir, "abc.txt");
                var compressOutput = Path.Combine(tmpDir, "comp.bin");
                await comp.Compress(input: compressInput, output: compressOutput, token: default);
                Assert.True(File.Exists(compressOutput));
                var decomp = new GZipCommand();
                var decompressionInput = compressOutput;
                var decompressionOutput = Path.Combine(tmpDir, "decomp.bin");
                await decomp.Decompress(input: decompressionInput, output: decompressionOutput, token: default);
                Assert.True(File.Exists(decompressionOutput));
                var originalBytes = File.ReadAllBytes(compressInput);
                var compBytes = File.ReadAllBytes(compressOutput);
                var decBytes = File.ReadAllBytes(decompressionOutput);
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
        public async Task Codec()
        {
            var testName = nameof(TestCodecLZip) + nameof(Codec) + "LZip";
            var (tmpDir, sampleDir) = Util.CreateTestDir(testName);
            try
            {
                var comp = new LZipCommand();
                var compressInput = Path.Combine(sampleDir, "abc.txt");
                var compressOutput = Path.Combine(tmpDir, "comp.bin");
                await comp.Compress(input: compressInput, output: compressOutput, token: default);
                Assert.True(File.Exists(compressOutput));
                var decomp = new LZipCommand();
                var decompressionInput = compressOutput;
                var decompressionOutput = Path.Combine(tmpDir, "decomp.bin");
                await decomp.Decompress(input: decompressionInput, output: decompressionOutput, token: default);
                Assert.True(File.Exists(decompressionOutput));
                var originalBytes = File.ReadAllBytes(compressInput);
                var compBytes = File.ReadAllBytes(compressOutput);
                var decBytes = File.ReadAllBytes(decompressionOutput);
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