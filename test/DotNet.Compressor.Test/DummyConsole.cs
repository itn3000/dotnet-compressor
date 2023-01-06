using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Text;

namespace DotNet.Compressor.Test
{
    class DummyConsole : IConsole
    {
        StringBuilder _Buffer = new StringBuilder();
        StringBuilder _ErrorBuffer = new StringBuilder();
        string _Input = string.Empty;
        public DummyConsole()
        {

        }
        public void Cancel()
        {
            if(CancelKeyPress != null)
            {
                CancelKeyPress(this, null);
            }
        }
        public DummyConsole(string input)
        {
            _Input = input;
        }
        public TextReader GetOutput()
        {
            return new StringReader(_Buffer.ToString());
        }
        public TextReader GetError()
        {
            return new StringReader(_ErrorBuffer.ToString());
        }
        public TextWriter Out => new StringWriter(_Buffer);

        public TextWriter Error => new StringWriter(_ErrorBuffer);

        public TextReader In => new StringReader(_Input);

        public bool IsInputRedirected => false;

        public bool IsOutputRedirected => false;

        public bool IsErrorRedirected => false;

        public ConsoleColor ForegroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ConsoleColor BackgroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event ConsoleCancelEventHandler CancelKeyPress;

        public void ResetColor()
        {
        }
    }
}