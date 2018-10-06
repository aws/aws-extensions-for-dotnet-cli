using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// Class that identifies the fields and the delegates to access those fields in a given IUpdatableResourceDataSource.
    /// </summary>
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


        /// <summary>
        /// All of the known CloudFormation resources that have fields pointing to S3 locations that can be updated from a local path.
        /// </summary>
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

        
        /// <summary>
        /// FieldDefinition identity what fields in a CloudFormation resource can be updated and the delegates to retrieve
        /// and set the information in the datasource.
        /// </summary>
        public class FieldDefinition
        {
            public string Name { get; set; }
            public bool IsCode { get; set; }
            
            /// <summary>
            /// The Func that knows how to get the local path for the field from the datasource.
            /// </summary>
            public Func<IUpdatableResourceDataSource, string> GetLocalPath { get; set; }
            
            /// <summary>
            /// The Action that knows how to set the S3 location for the field into this datasource.
            /// </summary>
            public Action<IUpdatableResourceDataSource, string, string> SetS3Location { get; set; }


            /// <summary>
            /// Creates a field definition that is storing the S3 location as a S3 path like s3://mybucket/myfile.zip
            /// </summary>
            /// <param name="isCode"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            public static FieldDefinition CreateFieldDefinitionUrlStyle(bool isCode, string propertyName)
            {
                return new FieldDefinition
                {
                    Name = propertyName,
                    IsCode = isCode,
                    GetLocalPath = (s) =>
                    {
                        var localPath = s.GetValue(propertyName);
                        if (string.IsNullOrEmpty(localPath))
                        {
                            if (isCode)
                            {
                                localPath = ".";                                
                            }
                        }
                        else if (localPath.StartsWith("s3://"))
                        {
                            localPath = null;
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
            
            /// <summary>
            /// Creates a field definition that is storing the S3 location using separate fields for bucket and key.
            /// </summary>
            /// <param name="isCode"></param>
            /// <param name="containerName"></param>
            /// <param name="s3BucketProperty"></param>
            /// <param name="s3KeyProperty"></param>
            /// <returns></returns>
            public static FieldDefinition CreateFieldDefinitionS3LocationStyle(bool isCode, string containerName, string s3BucketProperty, string s3KeyProperty)
            {
                return new FieldDefinition
                {
                    Name = containerName,
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