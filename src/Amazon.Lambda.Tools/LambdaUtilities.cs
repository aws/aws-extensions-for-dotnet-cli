using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdParty.Json.LitJson;
using System.Xml.Linq;
using Amazon.Common.DotNetCli.Tools;

using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.Xml.XPath;
using Environment = System.Environment;

namespace Amazon.Lambda.Tools
{
    public static class LambdaUtilities
    {
        public static readonly IList<string> ValidProjectExtensions = new List<string> { ".csproj", ".fsproj", ".vbproj" };

        static readonly IReadOnlyDictionary<string, string> _lambdaRuntimeToDotnetFramework = new Dictionary<string, string>()
        {
            // Using string "dotnetcore3.1" instead of Runtime.Dotnetcore31 because the AWS SDK for .NET hasn't been updated yet with the new value.
            {"dotnetcore3.1", "netcoreapp3.1"},
            {Amazon.Lambda.Runtime.Dotnetcore21.Value, "netcoreapp2.1"},
            {Amazon.Lambda.Runtime.Dotnetcore20.Value, "netcoreapp2.0"},
            {Amazon.Lambda.Runtime.Dotnetcore10.Value, "netcoreapp1.0"}
        };

        public static string DetermineTargetFrameworkFromLambdaRuntime(string lambdaRuntime, string projectLocation)
        {
            string framework;
            if (_lambdaRuntimeToDotnetFramework.TryGetValue(lambdaRuntime, out framework))
                return framework;

            framework = Utilities.LookupTargetFrameworkFromProjectFile(projectLocation);
            return framework;
        }

