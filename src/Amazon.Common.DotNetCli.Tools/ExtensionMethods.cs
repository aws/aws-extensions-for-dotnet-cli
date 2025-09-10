using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Amazon.Common.DotNetCli.Tools
{
    public static class ExtensionMethods
    {
        public static void SetIfNotNull(this Dictionary<string, object> data, string key, string value)
        {
            if (value == null)
                return;

            data[key] = value;
        }
        public static void SetFilePathIfNotNull(this Dictionary<string, object> data, string key, string filepath, string rootPath)
        {
            if (string.IsNullOrEmpty(filepath))
                return;

            if(Path.IsPathRooted(filepath) && !string.IsNullOrEmpty(rootPath))
            {
                var fileName = Path.GetFileName(filepath);
                var parentDirectory = Directory.GetParent(filepath).FullName;
                filepath = Path.Combine(Utilities.RelativePathTo(rootPath, parentDirectory), fileName);
            }

            data[key] = filepath;
        }

        public static void SetIfNotNull(this Dictionary<string, object> data, string key, bool? value)
        {
            if (!value.HasValue)
                return;

            data[key] = value.Value;
        }
        public static void SetIfNotNull(this Dictionary<string, object> data, string key, int? value)
        {
            if (!value.HasValue)
                return;

            data[key] = value.Value;
        }

        public static object GetJsonValue(this JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        array.Add(GetJsonValue(item));
                    }
                    return array;
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        obj[prop.Name] = GetJsonValue(prop.Value);
                    }
                    return obj;
                default:
                    return null;
            }
        }
    }
}
