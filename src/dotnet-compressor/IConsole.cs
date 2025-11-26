using System;
using System.IO;

namespace dotnet_compressor;

public interface IConsole
{
    TextWriter Error { get; }
    TextWriter Out { get; }
    void WriteLine(string str);
}

class DefaultConsole : IConsole
{
    public TextWriter Error => Console.Error;

    public TextWriter Out => Console.Out;
    public static readonly IConsole Instance = new DefaultConsole();
    public void WriteLine(string str) => Console.WriteLine(str);
}