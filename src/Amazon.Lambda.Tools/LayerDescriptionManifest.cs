using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Lambda.Tools
{
    /// <summary>
    /// Is class is serialized as the description of the layer. Space in the description is limited
    /// so property names are kept short. Enum are also stored as numbers.
    /// </summary>
    public class LayerDescriptionManifest
    {
        public LayerDescriptionManifest()
        {

        }

        public LayerDescriptionManifest(ManifestType type)
        {
            this.Nlt = type;
        }

        public enum ManifestType { RuntimePackageStore=1 }
        public enum OptimizedState { NoOptimized=0, Optimized=1}

        /// <summary>
        /// .NET Layer Type
        /// </summary>
        public ManifestType Nlt { get; set; }

        /// <summary>
        /// The sub directory in the packages zip file that NuGet packages will be placed. This 
        /// will be the sub folder under the /opt directory in the Lambda environment.
        /// </summary>
        public string Dir { get; set; }

        /// <summary>
        /// Indicates whether the packages were pre-jitted when the store was created.
        /// </summary>
        public OptimizedState Op { get; set; }

        /// <summary>
        /// The S3 bucket containing the artifact.xml file.
        /// </summary>
        public string Buc { get; set; }

        /// <summary>
        /// THe S3 object key for the artifact.xml file.
        /// </summary>
        public string Key { get; set; }
    }
}
