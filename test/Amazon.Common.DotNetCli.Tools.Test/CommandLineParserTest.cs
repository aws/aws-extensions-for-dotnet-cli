using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class CommandLineParserTest
    {
        [Fact]
        public void SingleStringArgument()
        {
            Action<CommandOptions> validation = x =>
            {
                Assert.Equal(1, x.Count);

                var option = x.FindCommandOption("-c");
                Assert.Equal("Release", option.Item2.StringValue);
            };

            var options = new List<CommandOption>
            {
                new CommandOption
                {
                    Name = "Configuration",
                    ValueType = CommandOption.CommandOptionValueType.StringValue,
                    ShortSwitch = "-c",
                    Switch = "--configuration"
                }

            };

            var values = CommandLineParser.ParseArguments(options, new string[] { "-c", "Release" });
            validation(values);

            values = CommandLineParser.ParseArguments(options, new string[] { "--configuration", "Release" });
            validation(values);
        }

        [Fact]
        public void SingleIntArgument()
        {
            Action<CommandOptions> validation = x =>
            {
                Assert.Equal(1, x.Count);

                var option = x.FindCommandOption("-i");
                Assert.Equal(100, option.Item2.IntValue);
            };

            var options = new List<CommandOption>
            {
                new CommandOption
                {
                    Name = "MyInt",
                    ValueType = CommandOption.CommandOptionValueType.IntValue,
                    ShortSwitch = "-i",
                    Switch = "--integer"
                }
            };

            var values = CommandLineParser.ParseArguments(options, new string[] { "-i", "100" });
            validation(values);

            values = CommandLineParser.ParseArguments(options, new string[] { "--integer", "100" });
            validation(values);
        }

        [Fact]
        public void SingleBoolArgument()
        {
            Action<CommandOptions> validation = x =>
            {
                Assert.Equal(1, x.Count);

                var option = x.FindCommandOption("-b");
                Assert.Equal(true, option.Item2.BoolValue);
            };

            var options = new List<CommandOption>
            {
                new CommandOption
                {
                    Name = "MyBool",
                    ValueType = CommandOption.CommandOptionValueType.BoolValue,
                    ShortSwitch = "-b",
                    Switch = "--bool"
                }
            };

            var values = CommandLineParser.ParseArguments(options, new string[] { "-b", "true" });
            validation(values);

            values = CommandLineParser.ParseArguments(options, new string[] { "--bool", "true" });
            validation(values);
        }
    }
}
