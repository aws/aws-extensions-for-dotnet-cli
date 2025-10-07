using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Amazon.Common.DotNetCli.Tools
{
    public abstract class DefaultConfigFile
    {
        JsonElement _rootData;

        public DefaultConfigFile()
            : this(new JsonElement(), string.Empty)
        {
        }

        public DefaultConfigFile(string sourceFile)
            : this(new JsonElement(), sourceFile)
        {
        }

        public DefaultConfigFile(JsonElement data, string sourceFile)
        {
            this._rootData = data;
            this.SourceFile = sourceFile;
        }

        /// <summary>
        /// The file the default values were read from.
        /// </summary>
        public string SourceFile
        {
            get;
            private set;
        }

        public abstract string DefaultConfigFileName { get; }


        public void LoadDefaults(string projectLocation, string configFile)
        {
            string path = Path.Combine(projectLocation, configFile);
            this.SourceFile = path;

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    this._rootData = doc.RootElement.Clone();
                }
                this.SourceFile = path;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error parsing default config {path}: {e.Message}", ToolsException.CommonErrorCode.DefaultsParseFail, e);
            }
        }

        /// <summary>
        /// Gets the default value for the CommandOption with the CommandOption's switch string.
        /// </summary>
        /// <param name="fullSwitchName"></param>
        /// <returns></returns>
        public object this[string fullSwitchName]
        {
            get
            {
                if (fullSwitchName.StartsWith("--"))
                    fullSwitchName = fullSwitchName.Substring(2);

                if (_rootData.ValueKind == JsonValueKind.Undefined || !_rootData.TryGetProperty(fullSwitchName, out JsonElement element))
                    return null;

                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        return element.GetInt32();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return element.GetBoolean();
                    case JsonValueKind.Array:
                        var items = new string[element.GetArrayLength()];
                        int index = 0;
                        foreach (JsonElement item in element.EnumerateArray())
                        {
                            items[index++] = item.ToString();
                        }
                        return items;
                    case JsonValueKind.Object:
                        var obj = new Dictionary<string, string>();
                        foreach (JsonProperty prop in element.EnumerateObject())
                        {
                            obj[prop.Name] = prop.Value.ToString();
                        }
                        return obj;
                    default:
                        return null;
                }
            }
        }

        protected JsonElement GetValue(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            if (_rootData.ValueKind == JsonValueKind.Undefined || !_rootData.TryGetProperty(key, out JsonElement element))
                return new JsonElement();
            return element;
        }

        /// <summary>
        /// Gets the default if it exists as a string. This is used for display purpose.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetValueAsString(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            if (_rootData.ValueKind == JsonValueKind.Undefined || !_rootData.TryGetProperty(key, out JsonElement element))
                return null;

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean().ToString();
                case JsonValueKind.Number:
                    return element.GetInt32().ToString();
                default:
                    return null;
            }
        }


        public static string FormatCommaDelimitedList(string[] values)
        {
            return values == null ? null : string.Join(",", values);
        }

        public static string FormatKeyValue(IDictionary<string, string> values)
        {
            if (values == null)
                return null;

            StringBuilder sb = new StringBuilder();

            foreach (var kvp in values)
            {
                if (sb.Length > 0)
                    sb.Append(";");

                sb.Append($"\"{kvp.Key}\"=\"{kvp.Value}\"");
            }

            return sb.ToString();
        }

        public string GetRawString(string key)
        {
            if (_rootData.ValueKind == JsonValueKind.Undefined || !_rootData.TryGetProperty(key, out JsonElement element) || element.ValueKind != JsonValueKind.String)
                return null;

            return element.GetString();
        }
    }
}
