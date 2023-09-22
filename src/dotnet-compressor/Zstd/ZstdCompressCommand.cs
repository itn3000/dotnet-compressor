using System;
using System.Net.Http.Headers;
using McMaster.Extensions.CommandLineUtils;
using ZstdSharp;

namespace dotnet_compressor.Zstd;

[Command("c", "compression", Description = "zstandard compression command")]
[HelpOption]
class ZstdCompressCommand
{
    [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input)", CommandOptionType.SingleValue)]
    public string InputFile { get; set; }
    [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output)", CommandOptionType.SingleValue)]
    public string OutputFile { get; set; }
    [Option("-l|--level=<COMPRESSION_LEVEL>", "compression level(from 0 to 9, higher is more reducible)", CommandOptionType.SingleValue)]
    public string LevelString { get; set; }
    [Option("-b|--buffersize=<BUFFER_SIZE>", "buffer size", CommandOptionType.SingleValue)]
    public string BufferSizeString { get; set; }
    public int OnExecute(IConsole console)
    {
        try
        {
            using var istm = Util.OpenInputStream(InputFile);
            using var ostm = Util.OpenOutputStream(OutputFile, true);
            using var ozstm = new CompressionStream(ostm, ParseLevel(), ParseBufferSize());
            istm.CopyTo(ozstm);
            return 0;
        }
        catch(Exception e)
        {
            console.Error.WriteLine($"failed to compress zstandard: {e}");
            return 1;
        }
    }
    int ParseBufferSize()
    {
        if(!string.IsNullOrEmpty(BufferSizeString) && uint.TryParse(BufferSizeString, out var bufferSize))
        {
            return (int)bufferSize;
        }
        else
        {
            return 0;
        }
    }
    int ParseLevel()
    {
        if(!string.IsNullOrEmpty(LevelString) && uint.TryParse(LevelString, out var level))
        {
            return (int)level;
        }
        else
        {
            return 0;
        }
    }
}