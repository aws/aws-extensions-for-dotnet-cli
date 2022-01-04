using System.Globalization;
using System.Text;

namespace Amazon.ElasticBeanstalk.Tools
{
    public class PublishOptions
    {
        private readonly string _initialOptions;
        private readonly bool _isWindowsEnvironment;
        private readonly bool _selfContained;

        public PublishOptions(string initialOptions, bool isWindowsEnvironment, bool selfContained)
        {
            _initialOptions = initialOptions ?? "";
            _isWindowsEnvironment = isWindowsEnvironment;
            _selfContained = selfContained;
        }

        public string ToCliString()
        {
            var publishOptionsBuilder = new StringBuilder(_initialOptions);

            if (DoesNotContainRuntime())
            {
                publishOptionsBuilder.Append($" --runtime {GetRuntimeString()}");
            } 

            if (DoesNotContainSelfContained())
            {
                publishOptionsBuilder.Append($" --self-contained {ConvertBoolToString(_selfContained)}");
            }

            return publishOptionsBuilder.ToString();
        }

        private bool DoesNotContainRuntime() => !_initialOptions.Contains("-r ") && !_initialOptions.Contains("--runtime ");

        private string GetRuntimeString() => _isWindowsEnvironment ? "win-x64" : "linux-x64";

        private bool DoesNotContainSelfContained() => !_initialOptions.Contains("--self-contained");

        private string ConvertBoolToString(bool boolean) => boolean.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
    }
}
