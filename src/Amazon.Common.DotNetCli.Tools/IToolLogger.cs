using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// The logger is used to write all status messages. When executing CLI commands they are written to the console.
    /// When the Visual Studio Toolkit calls the commands it passes its logger to redirect the output
    /// to the VS windows.
    /// </summary>
    public interface IToolLogger
    {
        void WriteLine(string message);

        void WriteLine(string message, params object[] args);
    }

    /// <summary>
    /// Default console implementation
    /// </summary>
    public class ConsoleToolLogger : IToolLogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
#if DEBUG
            Debugger.Log(0, "console", message + "\n");
#endif
        }

        public void WriteLine(string message, params object[] args)
        {
            var str = string.Format(message, args);
            Console.WriteLine(str);
#if DEBUG
            Debugger.Log(0, "console", str + "\n");
#endif
        }
    }
}
