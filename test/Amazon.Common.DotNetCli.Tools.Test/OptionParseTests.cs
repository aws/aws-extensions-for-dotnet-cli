using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class OptionParseTests
    {
        [Fact]
        public void ParseNullParameter()
        {
            var parameters = Utilities.ParseKeyValueOption(null);
            Assert.Empty(parameters);

            parameters = Utilities.ParseKeyValueOption(string.Empty);
            Assert.Empty(parameters);
        }

        [Fact]
        public void ParseKeyValueParameter()
        {
            var parameters = Utilities.ParseKeyValueOption("Table=Blog");
            Assert.Single(parameters);
            Assert.Equal("Blog", parameters["Table"]);

            parameters = Utilities.ParseKeyValueOption("Table=Blog;");
            Assert.Single(parameters);
            Assert.Equal("Blog", parameters["Table"]);

            parameters = Utilities.ParseKeyValueOption("\"ConnectionString\"=\"User=foo;Password=test\"");
            Assert.Single(parameters);
            Assert.Equal("User=foo;Password=test", parameters["ConnectionString"]);
        }

        [Fact]
        public void ParseTwoKeyValueParameter()
        {
            var parameters = Utilities.ParseKeyValueOption("Table=Blog;Bucket=MyBucket");
            Assert.Equal(2, parameters.Count);

            Assert.Equal("Blog", parameters["Table"]);
            Assert.Equal("MyBucket", parameters["Bucket"]);

            parameters = Utilities.ParseKeyValueOption("\"ConnectionString1\"=\"User=foo;Password=test\";\"ConnectionString2\"=\"Password=test;User=foo\"");
            Assert.Equal(2, parameters.Count);
            Assert.Equal("User=foo;Password=test", parameters["ConnectionString1"]);
            Assert.Equal("Password=test;User=foo", parameters["ConnectionString2"]);
        }

        [Fact]
        public void ParseEmptyValue()
        {
            var parameters = Utilities.ParseKeyValueOption("ShouldCreateTable=true;BlogTableName=");
            Assert.Equal(2, parameters.Count);
            Assert.Equal("true", parameters["ShouldCreateTable"]);
            Assert.Equal("", parameters["BlogTableName"]);

            parameters = Utilities.ParseKeyValueOption("BlogTableName=;ShouldCreateTable=true");
            Assert.Equal(2, parameters.Count);
            Assert.Equal("true", parameters["ShouldCreateTable"]);
            Assert.Equal("", parameters["BlogTableName"]);
        }

        [Fact]
        public void ParseErrors()
        {
            Assert.Throws<ToolsException>(() => Utilities.ParseKeyValueOption("=aaa"));
        }
    }
}
