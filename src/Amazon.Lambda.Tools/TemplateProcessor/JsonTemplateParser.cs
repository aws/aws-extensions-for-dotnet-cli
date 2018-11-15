using System;
using System.Collections.Generic;
using System.Text;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// JSON implementation of ITemplateParser
    /// </summary>
    public class JsonTemplateParser : ITemplateParser
    {
        JsonData Root { get; }
        public JsonTemplateParser(string templateBody)
        {
            try
            {
                this.Root = JsonMapper.ToObject(templateBody);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error parsing CloudFormation template: {e.Message}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateParseError, e);
            }
        }

        public string GetUpdatedTemplate()
        {
            var json = JsonMapper.ToJson(this.Root);
            return json;
        }

        public IEnumerable<IUpdatableResource> UpdatableResources()
        {
            var resources = this.Root["Resources"];
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
                UpdatableResourceDefinition updatableResourceDefinition;
                if (!UpdatableResourceDefinition.ValidUpdatableResourceDefinitions.TryGetValue(type,
                    out updatableResourceDefinition))
                    continue;
                
                var updatableResource = new UpdatableResource(field, updatableResourceDefinition, new JsonUpdatableResourceDataSource(this.Root, properties));
                yield return updatableResource;
            }
        }

        /// <summary>
        /// The JSON implementation of IUpdatableResourceDataSource
        /// </summary>
        public class JsonUpdatableResourceDataSource : IUpdatableResourceDataSource
        {
            JsonData Root { get; }
            JsonData Properties { get; }

            public JsonUpdatableResourceDataSource(JsonData root, JsonData properties)
            {
                this.Root = root;
                this.Properties = properties;
            }

            public string GetValueFromRoot(params string[] keyPath)
            {
                return GetValue(this.Root, keyPath);
            }

            public string GetValue(params string[] keyPath)
            {
                return GetValue(this.Properties, keyPath);
            }

            private string GetValue(JsonData node, params string[] keyPath)
            {
                foreach (var key in keyPath)
                {
                    if (node == null)
                        return null;

                    node = node[key];
                }

                return node?.ToString();
            }

            public void SetValue(string value, params string[] keyPath)
            {
                JsonData node = Properties;
                for (int i = 0; i < keyPath.Length - 1; i++)
                {
                    var childNode = node[keyPath[i]];
                    if (childNode == null)
                    {
                        childNode = new JsonData();
                        node[keyPath[i]] = childNode;
                    }
                    node = childNode;
                }

                node[keyPath[keyPath.Length - 1]] = value;
            }
        }
    }
}
