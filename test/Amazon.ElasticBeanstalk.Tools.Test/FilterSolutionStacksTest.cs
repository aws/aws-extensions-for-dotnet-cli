using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

using Amazon.ElasticBeanstalk.Tools.Commands;

namespace Amazon.ElasticBeanstalk.Tools.Test
{
    public class FilterSolutionStacksTest
    {
        [Fact]
        public void EnsureOnlyLatestVersionsAreReturnInputInDecendingOrder()
        {
            var availableSolutionStacks = new List<string>
            {
                "64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5",
                "64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5",
                "64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5",
                "64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)",
                "64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)"
            };

            var filterSolutionStacks = EBBaseCommand.FilterSolutionStackToLatestVersion(availableSolutionStacks);

            Assert.Equal(3, filterSolutionStacks.Count);
            Assert.Contains("64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5", filterSolutionStacks);
            Assert.Contains("64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5", filterSolutionStacks);
            Assert.Contains("64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)", filterSolutionStacks);
            Assert.DoesNotContain("64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)", filterSolutionStacks);
        }

        [Fact]
        public void EnsureOnlyLatestVersionsAreReturnInputInAscendingOrder()
        {
            var availableSolutionStacks = new List<string>
            {
                "64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5",
                "64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5",
                "64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5",
                "64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)",
                "64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)"
            };

            var filterSolutionStacks = EBBaseCommand.FilterSolutionStackToLatestVersion(availableSolutionStacks);

            Assert.Equal(3, filterSolutionStacks.Count);
            Assert.Contains("64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5", filterSolutionStacks);
            Assert.Contains("64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5", filterSolutionStacks);
            Assert.Contains("64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)", filterSolutionStacks);
            Assert.DoesNotContain("64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)", filterSolutionStacks);
        }

        [Fact]
        public void SolutionStackWithNoVersionIsPresent()
        {
            var availableSolutionStacks = new List<string>
            {
                "64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5",
                "64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5",
                "64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5",
                "Special versionless solution",
                "64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5",
                "64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)",
                "64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)"
            };

            var filterSolutionStacks = EBBaseCommand.FilterSolutionStackToLatestVersion(availableSolutionStacks);

            Assert.Equal(4, filterSolutionStacks.Count);
            Assert.Contains("Special versionless solution", filterSolutionStacks);

            Assert.Contains("64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5", filterSolutionStacks);
            Assert.Contains("64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5", filterSolutionStacks);
            Assert.Contains("64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)", filterSolutionStacks);
            Assert.DoesNotContain("64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)", filterSolutionStacks);
        }

        [Fact]
        public void SolutionStackWithInvalidVersionIsPresent()
        {
            var availableSolutionStacks = new List<string>
            {
                "64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5",
                "64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5",
                "64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5",
                "64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5",
                "Special versioning v1.Left.Right solution",
                "64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5",
                "64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)",
                "64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)"
            };

            var filterSolutionStacks = EBBaseCommand.FilterSolutionStackToLatestVersion(availableSolutionStacks);

            Assert.Equal(4, filterSolutionStacks.Count);
            Assert.Contains("Special versioning v1.Left.Right solution", filterSolutionStacks);

            Assert.Contains("64bit Windows Server 2012 R2 v2.5.0 running IIS 8.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2012 R2 v2.4.0 running IIS 8.5", filterSolutionStacks);
            Assert.Contains("64bit Windows Server 2008 R2 v1.2.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.1.0 running IIS 7.5", filterSolutionStacks);
            Assert.DoesNotContain("64bit Windows Server 2008 R2 v1.0.0 running IIS 7.5", filterSolutionStacks);
            Assert.Contains("64bit Amazon Linux 2 v0.1.1 running Corretto 11 (BETA)", filterSolutionStacks);
            Assert.DoesNotContain("64bit Amazon Linux 2 v0.1.0 running Corretto 11 (BETA)", filterSolutionStacks);
        }
    }
}
