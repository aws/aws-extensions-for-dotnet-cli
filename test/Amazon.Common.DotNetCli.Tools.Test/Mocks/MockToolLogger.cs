using System;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools.Test.Mocks;

public class MockToolLogger : IToolLogger
{
    private readonly StringBuilder _sbLog = new();

    public void WriteLine(string text)
    {
        _sbLog.Append(text);
        _sbLog.Append('\n');
    }

    public void WriteLine(string text, params object[] data)
    {
        _sbLog.AppendFormat(text, data);
        _sbLog.Append('\n');
    }

    public string Log => _sbLog.ToString();
}