using System;

namespace Amazon.Common.DotNetCli.Tools.Test.Mocks;

public class MockToolLogger : IToolLogger
{
    public void WriteLine(string _)
    {
        // NOP
    }

    public void WriteLine(string _1, params object[] _2)
    {
        // NOP
    }
}