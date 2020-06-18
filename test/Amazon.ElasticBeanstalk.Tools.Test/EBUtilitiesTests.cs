using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

using Amazon.ElasticBeanstalk.Tools.Commands;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.ElasticBeanstalk.Tools.Test
{
    public class EBUtilitiesTests
    {
        [Fact]
        public void FindExistingValueNullCollection()
        {
            List<ConfigurationOptionSetting> settings = null;
            Assert.Null(settings.FindExistingValue("ns", "name"));
        }

        [Theory]
        [InlineData("ns1", "name1", "found")]
        [InlineData("ns1", "name", null)]
        [InlineData("ns", "name1", null)]
        [InlineData("ns", "name", null)]
        public void FindExistingValues(string searchNS, string searchName, string expectedValue)
        {
            var ns = "ns1";
            var name = "name1";
            List<ConfigurationOptionSetting> settings = new List<ConfigurationOptionSetting>() { new ConfigurationOptionSetting {Namespace = ns, OptionName = name, Value = expectedValue } };

            var actualValue = settings.FindExistingValue(searchNS, searchName);
            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("./Resources/dotnet31-dependent-runtimeconfig.json", false)]
        [InlineData("./Resources/self-contained-example-runtimeconfig.json", true)]
        public void IsSelfContainedPublishTest(string filename, bool isSelfContained)
        {
            Assert.Equal(EBUtilities.IsSelfContainedPublish(filename), isSelfContained);
        }
    }
}
