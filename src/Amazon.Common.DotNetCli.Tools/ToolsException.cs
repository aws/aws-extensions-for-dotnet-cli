using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools
{
    public class ToolsException : Exception
    {
        public enum CommonErrorCode
        {

            DefaultsParseFail,
            CommandLineParseError,
            ProfileNotFound,
            RegionNotConfigured,
            MissingRequiredParameter,
            PersistConfigError,

            DotnetPublishFailed,

            IAMAttachRole,
            IAMCreateRole,
            IAMGetRole,
            RoleNotFound,
            PolicyNotFound,

            FailedToFindZipProgram,

            S3UploadError

        }

        public ToolsException(string message, CommonErrorCode code)
            : this(message, code, null)
        {

        }

        public ToolsException(string message, CommonErrorCode code, Exception e)
            :this(message, code.ToString(), e)
        {

        }

        protected ToolsException(string message, string errorCode, Exception e)
            : base(message)
        {
            this.Code = errorCode;

            var ae = e as AmazonServiceException;
            if (ae != null)
            {
                this.ServiceCode = $"{ae.ErrorCode}-{ae.StatusCode}";
            }
        }

        public string Code { get; }

        public string ServiceCode { get; }
    }
}
