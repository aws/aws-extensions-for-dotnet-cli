using System.Text;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Lambda.Tools.Test
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
            this.WriteLine(string.Format(message, args));
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
