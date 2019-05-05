using System;
using System.Collections.Generic;
using System.Text;

using Amazon.Runtime;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// Handle the callback from AWSCredentials when an MFA token code is required.
    /// </summary>
    internal class AssumeRoleMfaTokenCodeCallback 
    {
        AssumeRoleAWSCredentialsOptions Options { get; set; }

        internal AssumeRoleMfaTokenCodeCallback(AssumeRoleAWSCredentialsOptions options)
        {
            this.Options = options;
        }

        internal string Execute()
        {
            Console.Write($"Enter MFA code for {this.Options.MfaSerialNumber}: ");
            var code = Utilities.ReadSecretFromConsole();
            Console.WriteLine();
            return code;
        }
    }
}
