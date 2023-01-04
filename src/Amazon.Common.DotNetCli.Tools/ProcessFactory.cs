using System.Diagnostics;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// This class represents an instance of a process being executed, set up to capture
    /// STDOUT and STDERR and expose as properties.  Once it's created, you can execute
    /// Run multiple times.
    /// </summary>
    public class ProcessInstance
    {
        /// <summary>
        /// Results of process execution
        /// </summary>
        public struct ProcessResults
        {
            /// <summary>
            /// True if process executed within timout
            /// </summary>
            public bool Executed { get; set; }
            /// <summary>
            /// Non-zero upon success
            /// </summary>
            public int? ExitCode { get; set; }
            /// <summary>
            /// Output captured from STDOUT
            /// </summary>
            public string Output { get; set; }
            /// <summary>
            /// Output captured from STDERR or other error information
            /// </summary>
            public string Error { get; set; }
        }
        
        private readonly ProcessStartInfo _info;
        
        /// <summary>
        /// Set up ProcessStartInfo, forcing values required to capture STDOUT and STDERR
        /// </summary>
        /// <param name="info"></param>
        public ProcessInstance(ProcessStartInfo info)
        {
            _info = info;
            _info.RedirectStandardOutput = true;
            _info.RedirectStandardError = true;
            _info.UseShellExecute = false;
        }

        /// <summary>
        /// Run the process
        /// </summary>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns>Process instance execution results</returns>
        public ProcessResults Run(int timeoutInMilliseconds = 60000)
        {

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            
            using var proc = new Process();
            proc.StartInfo = _info;
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += (_, e) =>
            {
                if (! string.IsNullOrEmpty(e.Data)) stdout.Append(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) stderr.Append(e.Data);
            };
            bool executed;
            int? exitCode;
            string output;
            string error;
            
            if (proc.Start())
            {
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                executed = proc.WaitForExit(timeoutInMilliseconds);
                if (executed)
                    proc.WaitForExit(); // this ensures STDOUT is completely captured
                else
                    stderr.Append($"{(stderr.Length > 0 ? "\n" : "")}Timeout waiting for process");
                exitCode = proc.ExitCode;
                output = stdout.ToString();
                error = stderr.ToString();
            }
            else
            {
                executed = false;
                exitCode = null;
                output = "";
                error = "Unable to launch process";
            }

            return new ProcessResults()
            {
                Executed = executed,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
    }
    
    /// <summary>
    /// This class is a factory that generates executed Processes
    /// </summary>
    public class ProcessFactory: IProcessFactory
    {
        public static readonly IProcessFactory Default = new ProcessFactory();
        
        /// <summary>
        /// Launch the process described by "info" and return the execution results.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public ProcessInstance.ProcessResults RunProcess(ProcessStartInfo info, int timeout = 60000)
        {
            return new ProcessInstance(info).Run(timeout);
        }
    }

    public interface IProcessFactory
    {
        ProcessInstance.ProcessResults RunProcess(ProcessStartInfo info, int timeout = 60000);
    }
}