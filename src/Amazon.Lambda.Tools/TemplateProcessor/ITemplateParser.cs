using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdParty.Json.LitJson;
using YamlDotNet.RepresentationModel;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    public interface ITemplateParser
    {
        IEnumerable<IUpdatableResource> UpdatableResources();

        string GetUpdatedTemplate();
    }

    public interface IUpdatableResource
    {
        string Name { get; }

        string ResourceType { get; }

        string GetLocalPath();

        void SetS3Location(string s3Bucket, string s3Key);
    }

    public interface IUpdatableResourceDataSource
    {
        string GetValue(params string[] keyPath);

        void SetValue(string value, params string[] keyPath);
    }
}
