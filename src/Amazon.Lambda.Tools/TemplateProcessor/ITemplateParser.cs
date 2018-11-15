using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdParty.Json.LitJson;
using YamlDotNet.RepresentationModel;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// Interface to iterate over the CloudFormation resources that can be updated.
    /// </summary>
    public interface ITemplateParser
    {
        /// <summary>
        /// Iterator for the updatable resources.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IUpdatableResource> UpdatableResources();

        /// <summary>
        /// Get the new template after the resources have been updated.
        /// </summary>
        /// <returns></returns>
        string GetUpdatedTemplate();
    }

    /// <summary>
    /// Interface for a CloudFormation resource that can be updated.
    /// </summary>
    public interface IUpdatableResource
    {
        /// <summary>
        /// The CloudFormation name of the resource.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The CloudFormation resource type.
        /// </summary>
        string ResourceType { get; }

        /// <summary>
        /// If the resource is a AWS::Lambda::Function or AWS::Serverless::Function get the Lambda runtime for it.
        /// </summary>
        string LambdaRuntime { get; }
        
        /// <summary>
        /// The list of fields that can be updated.
        /// </summary>
        IList<IUpdateResourceField> Fields { get; }
    }

    /// <summary>
    /// Interface for the field in the IUpdatableResource that can be updated.
    /// </summary>
    public interface IUpdateResourceField
    {
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// True if the field should contain code like a Lambda package bundle.
        /// </summary>
        bool IsCode { get; }
        
        
        /// <summary>
        /// Reference back to the containing updatable resource.
        /// </summary>
        IUpdatableResource Resource { get; }
        
        /// <summary>
        /// Gets the local path for the resource. If the value is referencing an object in S3 then this returns null.
        /// </summary>
        /// <returns></returns>
        string GetLocalPath();
        
        /// <summary>
        /// Updates the location the field is pointing to where the local path was uploaded to S3.
        /// </summary>
        /// <param name="s3Bucket"></param>
        /// <param name="s3Key"></param>
        void SetS3Location(string s3Bucket, string s3Key);
    }

    /// <summary>
    /// Interface for the underlying datasource (JSON or YAML), 
    /// </summary>
    public interface IUpdatableResourceDataSource
    {
        /// <summary>
        /// Gets value starting from the root document.
        /// </summary>
        /// <param name="keyPath"></param>
        /// <returns></returns>
        string GetValueFromRoot(params string[] keyPath);


        /// <summary>
        /// Gets value in datasource.
        /// </summary>
        /// <param name="keyPath"></param>
        /// <returns></returns>
        string GetValue(params string[] keyPath);

        /// <summary>
        /// Set value in datasource.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="keyPath"></param>
        void SetValue(string value, params string[] keyPath);
    }
}
