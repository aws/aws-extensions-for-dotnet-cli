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
                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();
                rootData.Children.Add("CodeUri", new YamlScalarNode("/home/code"));

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(rootData);
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

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(rootData);
                list.Add(new object[] { source });
            }
            {
                var codeData = new YamlMappingNode();
                codeData.Children.Add("S3Bucket", new YamlScalarNode(""));
                codeData.Children.Add("S3Key", new YamlScalarNode("/currentProject"));

                var rootData = new YamlMappingNode();
                rootData.Children.Add("Code", codeData);

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(rootData);
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

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(rootData);
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

                var source = new JsonTemplateParser.JsonUpdatableResourceDataSource(rootData);
                list.Add(new object[] { source });
            }
            {
                var rootData = new YamlMappingNode();

                var source = new YamlTemplateParser.YamlUpdatableResourceDataSource(rootData);
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, dataSource);



            Assert.True(resource.IsUpdatable);
            Assert.Equal(".", resource.GetLocalPath());

            resource.SetS3Location("my-bucket", "my.zip");
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, dataSource);

            Assert.True(resource.IsUpdatable);
            Assert.Equal(localPath, resource.GetLocalPath());

            resource.SetS3Location("my-bucket", "my.zip");
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, dataSource);

            Assert.False(resource.IsUpdatable);
        }

        [Fact]
        public void LambdaFunction_GetCurrentDirectoryForWithNullCode()
        {
            var dataSource = new FakeUpdatableResourceDataSource(
                                    new Dictionary<string, string>
                                    {
                                    });
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, dataSource);

            Assert.True(resource.IsUpdatable);
            Assert.Equal(".", resource.GetLocalPath());

            resource.SetS3Location("my-bucket", "my.zip");
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, dataSource);

            Assert.True(resource.IsUpdatable);
            Assert.Equal(localPath, resource.GetLocalPath());

            resource.SetS3Location("my-bucket", "my.zip");
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, dataSource);

            Assert.True(resource.IsUpdatable);
            Assert.Equal(localPath, resource.GetLocalPath());

            resource.SetS3Location("my-bucket", "my.zip");
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
            var resource = new UpdatableResource("TestResource", TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, dataSource);

            Assert.False(resource.IsUpdatable);
        }


        public class FakeUpdatableResourceDataSource : IUpdatableResourceDataSource
        {
            IDictionary<string, string> Values { get; }

            public FakeUpdatableResourceDataSource()
            {
                Values = new Dictionary<string, string>();
            }

            public FakeUpdatableResourceDataSource(IDictionary<string, string> values)
            {
                this.Values = values;
            }

            public string GetValue(params string[] keyPath)
            {
                var key = string.Join("/", keyPath);
                if (Values.TryGetValue(key, out var value))
                    return value;
                return null;
            }

            public void SetValue(string value, params string[] keyPath)
            {
                var key = string.Join("/", keyPath);
                Values[key] = value;
            }
        }
    }
}
