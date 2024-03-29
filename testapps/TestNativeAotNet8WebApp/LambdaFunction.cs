using Amazon.Lambda.AspNetCoreServer;

namespace TestNativeAotNet8WebApp;

public class LambdaFunction : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseLambdaServer()
            .UseContentRoot(Directory.GetCurrentDirectory());
    }
}