using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdParty.Json.LitJson;

namespace Amazon.Common.DotNetCli.Tools
{
    public abstract class DefaultConfigFile
    {
        JsonData _rootData;

        public DefaultConfigFile()
    : this(new JsonData(), string.Empty)
        {
        }

        public DefaultConfigFile(string sourceFile)
            : this(new JsonData(), sourceFile)
        {
        }

        public DefaultConfigFile(JsonData data, string sourceFile)
        {
            this._rootData = data ?? new JsonData();
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

            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                try
                {
                    this._rootData = JsonMapper.ToObject(reader) as JsonData;
                    this.SourceFile = path;
                }
                catch (Exception e)
                {
                    throw new ToolsException($"Error parsing default config {path}: {e.Message}", ToolsException.CommonErrorCode.DefaultsParseFail, e);
                }
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

                if (this._rootData[fullSwitchName] == null)
                    return null;

                if (this._rootData[fullSwitchName].IsString)
                    return this._rootData[fullSwitchName].ToString();
                if (this._rootData[fullSwitchName].IsInt)
                    return (int)this._rootData[fullSwitchName];
                if (this._rootData[fullSwitchName].IsBoolean)
                    return (bool)this._rootData[fullSwitchName];
                if (this._rootData[fullSwitchName].IsArray)
                {
                    var items = new string[this._rootData[fullSwitchName].Count];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = this._rootData[fullSwitchName][i].ToString();
                    }
                    return items;
                }
                if (this._rootData[fullSwitchName].IsObject)
                {
                    var obj = new Dictionary<string, string>();
                    foreach (var key in this._rootData[fullSwitchName].PropertyNames)
                    {
                        obj[key] = this._rootData[key]?.ToString();
                    }
                    return obj;
                }

                return null;
            }
        }

        protected JsonData GetValue(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            return this._rootData[key];
        }

        /// <summary>
        /// Gets the default if it exists as a string. This is used for display purpose.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetValueAsString(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            var data = this._rootData[key];
            if (data == null)
                return null;

            if (data.IsString)
                return data.ToString();
            else if (data.IsBoolean)
                return ((bool)data).ToString();
            else if (data.IsInt)
                return ((int)data).ToString();

            return null;
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
            var data = this._rootData[key];
            if (data == null || !data.IsString)
                return null;

            return data.ToString();
        }
    }
}
