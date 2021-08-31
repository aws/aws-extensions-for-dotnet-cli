using System.Linq;
using Microsoft.Build.Locator;
using static Microsoft.Build.Locator.DiscoveryType;

namespace Amazon.Common.DotNetCli.Tools
{
    public static class MSBuildInitializer
    {
        /// <summary>Binds to the latest SDK-owned MSBuild assembly.</summary>
        /// <remarks>It must be run before any MSBuild code is JITted.</remarks>
        public static void Initialize()
        {
            var vsOpts = new VisualStudioInstanceQueryOptions
            {
                DiscoveryTypes = DotNetSdk
            };
            var instance = MSBuildLocator.QueryVisualStudioInstances(vsOpts).First();
            MSBuildLocator.RegisterInstance(instance);
        }
    }
}