using System;
using Xunit;
using System.IO;


namespace DotNet.Compressor.Test
{
    public class ZipTest
    {
        [Fact]
        public void RegexTest()
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
                cmd.OnExecute(new DummyConsole());
                Assert.True(File.Exists(cmd.OutputPath));
            }
            finally
            {
                Util.RemoveTestDir(nameof(ZipTest) + "Regex");
            }
        }
    }
}
