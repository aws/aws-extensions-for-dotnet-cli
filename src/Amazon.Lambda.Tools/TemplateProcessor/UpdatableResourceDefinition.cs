using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    public class UpdatableResourceDefinition
    {
        public static readonly UpdatableResourceDefinition DEF_LAMBDA_FUNCTION = new UpdatableResourceDefinition(
            "AWS::Lambda::Function",
            FieldDefinition.CreateFieldDefinitionS3LocationStyle(true, "Code", "S3Bucket", "S3Key")
        );

        public static readonly UpdatableResourceDefinition DEF_SERVERLESS_FUNCTION = new UpdatableResourceDefinition(
            "AWS::Serverless::Function",
            FieldDefinition.CreateFieldDefinitionUrlStyle(true, "CodeUri")
        );

        public static readonly UpdatableResourceDefinition DEF_APIGATEWAY_RESTAPI = new UpdatableResourceDefinition(
            "AWS::ApiGateway::RestApi",
            FieldDefinition.CreateFieldDefinitionS3LocationStyle(false, "BodyS3Location", "Bucket", "Key")
        );
        
        public static readonly UpdatableResourceDefinition DEF_APPSYNC_GRAPHQLSCHEMA = new UpdatableResourceDefinition(
            "AWS::AppSync::GraphQLSchema",
            FieldDefinition.CreateFieldDefinitionUrlStyle(false, "DefinitionS3Location")
        );        
        
        public static readonly UpdatableResourceDefinition DEF_APPSYNC_RESOLVER = new UpdatableResourceDefinition(
            "AWS::AppSync::Resolver",
            FieldDefinition.CreateFieldDefinitionUrlStyle(false, "ResponseMappingTemplateS3Location"),
            FieldDefinition.CreateFieldDefinitionUrlStyle(false, "RequestMappingTemplateS3Location")
        );  
        
        public static readonly UpdatableResourceDefinition DEF_SERVERLESS_API = new UpdatableResourceDefinition(
            "AWS::Serverless::Api",
            FieldDefinition.CreateFieldDefinitionUrlStyle(false, "DefinitionUri")
        );   
        
        public static readonly UpdatableResourceDefinition DEF_ELASTICBEANSTALK_APPLICATIONVERSION = new UpdatableResourceDefinition(
            "AWS::ElasticBeanstalk::ApplicationVersion",
            FieldDefinition.CreateFieldDefinitionS3LocationStyle(false, "SourceBundle", "S3Bucket", "S3Key")
        );  
        
        public static readonly UpdatableResourceDefinition DEF_CLOUDFORMATION_STACK = new UpdatableResourceDefinition(
            "AWS::CloudFormation::Stack",
            FieldDefinition.CreateFieldDefinitionUrlStyle(false, "TemplateUrl")
        );          

        
        public static IDictionary<string, UpdatableResourceDefinition> ValidUpdatableResourceDefinitions = new
            Dictionary<string, UpdatableResourceDefinition>
            {
                {DEF_APIGATEWAY_RESTAPI.Name, DEF_APIGATEWAY_RESTAPI},
                {DEF_LAMBDA_FUNCTION.Name, DEF_LAMBDA_FUNCTION},
                {DEF_SERVERLESS_FUNCTION.Name, DEF_SERVERLESS_FUNCTION},
                {DEF_APPSYNC_GRAPHQLSCHEMA.Name, DEF_APPSYNC_GRAPHQLSCHEMA},
                {DEF_APPSYNC_RESOLVER.Name, DEF_APPSYNC_RESOLVER},
                {DEF_SERVERLESS_API.Name, DEF_SERVERLESS_API},
                {DEF_ELASTICBEANSTALK_APPLICATIONVERSION.Name, DEF_ELASTICBEANSTALK_APPLICATIONVERSION},
                {DEF_CLOUDFORMATION_STACK.Name, DEF_CLOUDFORMATION_STACK},
            };


        public UpdatableResourceDefinition(string name, params FieldDefinition[] fields)
        {
            this.Name = name;
            this.Fields = fields;
        }
        
        
        public string Name { get; }
        
        public FieldDefinition[] Fields { get; }

        public class FieldDefinition
        {
            public bool IsCode { get; set; }
            public Func<IUpdatableResourceDataSource, string> GetLocalPath { get; set; }
            public Action<IUpdatableResourceDataSource, string, string> SetS3Location { get; set; }


            public static FieldDefinition CreateFieldDefinitionUrlStyle(bool isCode, string propertyName)
            {
                return new FieldDefinition
                {
                    IsCode = isCode,
                    GetLocalPath = (s) =>
                    {
                        var localPath = s.GetValue(propertyName);
                        if (string.IsNullOrEmpty(localPath) && isCode)
                        {
                            localPath = ".";
                        }
                        else if (localPath.StartsWith("s3://"))
                        {
                            return null;
                        }

                        return localPath;
                    },
                    SetS3Location = (s, b, k) =>
                    {
                        var s3Url = $"s3://{b}/{k}";
                        s.SetValue(s3Url, propertyName);
                    }
                };
            }
            
            public static FieldDefinition CreateFieldDefinitionS3LocationStyle(bool isCode, string containerName, string s3BucketProperty, string s3KeyProperty)
            {
                return new FieldDefinition
                {
                    IsCode = isCode,
                    GetLocalPath = (s) =>
                    {
                        var bucket = s.GetValue(containerName, s3BucketProperty);
                        if (!string.IsNullOrEmpty(bucket))
                            return null;

                        var value = s.GetValue(containerName, s3KeyProperty);
                        if (string.IsNullOrEmpty(value) && isCode)
                        {
                            value = ".";
                        }

                        return value;
                    },
                    SetS3Location = (s, b, k) =>
                    {
                        s.SetValue(b, containerName, s3BucketProperty);
                        s.SetValue(k, containerName, s3KeyProperty);
                    }
                };
            }            
        }
    }
}