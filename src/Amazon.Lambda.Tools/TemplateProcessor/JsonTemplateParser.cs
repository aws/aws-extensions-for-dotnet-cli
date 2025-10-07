using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// JSON implementation of ITemplateParser
    /// </summary>
    public class JsonTemplateParser : ITemplateParser
    {
        Dictionary<string, object> Root { get; }
        public JsonTemplateParser(string templateBody)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(templateBody))
                {
                    this.Root = doc.RootElement.GetJsonValue() as Dictionary<string, object>;
                }
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error parsing CloudFormation template: {e.Message}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateParseError, e);
            }
        }

        public string GetUpdatedTemplate()
        {
            return JsonSerializer.Serialize(this.Root);
        }

        public IEnumerable<IUpdatableResource> UpdatableResources()
        {
            if (!this.Root.ContainsKey("Resources") || !(this.Root["Resources"] is Dictionary<string, object> resources))
                throw new LambdaToolsException("CloudFormation template does not define any AWS resources", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingResourceSection);

            foreach (var kvp in resources)
            {
                if (!(kvp.Value is Dictionary<string, object> resource))
                    continue;

                if (!resource.ContainsKey("Properties") || !(resource["Properties"] is Dictionary<string, object> properties))
                    continue;

                var type = resource.ContainsKey("Type") ? resource["Type"]?.ToString() : null;
                UpdatableResourceDefinition updatableResourceDefinition;
                if (!UpdatableResourceDefinition.ValidUpdatableResourceDefinitions.TryGetValue(type,
                    out updatableResourceDefinition))
                    continue;
                
                var updatableResource = new UpdatableResource(kvp.Key, updatableResourceDefinition, new JsonUpdatableResourceDataSource(this.Root, resource, properties));
                yield return updatableResource;
            }
        }

        /// <summary>
        /// The JSON implementation of IUpdatableResourceDataSource
        /// </summary>
        public class JsonUpdatableResourceDataSource : IUpdatableResourceDataSource
        {
            Dictionary<string, object> Root { get; }
            Dictionary<string, object> Resource { get; }
            Dictionary<string, object> Properties { get; }

            public JsonUpdatableResourceDataSource(Dictionary<string, object> root, Dictionary<string, object> resource, Dictionary<string, object> properties)
            {
                this.Root = root;
                this.Resource = resource;
                this.Properties = properties;
            }

            public string GetValueFromRoot(params string[] keyPath)
            {
                return GetValue(this.Root, keyPath);
            }
            
            public string[] GetValueListFromRoot(params string[] keyPath)
            {
                return GetValueList(this.Root, keyPath);
            }

            public string GetValueFromResource(params string[] keyPath)
            {
                return GetValue(this.Resource, keyPath);
            }

            public string GetValue(params string[] keyPath)
            {
                return GetValue(this.Properties, keyPath);
            }

            private static string GetValue(Dictionary<string, object> node, params string[] keyPath)
            {
                object current = node;
                foreach (var key in keyPath)
                {
                    if (!(current is Dictionary<string, object> dict) || !dict.ContainsKey(key))
                        return null;

                    current = dict[key];
                }

                return current?.ToString();
            }
            
            public string[] GetValueList(params string[] keyPath)
            {
                return GetValueList(this.Properties, keyPath);
            }

            public Dictionary<string, string> GetValueDictionaryFromResource(params string[] keyPath)
            {
                return GetValueDictionaryFromResource(this.Resource, keyPath);
            }

            private static string[] GetValueList(Dictionary<string, object> node, params string[] keyPath)
            {
                object current = node;
                foreach (var key in keyPath)
                {
                    if (!(current is Dictionary<string, object> dict) || !dict.ContainsKey(key))
                        return null;

                    current = dict[key];
                }

                if (!(current is List<object> list) || list.Count == 0)
                    return null;

                var values = new string[list.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = list[i]?.ToString();
                }
                
                return values;
            }            

            public void SetValue(string value, params string[] keyPath)
            {
                Dictionary<string, object> node = Properties;
                for (int i = 0; i < keyPath.Length - 1; i++)
                {
                    if (!node.ContainsKey(keyPath[i]) || !(node[keyPath[i]] is Dictionary<string, object>))
                    {
                        node[keyPath[i]] = new Dictionary<string, object>();
                    }
                    node = (Dictionary<string, object>)node[keyPath[i]];
                }

                node[keyPath[keyPath.Length - 1]] = value;
            }

            private static Dictionary<string, string> GetValueDictionaryFromResource(Dictionary<string, object> node, params string[] keyPath)
            {
                object current = node;
                foreach (var key in keyPath)
                {
                    if (!(current is Dictionary<string, object> dict) || !dict.ContainsKey(key))
                        return null;

                    current = dict[key];
                }

                if (!(current is Dictionary<string, object> targetDict) || targetDict.Count == 0)
                    return null;

                var dictionary = new Dictionary<string, string>(targetDict.Count);
                foreach (var kvp in targetDict)
                {
                    dictionary[kvp.Key] = kvp.Value?.ToString();
                }

                return dictionary;
            }
        }
    }
}
