using System;
using System.IO;
using System.Linq;

using Amazon.Common.DotNetCli.Tools;
using Amazon.ElasticBeanstalk.Tools.Commands;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools
{
    public static class EBUtilities                                                                   
    {
        public static void SetupAWSDeploymentManifest(IToolLogger logger, EBBaseCommand command, DeployEnvironmentProperties options, string publishLocation)
        {
            var iisAppPath = command.GetStringValueOrDefault(options.UrlPath, EBDefinedCommandOptions.ARGUMENT_APP_PATH, false) ?? "/";
            var iisWebSite = command.GetStringValueOrDefault(options.IISWebSite, EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE, false) ?? "Default Web Site";

            var pathToManifest = Path.Combine(publishLocation, "aws-windows-deployment-manifest.json");
            string manifest;
            if (File.Exists(pathToManifest))
            {
                logger?.WriteLine("Updating existing deployment manifest");

                Func<string, JsonData, JsonData> getOrCreateNode = (name, node) =>
                {
                    JsonData child = node[name] as JsonData;
                    if (child == null)
                    {
                        child = new JsonData();
                        node[name] = child;
                    }
                    return child;
                };

                JsonData root = JsonMapper.ToObject(File.ReadAllText(pathToManifest));
                if (root["manifestVersion"] == null || !root["manifestVersion"].IsInt)
                {
                    root["manifestVersion"] = 1;
                }

                JsonData deploymentNode = getOrCreateNode("deployments", root);

                JsonData aspNetCoreWebNode = getOrCreateNode("aspNetCoreWeb", deploymentNode);

                JsonData appNode;
                if (aspNetCoreWebNode.GetJsonType() == JsonType.None || aspNetCoreWebNode.Count == 0)
                {
                    appNode = new JsonData();
                    aspNetCoreWebNode.Add(appNode);
                }
                else
                    appNode = aspNetCoreWebNode[0];


                if (appNode["name"] == null || !appNode["name"].IsString || string.IsNullOrEmpty((string)appNode["name"]))
                {
                    appNode["name"] = "app";
                }

                JsonData parametersNode = getOrCreateNode("parameters", appNode);
                parametersNode["appBundle"] = ".";
                parametersNode["iisPath"] = iisAppPath;
                parametersNode["iisWebSite"] = iisWebSite;

                manifest = root.ToJson();
            }
            else
            {
                logger?.WriteLine("Creating deployment manifest");

                manifest = EBConstants.DEFAULT_MANIFEST.Replace("{iisPath}", iisAppPath).Replace("{iisWebSite}", iisWebSite);

                if (File.Exists(pathToManifest))
                    File.Delete(pathToManifest);
            }

            logger?.WriteLine("\tIIS App Path: " + iisAppPath);
            logger?.WriteLine("\tIIS Web Site: " + iisWebSite);

            File.WriteAllText(pathToManifest, manifest);
        }

        public static void SetupPackageForLinux(IToolLogger logger, EBBaseCommand command, DeployEnvironmentProperties options, string publishLocation)
        {
            // Setup Procfile
            var runtimeFilePath = Directory.GetFiles(publishLocation, "*.runtimeconfig.json").FirstOrDefault();
            var runtimeFileName = Path.GetFileName(runtimeFilePath);
            var executingAssembly = runtimeFileName.Substring(0, runtimeFileName.Length - "runtimeconfig.json".Length - 1);
            var procfilePath = Path.Combine(publishLocation, "Procfile");
            File.WriteAllText(procfilePath, $"web: ./{executingAssembly}");


            // Add config to make chmod +x the executing assembly
            var configDirectory = Path.Combine(publishLocation, ".ebextensions");
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            var configContent =
$"container_commands:\n" +
$"  01_set_file_permissions:\n" +
$"     command: \"chmod +x {executingAssembly}\"\n";

            File.WriteAllText(Path.Combine(configDirectory, "assembly-permissions.config"), configContent);
        }

        public static bool IsSolutionStackWindows(string solutionStackName)
        {
            return solutionStackName.Contains("64bit Windows Server");
        }
    }
}
