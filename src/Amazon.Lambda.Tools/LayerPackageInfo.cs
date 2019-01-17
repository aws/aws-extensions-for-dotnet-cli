using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Lambda.Tools
{
    /// <summary>
    /// This class contains the runtime package store information from a collection of Lambda layers.
    /// </summary>
    public class LayerPackageInfo
    {
        public IList<LayerPackageInfoItem> Items { get; } = new List<LayerPackageInfoItem>();

        /// <summary>
        /// Generates the value that must be set for the DOTNET_SHARED_STORE environment variable.
        /// </summary>
        /// <returns></returns>
        public string GenerateDotnetSharedStoreValue()
        {
            var sb = new StringBuilder();

            foreach(var item in Items)
            {
                if (sb.Length > 0)
                    sb.Append(":");

                sb.Append($"/opt/{item.Directory}/");
            }

            return sb.ToString();
        }

        public class LayerPackageInfoItem
        {
            /// <summary>
            /// The directory under /opt folder in the Lambda environment that the store will be placed/
            /// </summary>
            public string Directory { get; set; }

            /// <summary>
            /// Local file path the artifact.xml file that list the NuGet packages in the Layer.
            /// </summary>
            public string ManifestPath { get; set; }
        }
    }
}
