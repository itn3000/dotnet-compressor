using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using ZstdSharp;

namespace dotnet_compressor.Zstd;

public class ZstdCommand
{
    /// <summary>
    /// zstandard compression command
    /// </summary>
    /// <param name="input">input file path(default: standard input)</param>
    /// <param name="output">output file path(default: standard output)</param>
    /// <param name="level">compression level(from 0 to 9, higher is more reducible)</param>
    /// <param name="bufferSize">buffer size</param>
    /// <param name="token"></param>
    /// <returns>0 if success, other if error</returns>
    [Command("zstd compress|zstd c")]
    public async Task<int> Compress(string? input = null, string? output = null, int level = 3, int bufferSize = 0, CancellationToken token = default)
    {
        using var istm = Util.OpenInputStream(input);
        using var ostm = Util.OpenOutputStream(output, true);
        using var ozstm = new CompressionStream(ostm, level, bufferSize);
        await istm.CopyToAsync(ozstm, token);
        return 0;
    }
    /// <summary>
    /// zstandard decompression command
    /// </summary>
    /// <param name="input">input file path(default: standard input)</param>
    /// <param name="output">output file path(default: standard output)</param>
    /// <param name="bufferSize">buffer size</param>
    /// <param name="token"></param>
    /// <returns>0 if success, other if error</returns>
    [Command("zstd decompress|zstd d")]
    public async Task<int> Decompress(string? input = null, string? output = null, int bufferSize = 0, CancellationToken token = default)
    {
        using var istm = Util.OpenInputStream(input);
        using var ostm = Util.OpenOutputStream(output, true);
        using var ozstm = new DecompressionStream(ostm, bufferSize);
        await istm.CopyToAsync(ozstm, token);
        return 0;
    }

}