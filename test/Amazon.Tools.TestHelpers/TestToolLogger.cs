using System;
using System.Collections.Generic;
using System.Text;

using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Tools.TestHelpers
{
    public class TestToolLogger : IToolLogger
    {
        StringBuilder _buffer = new StringBuilder();
        public void WriteLine(string message)
        {
            this._buffer.AppendLine(message);
        }

        public void WriteLine(string message, params object[] args)
        {
            this._buffer.AppendLine(string.Format(message, args));
        }

        public void ClearBuffer()
        {
            this._buffer.Clear();
        }

        public string Buffer
        {
            get { return this._buffer.ToString(); }
        }
    }
}
