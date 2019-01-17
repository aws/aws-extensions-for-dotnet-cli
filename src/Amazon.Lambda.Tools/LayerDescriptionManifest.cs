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
            this.Type = type;
        }

        public enum ManifestType { RuntimePackageStore=1 }
        public enum OptimizedState { NoOptimized=0, Optimized=1}

        public ManifestType Type { get; set; }
        public string Dir { get; set; }
        public OptimizedState Op { get; set; }

        public string Buc { get; set; }
        public string Key { get; set; }
    }
}
