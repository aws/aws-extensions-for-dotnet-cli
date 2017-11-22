using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Amazon.ECS.Tools.Commands;

using ThirdParty.Json.LitJson;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// This class gives access to the default values for the CommandOptions defined in the project's default json file.
    /// </summary>
    public class ECSToolsDefaults : DefaultConfigFile
    {
        public const string DEFAULT_FILE_NAME = "aws-ecs-tools-defaults.json";

        public ECSToolsDefaults()
        {

        }

        public ECSToolsDefaults(string sourceFile)
            : this(new JsonData(), sourceFile)
        {
        }

        public ECSToolsDefaults(JsonData data, string sourceFile)
            : base(data, sourceFile)
        {
        }


        public override string DefaultConfigFileName => DEFAULT_FILE_NAME;
    }
}
