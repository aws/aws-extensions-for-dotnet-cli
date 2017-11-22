using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Amazon.Common.DotNetCli.Tools.Commands
{

    public interface ICommandInfo
    {
        string Name { get; }
        string Description { get; }
        string Argument { get; }
        IList<CommandOption> CommandOptions { get; }
        ICommand CreateCommand(IToolLogger logger, string workingDirectory, string[] args);
    };

    public class GroupHeaderInfo : ICommandInfo
    {
        public GroupHeaderInfo(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public string Description { get; }

        public string Argument { get; }

        public IList<CommandOption> CommandOptions { get; }

        public ICommand CreateCommand(IToolLogger logger, string workingDirectory, string[] args)
        {
            return null;
        }
    }

    public class CommandInfo<T> : ICommandInfo
        where T : class, ICommand
    {
        public CommandInfo(string name, string description, IList<CommandOption> commandOptions)
        {
            this.Name = name;
            this.Description = description;
            this.CommandOptions = commandOptions;
        }
        public CommandInfo(string name, string description, IList<CommandOption> commandOptions, string argument)
            : this(name, description, commandOptions)
        {
            this.Argument = argument;
        }

        public string Name { get; }
        public string Description { get; }
        public string Argument { get; }
        public IList<CommandOption> CommandOptions { get; }

        public ICommand CreateCommand(IToolLogger logger, string workingDirectory, string[] args)
        {
            var typeClient = typeof(T);
            var constructor = typeClient.GetConstructor(new Type[] { typeof(IToolLogger), typeof(string), typeof(string[]) });
            if (constructor == null)
                throw new Exception($"Command Type {typeClient.FullName} is missing constructor");

            return constructor.Invoke(new object[] { logger, workingDirectory, args }) as T;
        }
    }
}
