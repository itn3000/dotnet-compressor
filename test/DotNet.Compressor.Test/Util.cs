using System;
using Xunit;
using System.IO;

namespace DotNet.Compressor.Test
{
    public static class Util
    {
        public static void RemoveTestDir(string testName)
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "dcomp-test", testName);
            if(Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }
        public static (string tmpRootDir, string sampleDir) CreateTestDir(string testName)
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "dcomp-test", testName);
            if(!Directory.Exists(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }
            var sampleDir = Path.Combine(tmpDir, "test");
            if(!Directory.Exists(sampleDir))
            {
                Directory.CreateDirectory(sampleDir);
            }
            // Japanese HIRAGANA LETTERs + '.txt'
            var japaneseFileName = new string(new char[]{ (char)0x3042, (char)0x3043, (char)0x3044 }) + ".txt";
            File.WriteAllText(Path.Combine(sampleDir, japaneseFileName), "aaaa");
            File.WriteAllText(Path.Combine(sampleDir, "abc.txt"), "hoge");
            return (tmpDir, sampleDir);
        }
    }
}
