#:package Amazon.Lambda.Core@2.8.0
#:package Amazon.Lambda.RuntimeSupport@1.14.1
#:package Amazon.Lambda.Serialization.SystemTextJson@2.4.4
#:property TargetFramework=net10.0

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;

// The function handler that will be called for each Lambda event
var handler = (string input, ILambdaContext context) =>
{
    return input.ToUpper();
};

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaSerializerContext>())
        .Build()
        .RunAsync();


[JsonSerializable(typeof(string))]
public partial class LambdaSerializerContext : JsonSerializerContext
{
}