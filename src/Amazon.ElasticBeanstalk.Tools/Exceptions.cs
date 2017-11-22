using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.ElasticBeanstalk.Tools
{
    public class ElasticBeanstalkExceptions : ToolsException
    {
        public enum EBCode
        {
            EnsureBucketExistsError,
            FailedToUploadBundle,
            FailedToFindEnvironment,
            FailedEnvironmentUpdate,
            FailedCreateApplication,
            FailedCreateApplicationVersion,
            FailedToUpdateTags,
            FailedToDeleteEnvironment,
            FailedToUpdateEnvironment,
            FailedToCreateEnvironment
        }

        public ElasticBeanstalkExceptions(string message, EBCode code) : base(message, code.ToString(), null)
        {
        }

        public ElasticBeanstalkExceptions(string message, CommonErrorCode code) : base(message, code.ToString(), null)
        {
        }

        public ElasticBeanstalkExceptions(string message, EBCode code, Exception e) : base(message, code.ToString(), e)
        {
        }

    }
}
