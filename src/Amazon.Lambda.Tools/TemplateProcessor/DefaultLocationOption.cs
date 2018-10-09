namespace Amazon.Lambda.Tools.TemplateProcessor
{
    /// <summary>
    /// Properties to use when building the current project which might have come from the command line.
    /// </summary>
    public class DefaultLocationOption
    {
        /// <summary>
        /// Build Configuration e.g. Release
        /// </summary>
        /// 
        public string Configuration { get; set; }
        /// <summary>
        /// .NET Target Framework e.g. netcoreapp2.1
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// Additional parameters to pass when invoking dotnet publish
        /// </summary>
        public string MSBuildParameters { get; set; }
        
        /// <summary>
        /// If true disables checking for correct version of Microsoft.AspNetCore.App.
        /// </summary>
        public bool DisableVersionCheck { get; set; }
        
        /// <summary>
        /// If the current project is already built pass in the current compiled package zip file.
        /// </summary>
        public string Package { get; set; }
    }
}