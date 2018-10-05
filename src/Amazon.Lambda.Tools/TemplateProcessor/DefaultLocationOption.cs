namespace Amazon.Lambda.Tools.TemplateProcessor
{
    public class DefaultLocationOption
    {
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string MSBuildParameters { get; set; }
        public bool DisableVersionCheck { get; set; }
        public string Package { get; set; }
    }
}