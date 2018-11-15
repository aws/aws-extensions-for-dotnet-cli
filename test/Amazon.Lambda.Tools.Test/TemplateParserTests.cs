using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;

using Amazon.Lambda.Tools.TemplateProcessor;
using ThirdParty.Json.LitJson;
using YamlDotNet.RepresentationModel;

namespace Amazon.Lambda.Tools.Test
{
    public class TemplateParserTests
    {
        [Fact]
        public void DetermineJsonReader()
        {
            Assert.IsType<JsonTemplateParser>(TemplateProcessorManager.CreateTemplateParser("{\"AWSTemplateFormatVersion\" : \"2010-09-09\"}"));
            Assert.IsType<JsonTemplateParser>(TemplateProcessorManager.CreateTemplateParser("\t\t{\"AWSTemplateFormatVersion\" : \"2010-09-09\"}"));
            Assert.IsType<JsonTemplateParser>(TemplateProcessorManager.CreateTemplateParser("\n{\"AWSTemplateFormatVersion\" : \"2010-09-09\"}"));
        }

        [Fact]
        public void DetermineYamlReader()
        {
            Assert.IsType<YamlTemplateParser>(TemplateProcessorManager.CreateTemplateParser("AWSTemplateFormatVersion: \"2010-09-09\""));
            Assert.IsType<YamlTemplateParser>(TemplateProcessorManager.CreateTemplateParser("-"));
        }

        [Theory]
        [MemberData(nameof(GetAndSetValuesOnRootData))]
        public void GetAndSetValuesOnRoot(IUpdatableResourceDataSource source)
        {
            Assert.Equal("/home/code", source.GetValue("CodeUri"));

            source.SetValue("s3://my-bucket/my-key", "CodeUri");
            Assert.Equal("s3://my-bucket/my-key", source.GetValue("CodeUri"));
        }

        public static IEnumerable<object[]> GetAndSetValuesOnRootData()
        {
            var list = new List<object[]>();
            {
                var rootData = new JsonData();
                rootData["CodeUri"] = "/home/code";
                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();
                rootData.Children.Add("CodeUri", new YamlScalarNode("/home/code"));

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }

            return list;
        }


        [Theory]
        [MemberData(nameof(GetAndSetValuesOnChildOnObjectData))]
        public void GetAndSetValuesOnChildOnObject(IUpdatableResourceDataSource source)
        {
            Assert.Equal("/currentProject", source.GetValue("Code", "S3Key"));

            source.SetValue("my-key", "Code", "S3Key");
            Assert.Equal("my-key", source.GetValue("Code", "S3Key"));
        }

        public static IEnumerable<object[]> GetAndSetValuesOnChildOnObjectData()
        {
            var list = new List<object[]>();
            {
                var codeData = new JsonData();
                codeData["S3Bucket"] = "";
                codeData["S3Key"] = "/currentProject";

                var rootData = new JsonData();
                rootData["Code"] = codeData;

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }
            {
                var codeData = new YamlMappingNode();
                codeData.Children.Add("S3Bucket", new YamlScalarNode(""));
                codeData.Children.Add("S3Key", new YamlScalarNode("/currentProject"));

                var rootData = new YamlMappingNode();
                rootData.Children.Add("Code", codeData);

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }

            return list;
        }

        [Theory]
        [MemberData(nameof(GetNullValueAndSetValuesOnRootOnObjectData))]
        public void GetNullValueAndSetValuesOnRootOnObject(IUpdatableResourceDataSource source)
        {
            Assert.Null(source.GetValue("CodeUri"));

            source.SetValue("my-key", "CodeUri");
            Assert.Equal("my-key", source.GetValue("CodeUri"));
        }

        public static IEnumerable<object[]> GetNullValueAndSetValuesOnRootOnObjectData()
        {
            var list = new List<object[]>();
            {
                var rootData = new JsonData();

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }

            return list;
        }

