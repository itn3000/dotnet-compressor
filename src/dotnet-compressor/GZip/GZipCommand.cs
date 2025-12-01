using System;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;
using System.Threading.Tasks;
using System.Threading;
using ConsoleAppFramework;

namespace dotnet_compressor
{
    class GZipCommand
    {
        /// <summary>
        /// compressing data as gzip format
        /// </summary>
        /// <param name="input">-i, input file path(default: standard input)</param>
        /// <param name="output">-o, output file path(default: standard output)</param>
        /// <param name="level">-l, compression level(from 0 to 9, higher is more reducible)</param>
        /// <param name="token"></param>
        /// <returns>0 if success, other if error</returns>
        [Command("gzip compress|gzip c")]
        public async Task<int> Compress(string? input = null, string? output = null, int level = 5, CancellationToken token = default)
        {
            using (var istm = Util.OpenInputStream(input))
            using (var ostm = Util.OpenOutputStream(output, true))
            {
                using (var ozstm = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(ostm))
                {
                    if (level >= 0)
                    {
                        ozstm.SetLevel(level);
                    }
                    await istm.CopyToAsync(ozstm, token);
                }
            }
            return 0;
        }
        /// <summary>
        /// decompressing data as gzip format
        /// </summary>
        /// <param name="input">-i, input file path(default: standard input)</param>
        /// <param name="output">-o, output file path(default: standard output)</param>
        /// <returns>0 if success, other if error</returns>
        [Command("gzip decompress|gzip d")]
        public async Task<int> Decompress(string? input = null, string? output = null, CancellationToken token = default)
        {
            using (var istm = Util.OpenInputStream(input))
            using (var ostm = Util.OpenOutputStream(output, true))
            {
                using (var izstm = new GZipInputStream(istm))
                {
                    await izstm.CopyToAsync(ostm, token);
                }
            }
            return 0;
        }

    }
}