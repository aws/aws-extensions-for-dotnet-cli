using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ElasticBeanstalk.Model;
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

        public static void SetupPackageForLinux(IToolLogger logger, EBBaseCommand command, DeployEnvironmentProperties options, string publishLocation, string reverseProxy, int? applicationPort)
        {
            // Setup Procfile
            var procfilePath = Path.Combine(publishLocation, "Procfile");

            if (File.Exists(procfilePath))
            {
                logger?.WriteLine("Found existing Procfile file found and using that for deployment");
                return;
            }

            logger?.WriteLine("Writing Procfile for deployment bundle");
            string runtimeConfigFilePath = GetRuntimeConfigFilePath(publishLocation);
            var runtimeConfigFileName = Path.GetFileName(runtimeConfigFilePath);
            var executingAssembly = runtimeConfigFileName.Substring(0, runtimeConfigFileName.Length - "runtimeconfig.json".Length - 1);

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


            JsonData root = JsonMapper.ToObject(runtimeConfigFile);
            var runtimeOptions = root["runtimeOptions"] as JsonData;
            if(runtimeOptions == null)
            {
                return false;
            }

            var includedFrameworks = runtimeOptions["includedFrameworks"] as JsonData;

            return includedFrameworks != null && includedFrameworks.Count > 0;
        }


        public static string FindExistingValue(this List<ConfigurationOptionSetting> settings, string ns, string name)
        {
            var setting = settings?.FirstOrDefault(x => string.Equals(x.Namespace, ns, StringComparison.InvariantCulture) && string.Equals(x.OptionName, name, StringComparison.InvariantCulture));
            return setting?.Value;
        }

        private static string GetRuntimeConfigFilePath(string publishLocation)
        {
            var runtimeConfigFiles = Directory.GetFiles(publishLocation, "*.runtimeconfig.json");

            // Handle case where application could have multiple runtimeconfig.json files in publish location.
            if (runtimeConfigFiles.Length > 1)
            {
                foreach (var file in runtimeConfigFiles)
                {
                    var fileContent = File.ReadAllText(file);

                    JsonData root = JsonMapper.ToObject(fileContent);
                    JsonData runtimeOptions = root["runtimeOptions"] as JsonData;
                    if (runtimeOptions == null)
                    {
                        continue;
                    }

                    // runtimeOptions node can contain framework or frameworks (refer https://github.com/dotnet/sdk/blob/main/documentation/specs/runtime-configuration-file.md#runtimeoptions-section-runtimeconfigjson).
                    var frameworkNode = runtimeOptions["framework"];
                    var frameworksNode = runtimeOptions["frameworks"];

                    if (IsAspNetFramework(frameworkNode))
                    {
                        return file;
                    }

                    if (frameworksNode != null && frameworksNode.Count > 0)
                    {
                        foreach (var framework in frameworksNode)
                        {
                            var tempNode = framework as JsonData;
                            if (IsAspNetFramework(tempNode))
                            {
                                return file;
                            }
                        }
                    }
                }
            }

            return Directory.GetFiles(publishLocation, "*.runtimeconfig.json").FirstOrDefault();
        }

        private static bool IsAspNetFramework(JsonData frameworkNode)
        {
            if (frameworkNode != null)
            {
                var name = frameworkNode["name"];

                if (name != null)
                {
                    string frameworkName = (string)name;
                    return !string.IsNullOrEmpty(frameworkName) && frameworkName.StartsWith("Microsoft.AspNetCore");
                }
            }

            return false;
        }
    }
}
