using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    public class UpdatableResource : IUpdatableResource
    {
        public string Name { get; }
        public string ResourceType { get; }
        IUpdatableResourceDataSource DataSource { get; }

        public UpdatableResource(string name, string resourceType, IUpdatableResourceDataSource dataSource)
        {
            this.Name = name;
            this.ResourceType = resourceType;
            this.DataSource = dataSource;
        }

        public string LambdaRuntime => this.DataSource.GetValue("Runtime");

        public string GetLocalPath()
        {
            if (string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, StringComparison.Ordinal))
            {
                var localPath = this.DataSource.GetValue("CodeUri") ?? ".";
                if (localPath.StartsWith("s3://"))
                    return null;

                return localPath;
            }
            else if (string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, StringComparison.Ordinal))
            {
                var bucket = this.DataSource.GetValue("Code", "S3Bucket");
                if (!string.IsNullOrEmpty(bucket))
                    return null;

                return this.DataSource.GetValue("Code", "S3Key") ?? ".";
            }

            return null;
        }

        public void SetS3Location(string s3Bucket, string s3Key)
        {
            if (string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, StringComparison.Ordinal))
            {
                var s3Url = $"s3://{s3Bucket}/{s3Key}";
                this.DataSource.SetValue(s3Url, "CodeUri");
            }
            else if (string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, StringComparison.Ordinal))
            {
                this.DataSource.SetValue(s3Bucket, "Code", "S3Bucket");
                this.DataSource.SetValue(s3Key, "Code", "S3Key");
            }
        }

        public bool IsUpdatable
        {
            get
            {
                if ((string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_SERVERLESS_FUNCTION, StringComparison.Ordinal) ||
                    string.Equals(this.ResourceType, TemplateProcessorManager.CF_TYPE_LAMBDA_FUNCTION, StringComparison.Ordinal)) && this.GetLocalPath() != null)
                {
                    return true;
                }


                return false;
            }
        }
    }
}
