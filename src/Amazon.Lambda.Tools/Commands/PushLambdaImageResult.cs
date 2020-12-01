using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Lambda.Tools.Commands
{
    public class PushLambdaImageResult
    {
        public bool Success { get; set; }
        public ToolsException LastToolsException { get; set; }
        public string ImageUri { get; set; }
    }
}
