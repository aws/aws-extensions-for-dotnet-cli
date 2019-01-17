using System.Text;
using Amazon.Common.DotNetCli.Tools;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    public class TestToolLogger : IToolLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        StringBuilder _buffer = new StringBuilder();

        public TestToolLogger()
        {

        }

        public TestToolLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void WriteLine(string message)
        {
            this._buffer.AppendLine(message);
            _testOutputHelper?.WriteLine(message);
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
