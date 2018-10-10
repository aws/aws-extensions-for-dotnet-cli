using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;

using Xunit;

namespace Amazon.Lambda.Tools.Test
{
    public class TemplateCodeUpdateYamlTest
    {
        static readonly string SERVERLESS_FUNCTION =
@"
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31
Resources:
  TheServerlessFunction:
    Type: AWS::Serverless::Function
    Properties:
      CodeUri: ./
      Handler: lambda.handler
      MemorySize: 1024
      Role: LambdaExecutionRole.Arn
      Runtime: dotnetcore1.0
      Timeout: 30
      Events:
        ProxyApiGreedy:
          Type: Api
          Properties:
            RestApiId: ApiGatewayApi
            Path: /{proxy+}
            Method: ANY
";

        static readonly string LAMBDA_FUNCTION =
@"
AWSTemplateFormatVersion: '2010-09-09'
Resources:
  TheLambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      Handler: lambda.handler
      MemorySize: 1024
      Role: LambdaExecutionRole.Arn
      Runtime: dotnetcore1.0
      Timeout: 30
      Code:
        S3Bucket: PlaceHolderObject
        S3Key: PlaceHolderKey
";

        
        static readonly string API_WITH_SWAGGER =
@"AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31
Description: An AWS Serverless Application.
Resources: 
  Api:
    Type: AWS::Serverless::Api
    Properties:
      StageName: prod
      DefinitionBody: 
        swagger: '2.0'
        info:
          version: '2018-04-18T18:37:10Z'
          title: 'manual-deploy'
        host: 'prepend.execute-api.region.amazonaws.com'
        basePath: '/prod'
        schemes:
        - 'https'
        paths:
          /ride:
            post:
              produces:
              - 'application/json'
              responses:
                '200':
                  description: '200 response'
                  schema:
                    $ref: '#/definitions/Empty'
                  headers:
                    Content-Type:
                      type: 'string'    
        definitions:
          Empty:
            type: 'object'
            title: 'Empty Schema'";
            
        const string S3_BUCKET = "myBucket";
        const string S3_OBJECT = "myObject";
        static readonly string S3_URL = $"s3://{S3_BUCKET}/{S3_OBJECT}";

        [Fact]
        public void ReplaceServerlessApiCodeLocation()
        {
            var updateTemplateBody = LambdaUtilities.UpdateCodeLocationInTemplate(SERVERLESS_FUNCTION, S3_BUCKET, S3_OBJECT);

            var root = new Deserializer().Deserialize(new StringReader(updateTemplateBody)) as IDictionary<object, object>;

            var resources = root["Resources"] as IDictionary<object, object>;
            var resource = resources["TheServerlessFunction"] as IDictionary<object, object>;
            var properties = resource["Properties"] as IDictionary<object, object>;
            Assert.Equal(S3_URL, properties["CodeUri"]);
        }

        [Fact]
        public void ReplaceLambdaFunctionCodeLocation()
        {
            var updateTemplateBody = LambdaUtilities.UpdateCodeLocationInTemplate(LAMBDA_FUNCTION, S3_BUCKET, S3_OBJECT);

            var root = new Deserializer().Deserialize(new StringReader(updateTemplateBody)) as IDictionary<object, object>;

            var resources = root["Resources"] as IDictionary<object, object>;
            var resource = resources["TheLambdaFunction"] as IDictionary<object, object>;
            var properties = resource["Properties"] as IDictionary<object, object>;
            var code = properties["Code"] as IDictionary<object, object>;

            Assert.Equal(S3_BUCKET, code["S3Bucket"]);
            Assert.Equal(S3_OBJECT, code["S3Key"]);
        }

        [Fact]
        public void ReplaceCodeLocationPreservesSwagger()
        {
            var updateTemplateBody = LambdaUtilities.UpdateCodeLocationInTemplate(API_WITH_SWAGGER, S3_BUCKET, S3_OBJECT);
            
            Assert.Contains("'200':", updateTemplateBody);
        }

        [Fact]
        public void DeployServerlessWithSwaggerWithTemplateSubstitution()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../test/Amazon.Lambda.Tools.Test/TestFiles/testtemplate.yaml");

            var template = File.ReadAllText(fullPath);

            //Does not throw an error when parsing template
            var updateTemplateBody = LambdaUtilities.UpdateCodeLocationInYamlTemplate(template, S3_BUCKET, S3_OBJECT);
            //validate that functions survive the template update
            Assert.Contains("DevStack: !Equals [!Ref 'AWS::StackName', dev]", updateTemplateBody);
        }
    }
}
