using System;
using ICSharpCode.SharpZipLib.BZip2;
using System.Threading.Tasks;
using System.Threading;
using ConsoleAppFramework;

namespace dotnet_compressor
{
    class BZip2Command
    {
        /// <summary>
        /// do bzip2 compression
        /// </summary>
        /// <param name="input">input file path(default: standard input)</param>
        /// <param name="output">output file path(default: standard output)</param>
        /// <param name="token"></param>
        /// <returns>return 0 on success, 1 if error</returns>
        [Command("bzip2 compress|bzip2 c")]
        public async Task<int> Compress(string? input = null, string? output = null, CancellationToken token = default)
        {
            try
            {
                using(var istm = Util.OpenInputStream(input))
                using(var ostm = Util.OpenOutputStream(output, true))
                {
                    using(var ozstm = new BZip2OutputStream(ostm))
                    {
                        await istm.CopyToAsync(ozstm, token);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"failed bzip2 compression:{e}");
                return 1;
            }
            return 0;
        }
        /// <summary>
        /// do bzip2 decompression
        /// </summary>
        /// <param name="input">input file path(default: standard input)</param>
        /// <param name="output">output file path(default: standard output)</param>
        /// <param name="token"></param>
        /// <returns>return 0 on success, 1 if error</returns>
        [Command("bzip2 decompress|bzip2 d")]
        public async Task<int> Decompress(string? input, string? output = null, CancellationToken token = default)
        {
            try
            {
                using(var istm = Util.OpenInputStream(input))
                using(var ostm = Util.OpenOutputStream(output, true))
                {
                    using(var izstm = new BZip2InputStream(istm))
                    {
                        izstm.CopyTo(ostm);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"failed bzip2 decompression:{e}");
                return 1;
            }
            return 0;
        }
    }
}