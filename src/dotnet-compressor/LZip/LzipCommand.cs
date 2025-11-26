using System;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors;
using System.Threading.Tasks;
using System.Threading;
using ConsoleAppFramework;

namespace dotnet_compressor
{
    class LZipCommand
    {
        /// <summary>
        /// compressing data as lzip format
        /// </summary>
        /// <param name="input">-i, input file path(default: standard input)</param>
        /// <param name="output">-o, output file path(default: standard output)</param>
        /// <param name="token"></param>
        /// <returns>0 if success, other if error</returns>
        [Command("lzip compress|lzip c")]
        public async Task<int> Compress(string? input = null, string? output = null, CancellationToken token = default)
        {
            using (var istm = Util.OpenInputStream(input))
            using (var ostm = Util.OpenOutputStream(output, true))
            {
                using (var ozstm = new LZipStream(ostm, CompressionMode.Compress))
                {
                    await istm.CopyToAsync(ozstm, token);
                }
            }
            return 0;
        }
        /// <summary>
        /// decompressing data as lzip format
        /// </summary>
        /// <param name="input">-i, input file path(default: standard input)</param>
        /// <param name="output">-o, output file path(default: standard output)</param>
        /// <param name="token"></param>
        /// <returns>0 if success, other if error</returns>
        [Command("lzip decompress|lzip d")]
        public async Task<int> Decompress(string? input = null, string? output = null, CancellationToken token = default)
        {
            using (var istm = Util.OpenInputStream(input))
            using (var ostm = Util.OpenOutputStream(output, true))
            {
                using (var izstm = new LZipStream(istm, CompressionMode.Decompress))
                {
                    await izstm.CopyToAsync(ostm, token);
                }
            }
            return 0;
        }
    }
}