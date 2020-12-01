using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace UseDefaultDocker
{
    public class Function
    {
        
        public string FunctionHandler(string input, ILambdaContext context)
        {
            context.Logger.LogLine("UseDefaultDocker");
            return input?.ToUpper();
        }
    }
    
}
