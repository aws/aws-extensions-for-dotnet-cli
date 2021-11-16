using System.IO;

using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;

namespace TestServerlessWebApp
{
    public class LambdaFunction : APIGatewayProxyFunction
    {
        public const string BinaryContentType = "application/octet-stream";

        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .UseLambdaServer()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
        }
    }
}
