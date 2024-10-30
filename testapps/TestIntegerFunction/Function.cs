using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestIntegerFunction
{
    
    public class Function
    {
        [Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public string FunctionHandler(int input)
        {
            return "Hello " + input;
        }
    }
}
