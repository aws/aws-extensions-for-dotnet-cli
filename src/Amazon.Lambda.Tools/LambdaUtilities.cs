using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using YamlDotNet.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdParty.Json.LitJson;
using System.Xml.Linq;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Lambda.Tools
{
    public static class LambdaUtilities
    {


        /// <summary>
        /// Make sure nobody is trying to deploy a function based on a higher .NET Core framework then the Lambda runtime knows about.
        /// </summary>
        /// <param name="lambdaRuntime"></param>
        /// <param name="targetFramework"></param>
        public static void ValidateTargetFrameworkAndLambdaRuntime(string lambdaRuntime, string targetFramework)
        {
            if (lambdaRuntime.Length < 3)
                return;

            string suffix = lambdaRuntime.Substring(lambdaRuntime.Length - 3);
            if (!Version.TryParse(suffix, out var runtimeVersion))
                return;

            if (targetFramework.Length < 3)
                return;

            suffix = targetFramework.Substring(targetFramework.Length - 3);
            if (!Version.TryParse(suffix, out var frameworkVersion))
                return;

            if (runtimeVersion < frameworkVersion)
            {
                throw new LambdaToolsException($"The framework {targetFramework} is a newer version than Lambda runtime {lambdaRuntime} supports", LambdaToolsException.LambdaErrorCode.FrameworkNewerThanRuntime);
            }
        }


        public static void ValidateMicrosoftAspNetCoreAllReference(IToolLogger logger, string csprofPath, out string manifestContent)
        {
            if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LambdaConstants.ENV_DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE)))
            {
                var filePath = Environment.GetEnvironmentVariable(LambdaConstants.ENV_DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE);
                if(File.Exists(filePath))
                {
                    logger?.WriteLine($"Using local manifest override: {filePath}");
                    manifestContent = File.ReadAllText(filePath);
                }
                else
                {
                    logger?.WriteLine("Using local manifest override");
                    manifestContent = null;
                }
            }
            else
            {
                manifestContent = ToolkitConfigFileFetcher.Instance.GetFileContentAsync(logger, "LambdaPackageStoreManifest.xml").Result;
            }
            if (string.IsNullOrEmpty(manifestContent))
            {
                return;
            }

            if (Directory.Exists(csprofPath))
            {
                var projectFiles = Directory.GetFiles(csprofPath, "*.csproj", SearchOption.TopDirectoryOnly);
                if(projectFiles.Length != 1)
                {
                    logger?.WriteLine("Unable to determine csproj project file when validating version of Microsoft.AspNetCore.All");
                    return;
                }
                csprofPath = projectFiles[0];
            }

            // If the file is not a csproj file then skip validation. This could happen
            // if the project is an F# project or an older style project.json.
            if (!string.Equals(Path.GetExtension(csprofPath), ".csproj"))
                return;

            var projectContent = File.ReadAllText(csprofPath);

            
            ValidateMicrosoftAspNetCoreAllReferenceWithManifest(logger, manifestContent, projectContent);
        }

        /// <summary>
        /// Make sure that if the project references the Microsoft.AspNetCore.All package which is in implicit package store
        /// that the Lambda runtime has that store available. Otherwise the Lambda function will fail with an Internal server error.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="manifestContent"></param>
        /// <param name="csprojContent"></param>
        public static void ValidateMicrosoftAspNetCoreAllReferenceWithManifest(IToolLogger logger, string manifestContent, string csprojContent)
        {
            const string ASPNET_CORE_ALL = "Microsoft.AspNetCore.All";
            try
            {
                XDocument csprojXmlDoc = XDocument.Parse(csprojContent);

                Func<string> searchForAspNetCoreAllVersion = () =>
                {
                    // Not using XPath because to avoid adding an addition dependency for a simple one time use.
                    foreach (var group in csprojXmlDoc.Root.Elements("ItemGroup"))
                    {
                        foreach (XElement packageReference in group.Elements("PackageReference"))
                        {
                            var name = packageReference.Attribute("Include")?.Value;
                            if (string.Equals(name, ASPNET_CORE_ALL, StringComparison.Ordinal))
                            {
                                return packageReference.Attribute("Version")?.Value;
                            }
                        }
                    }

                    return null;
                };

                var projectAspNetCoreVersion = searchForAspNetCoreAllVersion();

                if (string.IsNullOrEmpty(projectAspNetCoreVersion))
                {
                    // Project is not using Microsoft.AspNetCore.All so skip validation.
                    return;
                }


                var manifestXmlDoc = XDocument.Parse(manifestContent);

                string latestLambdaDeployedVersion = null;
                foreach (var element in manifestXmlDoc.Root.Elements("Package"))
                {
                    var name = element.Attribute("Id")?.Value;
                    if (string.Equals(name, ASPNET_CORE_ALL, StringComparison.Ordinal))
                    {
                        var version = element.Attribute("Version")?.Value;
                        if (string.Equals(projectAspNetCoreVersion, version, StringComparison.Ordinal))
                        {
                            // Version specifed in project file is available in Lambda Runtime
                            return;
                        }

                        // Record latest supported version to provide meaningful error message.
                        if (latestLambdaDeployedVersion == null || Version.Parse(latestLambdaDeployedVersion) < Version.Parse(version))
                        {
                            latestLambdaDeployedVersion = version;
                        }
                    }
                }

                throw new LambdaToolsException($"Project is referencing version {projectAspNetCoreVersion} of {ASPNET_CORE_ALL} which is newer " +
                    $"than {latestLambdaDeployedVersion}, the latest version available in the Lambda Runtime environment. Please update your project to " +
                    $"use version {latestLambdaDeployedVersion} and then redeploy your Lambda function.", LambdaToolsException.LambdaErrorCode.AspNetCoreAllValidation);
            }
            catch (LambdaToolsException)
            {
                throw;
            }
            catch(Exception e)
            {
                logger?.WriteLine($"Unknown error validating version of {ASPNET_CORE_ALL}: {e.Message}");
            }
        }


        public static string ProcessTemplateSubstitions(IToolLogger logger, string templateBody, IDictionary<string, string> substitutions, string workingDirectory)
        {
            if (DetermineTemplateFormat(templateBody) != TemplateFormat.Json || substitutions == null || !substitutions.Any())
                return templateBody;

            logger?.WriteLine($"Processing {substitutions.Count} substitutions.");
            var root = JsonConvert.DeserializeObject(templateBody) as JObject;

            foreach(var kvp in substitutions)
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
                    switch(token.Type)
                    {
                        case JTokenType.String:
                            ((JValue)token).Value = replacementValue;
                            break;
                        case JTokenType.Boolean:
                            bool b;
                            if(bool.TryParse(replacementValue, out b))
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
                            catch(Exception e)
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
                catch(Exception e)
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
            var deserialize = new YamlDotNet.Serialization.Deserializer();

            var root = deserialize.Deserialize(new StringReader(templateBody)) as Dictionary<object, object>;
            if (root == null)
                return templateBody;

            if (!root.ContainsKey("Resources"))
                return templateBody;

            var resources = root["Resources"] as IDictionary<object, object>;


            foreach(var kvp in resources)
            {
                var resource = kvp.Value as IDictionary<object, object>;
                if (resource == null)
                    continue;

                if (!resource.ContainsKey("Properties"))
                    continue;
                var properties = resource["Properties"] as IDictionary<object, object>;


                if (!resource.ContainsKey("Type"))
                    continue;

                var type = resource["Type"]?.ToString();
                if (string.Equals(type, "AWS::Serverless::Function", StringComparison.Ordinal))
                {
                    properties["CodeUri"] = s3Url;
                }

                if (string.Equals(type, "AWS::Lambda::Function", StringComparison.Ordinal))
                {
                    var code = new Dictionary<object, object>();
                    code["S3Bucket"] = s3Bucket;
                    code["S3Key"] = s3Key;
                    properties["Code"] = code;
                }
            }

            var serializer = new Serializer();
            var updatedTemplateBody = serializer.Serialize(root);

            return updatedTemplateBody;
        }


        internal static TemplateFormat DetermineTemplateFormat(string templateBody)
        {
            templateBody = templateBody.Trim();
            if (templateBody.Length > 0 && templateBody[0] == '{')
                return TemplateFormat.Json;

            return TemplateFormat.Yaml;
        }
    }
}
