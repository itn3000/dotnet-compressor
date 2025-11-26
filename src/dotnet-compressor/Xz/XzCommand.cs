using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using SharpCompress.Compressors.Xz;

namespace dotnet_compressor
{
    class XzCommand
    {
        /// <summary>
        /// decompressing data as xz format
        /// </summary>
        /// <param name="input">input file path(default: standard input)</param>
        /// <param name="output">output file path(default: standard output)</param>
        /// <param name="token"></param>
        /// <returns>0 if success, other if error</returns>
        [Command("xz decompress|xz d")]
        public async Task<int> Decompress(string? input = null, string? output = null, CancellationToken token = default)
        {
            using (var istm = Util.OpenInputStream(input))
            using (var ostm = Util.OpenOutputStream(output, true))
            {
                using (var izstm = new XZStream(istm))
                {
                    await izstm.CopyToAsync(ostm, token);
                }
            }
            return 0;
        }
    }
}