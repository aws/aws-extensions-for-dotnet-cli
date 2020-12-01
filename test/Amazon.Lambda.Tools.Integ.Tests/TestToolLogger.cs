using System.Text;
using Xunit.Abstractions;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Lambda.Tools.Integ.Tests
{
    public class TestToolLogger : IToolLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        StringBuilder _buffer = new StringBuilder();

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