        public static string DetermineLambdaRuntimeFromTargetFramework(string targetFramework)
        {
            var kvp = _lambdaRuntimeToDotnetFramework.FirstOrDefault(x => string.Equals(x.Value, targetFramework, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(kvp.Key))
                return null;

            return kvp.Key;
        }

        /// <summary>
        /// Make sure nobody is trying to deploy a function based on a higher .NET Core framework than the Lambda runtime knows about.
        /// </summary>
        /// <param name="lambdaRuntime"></param>
        /// <param name="targetFramework"></param>
        public static void ValidateTargetFrameworkAndLambdaRuntime(string lambdaRuntime, string targetFramework)
        {
            if (lambdaRuntime.Length < 3)
                return;

            string suffix = lambdaRuntime.Substring(lambdaRuntime.Length - 3);
            Version runtimeVersion;
            if (!Version.TryParse(suffix, out runtimeVersion))
                return;

            if (targetFramework.Length < 3)
                return;

            suffix = targetFramework.Substring(targetFramework.Length - 3);
            Version frameworkVersion;
            if (!Version.TryParse(suffix, out frameworkVersion))
                return;

            if (runtimeVersion < frameworkVersion)
            {
                throw new LambdaToolsException($"The framework {targetFramework} is a newer version than Lambda runtime {lambdaRuntime} supports", LambdaToolsException.LambdaErrorCode.FrameworkNewerThanRuntime);
            }
        }

        public static string LoadPackageStoreManifest(IToolLogger logger, string targetFramework)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LambdaConstants.ENV_DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE)))
            {
                var filePath = Environment.GetEnvironmentVariable(LambdaConstants.ENV_DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE);
                if (File.Exists(filePath))
                {
                    logger?.WriteLine($"Using local manifest override: {filePath}");
                    return File.ReadAllText(filePath);
                }
                else
                {
                    logger?.WriteLine("Using local manifest override");
                    return null;
                }
            }

            string manifestFilename = null;
            if (string.Equals("netcoreapp2.0", targetFramework, StringComparison.OrdinalIgnoreCase))
                manifestFilename = "LambdaPackageStoreManifest.xml";
            else if (string.Equals("netcoreapp2.1", targetFramework, StringComparison.OrdinalIgnoreCase))
                manifestFilename = "LambdaPackageStoreManifest-v2.1.xml";

            if (manifestFilename == null)
                return null;

            return ToolkitConfigFileFetcher.Instance.GetFileContentAsync(logger, manifestFilename).Result;
        }


        public static string ProcessTemplateSubstitions(IToolLogger logger, string templateBody, IDictionary<string, string> substitutions, string workingDirectory)
        {
            if (DetermineTemplateFormat(templateBody) != TemplateFormat.Json || substitutions == null || !substitutions.Any())
                return templateBody;

            logger?.WriteLine($"Processing {substitutions.Count} substitutions.");
            var root = JsonConvert.DeserializeObject(templateBody) as JObject;

            foreach (var kvp in substitutions)
            {
                logger?.WriteLine($"Processing substitution: {kvp.Key}");
                var token = root.SelectToken(kvp.Key);
                if (token == null)
                    throw new LambdaToolsException($"Failed to locate JSONPath {kvp.Key} for template substitution.", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);

                logger?.WriteLine($"\tFound element of type {token.Type}");

                string replacementValue;
                if (workingDirectory != null && File.Exists(Path.Combine(workingDirectory, kvp.Value)))
                {
                    var path = Path.Combine(workingDirectory, kvp.Value);
                    logger?.WriteLine($"\tReading: {path}");
                    replacementValue = File.ReadAllText(path);
                }
                else
                {
                    replacementValue = kvp.Value;
                }

                try
                {
                    switch (token.Type)
                    {
                        case JTokenType.String:
                            ((JValue)token).Value = replacementValue;
                            break;
                        case JTokenType.Boolean:
                            bool b;
                            if (bool.TryParse(replacementValue, out b))
                            {
                                ((JValue)token).Value = b;
                            }
                            else
                            {
                                throw new LambdaToolsException($"Failed to convert {replacementValue} to a bool", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                            }

                            break;
                        case JTokenType.Integer:
                            int i;
                            if (int.TryParse(replacementValue, out i))
                            {
                                ((JValue)token).Value = i;
                            }
                            else
                            {
                                throw new LambdaToolsException($"Failed to convert {replacementValue} to an int", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                            }
                            break;
                        case JTokenType.Float:
                            double d;
                            if (double.TryParse(replacementValue, out d))
                            {
                                ((JValue)token).Value = d;
                            }
                            else
                            {
                                throw new LambdaToolsException($"Failed to convert {replacementValue} to a double", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                            }
                            break;
                        case JTokenType.Array:
                        case JTokenType.Object:
                            var jcon = token as JContainer;
                            var jprop = jcon.Parent as JProperty;
                            JToken subData;
                            try
                            {
                                subData = JsonConvert.DeserializeObject(replacementValue) as JToken;
                            }
                            catch (Exception e)
                            {
                                throw new LambdaToolsException($"Failed to parse substitue JSON data: {e.Message}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                            }
                            jprop.Value = subData;
                            break;
                        default:
                            throw new LambdaToolsException($"Unable to determine how to convert substitute value into the template. " +
                                                            "Make sure to have a default value in the template which is used to determine the type. " +
                                                            "For example \"\" for string fields or {} for JSON objects.",
                                                            LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                    }
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException($"Error setting property {kvp.Key} with value {kvp.Value}: {e.Message}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateSubstitutionError);
                }
            }

            var json = JsonConvert.SerializeObject(root);
            return json;
        }


        /// <summary>
        /// Search for the CloudFormation resources that references the app bundle sent to S3 and update them.
        /// </summary>
        /// <param name="templateBody"></param>
        /// <param name="s3Bucket"></param>
        /// <param name="s3Key"></param>
        /// <returns></returns>
        public static string UpdateCodeLocationInTemplate(string templateBody, string s3Bucket, string s3Key)
        {
            switch (LambdaUtilities.DetermineTemplateFormat(templateBody))
            {
                case TemplateFormat.Json:
                    return UpdateCodeLocationInJsonTemplate(templateBody, s3Bucket, s3Key);
                case TemplateFormat.Yaml:
                    return UpdateCodeLocationInYamlTemplate(templateBody, s3Bucket, s3Key);
                default:
                    throw new LambdaToolsException("Unable to determine template file format", LambdaToolsException.LambdaErrorCode.ServerlessTemplateParseError);
            }
        }

        public static string UpdateCodeLocationInJsonTemplate(string templateBody, string s3Bucket, string s3Key)
        {
            var s3Url = $"s3://{s3Bucket}/{s3Key}";
            JsonData root;
            try
            {
                root = JsonMapper.ToObject(templateBody);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error parsing CloudFormation template: {e.Message}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateParseError, e);
            }

            var resources = root["Resources"];
            if (resources == null)
                throw new LambdaToolsException("CloudFormation template does not define any AWS resources", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingResourceSection);


            foreach (var field in resources.PropertyNames)
            {
                var resource = resources[field];
                if (resource == null)
                    continue;

                var properties = resource["Properties"];
                if (properties == null)
                    continue;

                var type = resource["Type"]?.ToString();
                if (string.Equals(type, "AWS::Serverless::Function", StringComparison.Ordinal))
                {
                    properties["CodeUri"] = s3Url;
                }

                if (string.Equals(type, "AWS::Lambda::Function", StringComparison.Ordinal))
                {
                    var code = new JsonData();
                    code["S3Bucket"] = s3Bucket;
                    code["S3Key"] = s3Key;
                    properties["Code"] = code;
                }
            }

            var json = JsonMapper.ToJson(root);
            return json;
        }

        public static string UpdateCodeLocationInYamlTemplate(string templateBody, string s3Bucket, string s3Key)
        {
            var s3Url = $"s3://{s3Bucket}/{s3Key}";

            // Setup the input
            var input = new StringReader(templateBody);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Examine the stream
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;

            if (root == null)
                return templateBody;

            var resourcesKey = new YamlScalarNode("Resources");

            if (!root.Children.ContainsKey(resourcesKey))
                return templateBody;

            var resources = (YamlMappingNode)root.Children[resourcesKey];

            foreach (var resource in resources.Children)
            {
                var resourceBody = (YamlMappingNode)resource.Value;
                var type = (YamlScalarNode)resourceBody.Children[new YamlScalarNode("Type")];
                var properties = (YamlMappingNode)resourceBody.Children[new YamlScalarNode("Properties")];

                if (properties == null) continue;
                if (type == null) continue;

                if (string.Equals(type?.Value, "AWS::Serverless::Function", StringComparison.Ordinal))
                {
                    properties.Children.Remove(new YamlScalarNode("CodeUri"));
                    properties.Add("CodeUri", s3Url);
                }
                else if (string.Equals(type?.Value, "AWS::Lambda::Function", StringComparison.Ordinal))
                {
                    properties.Children.Remove(new YamlScalarNode("Code"));
                    var code = new YamlMappingNode();
                    code.Add("S3Bucket", s3Bucket);
                    code.Add("S3Key", s3Key);

                    properties.Add("Code", code);
                }
            }
            var myText = new StringWriter();
            yaml.Save(myText);

            return myText.ToString();
        }


        internal static TemplateFormat DetermineTemplateFormat(string templateBody)
        {
            templateBody = templateBody.Trim();
            if (templateBody.Length > 0 && templateBody[0] == '{')
                return TemplateFormat.Json;

            return TemplateFormat.Yaml;
        }

        /// <summary>
        /// If the template is a JSON document get the list of parameters to make sure the passed in parameters are valid for the template.
        /// </summary>
        /// <param name="templateBody"></param>
        /// <returns></returns>
        internal static List<Tuple<string, bool>> GetTemplateDefinedParameters(string templateBody)
        {
            if (templateBody.Trim().StartsWith("{"))
                return GetJsonTemplateDefinedParameters(templateBody);
            else
                return GetYamlTemplateDefinedParameters(templateBody);
        }

        private static List<Tuple<string, bool>> GetJsonTemplateDefinedParameters(string templateBody)
        {
            try
            {
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject(templateBody) as JObject;
                if (root == null)
                    return null;

                var parameters = root["Parameters"] as JObject;

                var parms = new List<Tuple<string, bool>>();
                if (parameters == null)
                    return parms;

                foreach (var property in parameters.Properties())
                {
                    var noEcho = false;
                    var prop = parameters[property.Name] as JObject;
                    if (prop != null && prop["NoEcho"] != null)
                    {
                        noEcho = Boolean.Parse(prop["NoEcho"].ToString());
                    }

                    parms.Add(new Tuple<string, bool>(property.Name, noEcho));
                }

                return parms;
            }
            catch
            {
                return null;
            }
        }

        private static List<Tuple<string, bool>> GetYamlTemplateDefinedParameters(string templateBody)
        {
            try
            {
                var yaml = new YamlStream();
                yaml.Load(new StringReader(templateBody));

                // Examine the stream
                var root = (YamlMappingNode)yaml.Documents[0].RootNode;
                if (root == null)
                    return null;

                var parms = new List<Tuple<string, bool>>();

                var parametersKey = new YamlScalarNode("Parameters");
                if (!root.Children.ContainsKey(parametersKey))
                    return parms;

                var parameters = (YamlMappingNode)root.Children[parametersKey];

                var noEchoKey = new YamlScalarNode("NoEcho");

                foreach (var parameter in parameters.Children)
                {
                    var parameterBody = parameter.Value as YamlMappingNode;
                    if (parameterBody == null)
                        continue;

                    var noEcho = false;
                    if(parameterBody.Children.ContainsKey(noEchoKey))
                    {
                        noEcho = bool.Parse(parameterBody.Children[noEchoKey].ToString());
                    }

                    parms.Add(new Tuple<string, bool>(parameter.Key.ToString(), noEcho));
                }

                return parms;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<LayerPackageInfo> LoadLayerPackageInfos(IToolLogger logger, IAmazonLambda lambdaClient, IAmazonS3 s3Client, IEnumerable<string> layerVersionArns)
        {
            var info = new LayerPackageInfo();
            if (layerVersionArns == null || !layerVersionArns.Any())
                return info;

            logger.WriteLine("Inspecting Lambda layers for runtime package store manifests");
            foreach(var arn in layerVersionArns)
            {
                try
                {
                    var p = ParseLayerVersionArn(arn);
                    var getLayerResponse = await lambdaClient.GetLayerVersionAsync(new GetLayerVersionRequest { LayerName = p.Name, VersionNumber = p.VersionNumber });

                    LayerDescriptionManifest manifest;
                    if (!LambdaUtilities.AttemptToParseLayerDescriptionManifest(getLayerResponse.Description, out manifest))
                    {
                        logger.WriteLine($"... {arn}: Skipped, does not contain a layer description manifest");
                        continue;
                    }
                    if (manifest.Nlt != LayerDescriptionManifest.ManifestType.RuntimePackageStore)
                    {
                        logger.WriteLine($"... {arn}: Skipped, layer is of type {manifest.Nlt.ToString()}, not {LayerDescriptionManifest.ManifestType.RuntimePackageStore}");
                        continue;
                    }

                    string filePath = Path.GetTempFileName();
                    using (var getResponse = await s3Client.GetObjectAsync(manifest.Buc, manifest.Key))
                    using (var reader = new StreamReader(getResponse.ResponseStream))
                    {
                        await getResponse.WriteResponseStreamToFileAsync(filePath, false, default(System.Threading.CancellationToken));
                    }

                    logger.WriteLine($"... {arn}: Downloaded package manifest for runtime package store layer");
                    info.Items.Add(new LayerPackageInfo.LayerPackageInfoItem
                    {
                        Directory = manifest.Dir,
                        ManifestPath = filePath
                    });
                }
                catch(Exception e)
                {
                    logger.WriteLine($"... {arn}: Skipped, error inspecting layer. {e.Message}");
                }
            }

            return info;
        }

        internal static bool AttemptToParseLayerDescriptionManifest(string json, out LayerDescriptionManifest manifest)
        {
            manifest = null;
            if (string.IsNullOrEmpty(json) || json[0] != '{')
                return false;

            try
            {
                manifest = JsonMapper.ToObject<LayerDescriptionManifest>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal class ParseLayerVersionArnResult
        {
            internal string Name { get; }
            internal long VersionNumber { get; }

            internal ParseLayerVersionArnResult(string name, long versionNumber)
            {
                this.Name = name;
                this.VersionNumber = versionNumber;
            }
        }
        internal static ParseLayerVersionArnResult ParseLayerVersionArn(string layerVersionArn)
        {
            try
            {
                int pos = layerVersionArn.LastIndexOf(':');

                var number = long.Parse(layerVersionArn.Substring(pos + 1));
                var arn = layerVersionArn.Substring(0, pos);

                return new ParseLayerVersionArnResult(arn, number);
            }
            catch (Exception)
            {
                throw new LambdaToolsException("Error parsing layer version arn into layer name and version number",
                    LambdaToolsException.LambdaErrorCode.ParseLayerVersionArnFail);
            }
        }
        
        public static string DetermineListDisplayLayerDescription(string description, int maxDescriptionLength)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";
            try
            {
                LayerDescriptionManifest manifest;
                var parsed = AttemptToParseLayerDescriptionManifest(description, out manifest);
                if(parsed)
                {
                    if (manifest?.Nlt == LayerDescriptionManifest.ManifestType.RuntimePackageStore)
                    {
                        if (manifest.Op == LayerDescriptionManifest.OptimizedState.Optimized)
                            return LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE_DISPLAY_NAME + " (Optimized)";

                        return LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE_DISPLAY_NAME;
                    }
                }
            }
            catch (Exception)
            {
            }

            if (description.Length <= maxDescriptionLength)
            {
                return description;
            }
            
            return description.Substring(0, maxDescriptionLength);
        }
        

        public class ConvertManifestToSdkManifestResult
        {
            public bool ShouldDelete { get; }
            public string PackageManifest { get; }

            public ConvertManifestToSdkManifestResult(bool shouldDelete, string packageManifest)
            {
                this.ShouldDelete = shouldDelete;
                this.PackageManifest = packageManifest;
            }
        }

        public static ConvertManifestToSdkManifestResult ConvertManifestToSdkManifest(string targetFramework, string packageManifest)
        {
            var content = File.ReadAllText(packageManifest);

            var result = ConvertManifestContentToSdkManifest(targetFramework, content);

            if (!result.Updated)
            {
                return new ConvertManifestToSdkManifestResult(false, packageManifest);
            }

            var newPath = Path.GetTempFileName();
            File.WriteAllText(newPath, result.UpdatedContent);
            return new ConvertManifestToSdkManifestResult(true, newPath);

        }

        public class ConvertManifestContentToSdkManifestResult
        {
            public bool Updated { get; }
            public string UpdatedContent { get; }

            public ConvertManifestContentToSdkManifestResult(bool updated, string updatedContent)
            {
                this.Updated = updated;
                this.UpdatedContent = updatedContent;
            }
        }

        public static ConvertManifestContentToSdkManifestResult ConvertManifestContentToSdkManifest(string targetFramework, string packageManifestContent)
        {
            var originalDoc = XDocument.Parse(packageManifestContent);

            var sdkType = originalDoc.Root.Attribute("Sdk")?.Value ?? "Microsoft.NET.Sdk";
            var isWebSdk = string.Equals(sdkType, "Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);

            if (string.Equals("netcoreapp2.1", targetFramework) && !isWebSdk)
                return new ConvertManifestContentToSdkManifestResult(false, packageManifestContent);

            
            var root = new XElement("Project");
            root.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");

            var itemGroup = new XElement("ItemGroup");
            root.Add(itemGroup);


            Version dotnetSdkVersion;
            try
            {
                dotnetSdkVersion = Amazon.Common.DotNetCli.Tools.DotNetCLIWrapper.GetSdkVersion();
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error detecting .NET SDK version: \n\t" + e.Message, LambdaToolsException.LambdaErrorCode.FailedToDetectSdkVersion, e );
            }


            if (isWebSdk)
            {
                if(string.Equals("netcoreapp2.1", targetFramework, StringComparison.OrdinalIgnoreCase))
                {
                    if (dotnetSdkVersion < LambdaConstants.MINIMUM_DOTNET_SDK_VERSION_FOR_ASPNET_LAYERS)
                    {
                        throw new LambdaToolsException($"To create a runtime package store layer for an ASP.NET Core project " +
                                                       $"version {LambdaConstants.MINIMUM_DOTNET_SDK_VERSION_FOR_ASPNET_LAYERS} " +
                                                       "or above of the .NET Core SDK must be installed. " +
                                                       "If a 2.1.X SDK is used then the \"dotnet store\" command will include all " +
                                                       "of the ASP.NET Core dependencies that are already available in Lambda.",
                            LambdaToolsException.LambdaErrorCode.LayerNetSdkVersionMismatch);
                    }

                    // These were added to make sure the ASP.NET Core dependencies are filter if any of the packages
                    // depend on them.
                    // See issue for more info: https://github.com/dotnet/cli/issues/10784
                    var aspNerCorePackageReference = new XElement("PackageReference");
                    aspNerCorePackageReference.SetAttributeValue("Include", "Microsoft.AspNetCore.App");
                    itemGroup.Add(aspNerCorePackageReference);

                    var aspNerCoreUpdatePackageReference = new XElement("PackageReference");
                    aspNerCoreUpdatePackageReference.SetAttributeValue("Update", "Microsoft.NETCore.App");
                    aspNerCoreUpdatePackageReference.SetAttributeValue("Publish", "false");
                    itemGroup.Add(aspNerCoreUpdatePackageReference);
                }   
                else
                {
                    var frameworkReference = new XElement("FrameworkReference");
                    frameworkReference.SetAttributeValue("Include", "Microsoft.AspNetCore.App");
                    var frameworkReferenceGroupItemGroup = new XElement("ItemGroup");
                    frameworkReferenceGroupItemGroup.Add(frameworkReference);
                    root.Add(frameworkReferenceGroupItemGroup);
                }
            }


            foreach (var packageReference in originalDoc.XPathSelectElements("//ItemGroup/PackageReference"))
            {
                var packageName = packageReference.Attribute("Include")?.Value;
                var version = packageReference.Attribute("Version")?.Value;

                if (string.Equals(packageName, "Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(packageName, "Microsoft.AspNetCore.All", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                var newRef = new XElement("PackageReference");
                newRef.SetAttributeValue("Include", packageName);
                newRef.SetAttributeValue("Version", version);
                itemGroup.Add(newRef);
            }
            
            // In .NET Core 3.1 the dotnet store command will include system dependencies like System.Runtime if 
            // any of the packages referenced in the packages included explicit references system dependencies.
            // This is common on older packages or versions of packages before .NET Core 2.1. Newtonsoft.Json version 9.0.1
            // is an example of this behavior.
            //
            // To avoid these system dependencies getting added to the layer we need to inject the list of system
            // dependency to prune from the store graph.
            // 
            // For further information on the issue check out this GitHub issue: https://github.com/dotnet/sdk/issues/10973 
            if (string.Equals(targetFramework, "netcoreapp3.1", StringComparison.OrdinalIgnoreCase))
            {
                var lambdaAssembly = typeof(LambdaUtilities).Assembly;
                string manifestName;
                if (isWebSdk)
                {
                    manifestName = lambdaAssembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(LambdaConstants.PRUNE_LIST_SDKWEB_XML));
                }
                else
                {
                    manifestName = lambdaAssembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(LambdaConstants.PRUNE_LIST_SDK_XML));
                }

                string pruneListString;
                using (var stream = lambdaAssembly.GetManifestResourceStream(manifestName))
                {
                    pruneListString = new StreamReader(stream).ReadToEnd();
                }

                var pruneListElement = XElement.Parse(pruneListString);
                
                root.Add(pruneListElement);
            }
            
            var updatedDoc = new XDocument(root);
            var updatedContent = updatedDoc.ToString();
            
            return new ConvertManifestContentToSdkManifestResult(true, updatedContent);
        }

        /// <summary>
        /// Determines what runtime identifier (RID) to use when running a dotnet CLI command. For the
        /// older .NET Lambda runtimes that are not running on Amazon Linux 2 keep using the existing rhel.7.2-x64
        /// RID. For all other runtimes that are running on Amazon Linux 2 use the newer linux-x64 RID which did
        /// not exist when this tool was first created.
        /// </summary>
        /// <param name="targetFramework"></param>
        /// <returns></returns>
        public static string DetermineRuntimeParameter(string targetFramework)
        {
            switch (targetFramework)
            {
                case "netcoreapp1.0":
                case "netcoreapp2.0":
                case "netcoreapp2.1":
                    return LambdaConstants.LEGACY_RUNTIME_HIERARCHY_STARTING_POINT;
                default:
                    return "linux-x64";
            }
        }
    }
}