        [Theory]
        [MemberData(nameof(GetNullValueAndSetValuesOnChildOnObjectData))]
        public void GetNullValueAndSetValuesOnChildOnObject(IUpdatableResourceDataSource source)
        {
            Assert.Null(source.GetValue("Code", "S3Key"));

            source.SetValue("my-key", "Code", "S3Key");
            Assert.Equal("my-key", source.GetValue("Code", "S3Key"));
        }

        public static IEnumerable<object[]> GetNullValueAndSetValuesOnChildOnObjectData()
        {
            var list = new List<object[]>();
            {
                var rootData = new JsonData();

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(null, rootData);
                list.Add(new object[] { source });
            }

            return list;
        }


        [Fact]
        public void ServerlessFunction_GetCurrentDirectoryForWithNullCodeUri()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_SERVERLESS_FUNCTION, dataSource);



            Assert.Equal(".", resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "my.zip");
            Assert.Equal("s3://my-bucket/my.zip", dataSource.GetValue("CodeUri"));
        }

        [Theory]
        [InlineData(".")]
        [InlineData("/home")]
        public void ServerlessFunction_GetLocalPath(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                        {"CodeUri", localPath }
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_SERVERLESS_FUNCTION, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "my.zip");
            Assert.Equal("s3://my-bucket/my.zip", dataSource.GetValue("CodeUri"));
        }

        [Fact]
        public void ServerlessFunction_NotUpdatableBecausePointAtAlreadyS3()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                        {"CodeUri", "s3://my-bucket/my.zip" }
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_SERVERLESS_FUNCTION, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }

        [Fact]
        public void LambdaFunction_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_LAMBDA_FUNCTION, dataSource);

            Assert.Equal(".", resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "my.zip");
            Assert.Equal("my-bucket", dataSource.GetValue("Code/S3Bucket"));
            Assert.Equal("my.zip", dataSource.GetValue("Code/S3Key"));
        }

        [Theory]
        [InlineData(".")]
        [InlineData("/home")]
        public void LambdaFunction_GetLocalPath(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                        {"Code/S3Key", localPath }
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_LAMBDA_FUNCTION, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "my.zip");
            Assert.Equal("my-bucket", dataSource.GetValue("Code/S3Bucket"));
            Assert.Equal("my.zip", dataSource.GetValue("Code/S3Key"));
        }

        [Theory]
        [InlineData(".")]
        [InlineData("/home")]
        public void LambdaFunction_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                        {"Code/S3Bucket", "" },
                                        {"Code/S3Key", localPath }
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_LAMBDA_FUNCTION, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "my.zip");
            Assert.Equal("my-bucket", dataSource.GetValue("Code/S3Bucket"));
            Assert.Equal("my.zip", dataSource.GetValue("Code/S3Key"));
        }

        [Fact]
        public void LambdaFunction_NotUpdatableBecausePointAtAlreadyS3()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                        {"Code/S3Bucket", "my-bucket" },
                                        {"Code/S3Key", "my.zip" }
                                    });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_LAMBDA_FUNCTION, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Fact]
        public void ApiGatewayApi_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_SERVERLESS_API, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Theory]
        [InlineData("/home/swagger.yml")]
        public void ApiGatewayApi_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"BodyS3Location/Key", localPath }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APIGATEWAY_RESTAPI, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", localPath);
            Assert.Equal("my-bucket", dataSource.GetValue("BodyS3Location/Bucket"));
            Assert.Equal(localPath, dataSource.GetValue("BodyS3Location/Key"));
        }
        
        [Fact]
        public void AppSyncGraphQl_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APPSYNC_GRAPHQLSCHEMA, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Theory]
        [InlineData("/home/swagger.yml")]
        public void AppSyncGraphQl_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"DefinitionS3Location", localPath }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APPSYNC_GRAPHQLSCHEMA, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "swagger.yml");
            Assert.Equal("s3://my-bucket/swagger.yml", dataSource.GetValue("DefinitionS3Location"));
        }
        

        [Fact]
        public void AppSyncResolver_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APPSYNC_RESOLVER, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
            Assert.Null(resource.Fields[1].GetLocalPath());
        }
        
        [Fact]
        public void AppSyncResolver_GetLocalPathAndEmptyS3Bucket()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"ResponseMappingTemplateS3Location", "response.xml" },
                    {"RequestMappingTemplateS3Location", "request.xml" }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APPSYNC_RESOLVER, dataSource);

            Assert.Equal("response.xml", resource.Fields[0].GetLocalPath());
            Assert.Equal("request.xml", resource.Fields[1].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "response.xml-updated");
            Assert.Equal("s3://my-bucket/response.xml-updated", dataSource.GetValue("ResponseMappingTemplateS3Location"));
            resource.Fields[1].SetS3Location("my-bucket", "request.xml-updated");
            Assert.Equal("s3://my-bucket/request.xml-updated", dataSource.GetValue("RequestMappingTemplateS3Location"));
        }
        
        [Fact]
        public void ServerlessApi_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_APIGATEWAY_RESTAPI, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Theory]
        [InlineData("/home/swagger.yml")]
        public void ServerlessApi_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"DefinitionUri", localPath }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_SERVERLESS_API, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "swagger.yml");
            Assert.Equal("s3://my-bucket/swagger.yml", dataSource.GetValue("DefinitionUri"));
        }        
        
        
        [Fact]
        public void ElasticBeanstalkApplicationVersion_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_ELASTICBEANSTALK_APPLICATIONVERSION, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Theory]
        [InlineData("/home/app.zip")]
        public void ElasticBeanstalkApplicationVersion_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"SourceBundle/S3Key", localPath }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_ELASTICBEANSTALK_APPLICATIONVERSION, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", localPath);
            Assert.Equal("my-bucket", dataSource.GetValue("SourceBundle/S3Bucket"));
            Assert.Equal(localPath, dataSource.GetValue("SourceBundle/S3Key"));
        }  
        
        
        [Fact]
        public void CloudFormationStack_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_CLOUDFORMATION_STACK, dataSource);

            Assert.Null(resource.Fields[0].GetLocalPath());
        }
        
        [Theory]
        [InlineData("/home/infra.template")]
        public void CloudFormationStack_GetLocalPathAndEmptyS3Bucket(string localPath)
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                new Dictionary<string, string>
                {
                    {"TemplateUrl", localPath }
                });
            var resource = new UpdatableResource("TestResource", UpdatableResourceDefinition.DEF_CLOUDFORMATION_STACK, dataSource);

            Assert.Equal(localPath, resource.Fields[0].GetLocalPath());

            resource.Fields[0].SetS3Location("my-bucket", "swagger.yml");
            Assert.Equal("s3://my-bucket/swagger.yml", dataSource.GetValue("TemplateUrl"));
        }     
        
        public class FakeUpdatableResourceDataSource : IUpdatableResourceDataSource
        {
            IDictionary<string, string> Root { get; }
            IDictionary<string, string> Properties { get; }

            public FakeUpdatableResourceDataSource(IDictionary<string, string> root, IDictionary<string, string> properties)
            {
                this.Root = root;
                this.Properties = properties;
            }

            public FakeUpdatableResourceDataSource(IDictionary<string, string> properties)
                : this(properties, properties)
            {
            }

            public string GetValueFromRoot(params string[] keyPath)
            {
                return GetValue(this.Root, keyPath);
            }

            public string GetValue(params string[] keyPath)
            {
                return GetValue(this.Properties, keyPath);
            }

            private string GetValue(IDictionary<string, string> values, params string[] keyPath)
            {
                var key = string.Join("/", keyPath);
                if (values.TryGetValue(key, out var value))
                    return value;
                return null;
            }

            public void SetValue(string value, params string[] keyPath)
            {
                var key = string.Join("/", keyPath);
                Properties[key] = value;
            }
        }
    }
}
