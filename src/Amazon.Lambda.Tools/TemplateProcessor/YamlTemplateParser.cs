using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// JSON implementation of ITemplateParser
    /// </summary>
    public class YamlTemplateParser : ITemplateParser
    {
        YamlStream Yaml { get; }
        public YamlTemplateParser(string templateBody)
        {
            var input = new StringReader(templateBody);

            // Load the stream
            this.Yaml = new YamlStream();
            this.Yaml.Load(input);
        }

        public string GetUpdatedTemplate()
        {
            var myText = new StringWriter();
            this.Yaml.Save(myText, assignAnchors: false);

            return myText.ToString();
        }

        public IEnumerable<IUpdatableResource> UpdatableResources()
        {
            var root = (YamlMappingNode)this.Yaml.Documents[0].RootNode;

            if (root == null)
                throw new LambdaToolsException("CloudFormation template does not define any AWS resources", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingResourceSection);

            var resourcesKey = new YamlScalarNode("Resources");

            if (!root.Children.ContainsKey(resourcesKey))
                throw new LambdaToolsException("CloudFormation template does not define any AWS resources", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingResourceSection);

            var resources = (YamlMappingNode)root.Children[resourcesKey];

            foreach (var resource in resources.Children)
            {
                var resourceBody = (YamlMappingNode)resource.Value;
                var type = (YamlScalarNode)resourceBody.Children[new YamlScalarNode("Type")];
                var properties = (YamlMappingNode)resourceBody.Children[new YamlScalarNode("Properties")];

                if (properties == null) continue;
                if (type == null) continue;

                
                UpdatableResourceDefinition updatableResourceDefinition;
                if (!UpdatableResourceDefinition.ValidUpdatableResourceDefinitions.TryGetValue(type.Value,
                    out updatableResourceDefinition))
                    continue;
                
                var updatableResource = new UpdatableResource(resource.Key.ToString(), updatableResourceDefinition, new YamlUpdatableResourceDataSource(root, properties));
                
                yield return updatableResource;
            }

        }

        /// <summary>
        /// The JSON implementation of IUpdatableResourceDataSource
        /// </summary>
        public class YamlUpdatableResourceDataSource : IUpdatableResourceDataSource
        {
            YamlMappingNode Root { get; }
            YamlMappingNode Properties { get; }

            public YamlUpdatableResourceDataSource(YamlMappingNode root, YamlMappingNode properties)
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

            private string GetValue(YamlNode node, params string[] keyPath)
            {
                foreach (var key in keyPath)
                {
                    if (node == null || !(node is YamlMappingNode))
                        return null;

                    var mappingNode = ((YamlMappingNode)node);
                    if (!mappingNode.Children.ContainsKey(key))
                        return null;

                    node = mappingNode.Children[key];
                }

                if (node is YamlScalarNode)
                {
                    return ((YamlScalarNode)node).Value;
                }

                return null;
            }

            public void SetValue(string value, params string[] keyPath)
            {
                YamlMappingNode node = this.Properties;
                for (int i = 0; i < keyPath.Length - 1; i++)
                {
                    var childNode = node.Children.ContainsKey(keyPath[i]) ? node[keyPath[i]] as YamlMappingNode : null;
                    if (childNode == null)
                    {
                        childNode = new YamlMappingNode();
                        ((YamlMappingNode)node).Children.Add(keyPath[i], childNode);
                    }
                    node = childNode;
                }

                node.Children.Remove(keyPath[keyPath.Length - 1]);
                node.Children.Add(keyPath[keyPath.Length - 1], new YamlScalarNode(value));
            }
        }
    }
}
