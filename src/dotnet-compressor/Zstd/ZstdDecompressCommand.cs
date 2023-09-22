using System;
using System.Net.Http.Headers;
using McMaster.Extensions.CommandLineUtils;
using ZstdSharp;

namespace dotnet_compressor.Zstd;

[Command("d", "decompression", Description = "zstandard decompression command")]
[HelpOption]
class ZstdDeompressCommand
{
    [Option("-i|--input=<INPUT_FILE>", "input file path(default: standard input)", CommandOptionType.SingleValue)]
    public string InputFile { get; set; }
    [Option("-o|--output=<OUTPUT_FILE_PATH>", "output file path(default: standard output)", CommandOptionType.SingleValue)]
    public string OutputFile { get; set; }
    [Option("-b|--buffersize=<BUFFER_SIZE>", "buffer size", CommandOptionType.SingleValue)]
    public string BufferSizeString { get; set; }
    public int OnExecute(IConsole console)
    {
        try
        {
            using var istm = Util.OpenInputStream(InputFile);
            using var ostm = Util.OpenOutputStream(OutputFile, true);
            using var ozstm = new DecompressionStream(ostm, ParseBufferSize());
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
}