using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TheFunction
{
    public class Function
    {
        
        public string FunctionHandler(ILambdaContext context)
        {
            context.Logger.LogLine("Hello from support library test");
            return Supportlibrary.Message.Greeting;
        }
    }
    
}
