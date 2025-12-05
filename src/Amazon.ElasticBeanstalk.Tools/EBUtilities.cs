using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticBeanstalk.Tools.Commands;

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

                var data = new Dictionary<string, object>();
                string existingJson = File.ReadAllText(pathToManifest);
                using (JsonDocument doc = JsonDocument.Parse(existingJson))
                {
                    foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
                    {
                        data[prop.Name] = prop.Value.GetJsonValue();
                    }
                }

                if (!data.ContainsKey("manifestVersion") || !(data["manifestVersion"] is int))
                {
                    data["manifestVersion"] = 1;
                }

                if (!data.ContainsKey("deployments"))
                    data["deployments"] = new Dictionary<string, object>();
                var deployments = (Dictionary<string, object>)data["deployments"];

                if (!deployments.ContainsKey("aspNetCoreWeb"))
                    deployments["aspNetCoreWeb"] = new List<object>();
                var aspNetCoreWeb = (List<object>)deployments["aspNetCoreWeb"];

                Dictionary<string, object> appNode;
                if (aspNetCoreWeb.Count == 0)
                {
                    appNode = new Dictionary<string, object>();
                    aspNetCoreWeb.Add(appNode);
                }
                else
                    appNode = (Dictionary<string, object>)aspNetCoreWeb[0];

                if (!appNode.ContainsKey("name") || !(appNode["name"] is string name) || string.IsNullOrEmpty(name))
                {
                    appNode["name"] = "app";
                }

                if (!appNode.ContainsKey("parameters"))
                    appNode["parameters"] = new Dictionary<string, object>();
                var parameters = (Dictionary<string, object>)appNode["parameters"];
                parameters["appBundle"] = ".";
                parameters["iisPath"] = iisAppPath;
                parameters["iisWebSite"] = iisWebSite;

                var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
                manifest = JsonSerializer.Serialize(data, jsonSerializerOptions);
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

        public static bool IsSolutionStackWindows(string solutionStackName)
        {
            return solutionStackName.Contains("64bit Windows Server");
        }

        public static bool IsSolutionStackLinuxNETCore(string solutionStackName)
        {
            return IsSolutionStackLinux(solutionStackName) && IsSolutionStackNETCore(solutionStackName);
        }

        public static bool IsSolutionStackLinux(string solutionStackName)
        {
            return solutionStackName.StartsWith("64bit Amazon Linux 2");
        }

        public static bool IsSolutionStackNETCore(string solutionStackName)
        {
            return solutionStackName.Contains(".NET Core") || solutionStackName.Contains("DotNetCore");
        }

        public static bool IsLoadBalancedEnvironmentType(string environmentType)
        {
            return string.Equals(environmentType, EBConstants.ENVIRONMENT_TYPE_LOADBALANCED, StringComparison.OrdinalIgnoreCase);
        }


        public static void SetupPackageForLinux(IToolLogger logger, EBBaseCommand command, DeployEnvironmentProperties options, string publishLocation, string reverseProxy, int? applicationPort, string projectLocation)
        {
            // Setup Procfile
            var procfilePath = Path.Combine(publishLocation, "Procfile");

            if (File.Exists(procfilePath))
            {
                logger?.WriteLine("Found existing Procfile file found and using that for deployment");
                return;
            }

            logger?.WriteLine("Writing Procfile for deployment bundle");
            var executingAssembly = Utilities.LookupAssemblyNameFromProjectFile(projectLocation, null);
            var runtimeConfigFilePath = Directory.GetFiles(publishLocation, $"{executingAssembly}.runtimeconfig.json").FirstOrDefault();

            string webCommandLine;
            if (IsSelfContainedPublish(runtimeConfigFilePath))
            {
                webCommandLine = $"./{executingAssembly}";
            }
            else
            {
                webCommandLine = $"dotnet exec ./{executingAssembly}.dll";
            }

            if (string.Equals(reverseProxy, EBConstants.PROXY_SERVER_NONE, StringComparison.InvariantCulture))
            {
                logger?.WriteLine("... Proxy server disabled, configuring Kestrel to listen to traffic from all hosts");
                var port = applicationPort.HasValue ? applicationPort.Value : EBConstants.DEFAULT_APPLICATION_PORT;
                webCommandLine += $" --urls http://0.0.0.0:{port.ToString(CultureInfo.InvariantCulture)}/";
            }

            var content = "web: " + webCommandLine;
            logger?.WriteLine($"... Procfile command used to start application");
            logger?.WriteLine($"    {content}");
            File.WriteAllText(procfilePath, content);
        }

        public static bool IsSelfContainedPublish(string runtimeConfigFile)
        {
            if(File.Exists(runtimeConfigFile))
            {
                runtimeConfigFile = File.ReadAllText(runtimeConfigFile);
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(runtimeConfigFile))
                {
                    if (!doc.RootElement.TryGetProperty("runtimeOptions", out JsonElement runtimeOptions))
                        return false;

                    if (!runtimeOptions.TryGetProperty("includedFrameworks", out JsonElement includedFrameworks))
                        return false;

                    return includedFrameworks.ValueKind == JsonValueKind.Array && includedFrameworks.GetArrayLength() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string FindExistingValue(this List<ConfigurationOptionSetting> settings, string ns, string name)
        {
            var setting = settings?.FirstOrDefault(x => string.Equals(x.Namespace, ns, StringComparison.InvariantCulture) && string.Equals(x.OptionName, name, StringComparison.InvariantCulture));
            return setting?.Value;
        }
    }
}
