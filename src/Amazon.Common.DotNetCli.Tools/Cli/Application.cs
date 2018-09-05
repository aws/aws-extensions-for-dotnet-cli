using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Amazon.Common.DotNetCli.Tools.Commands;
using System.IO;
using System.Linq;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.Common.DotNetCli.Tools.CLi
{
    public class Application
    {
        public string SubCommandName { get; }
        public string ToolDisplayName { get; }
        IList<ICommandInfo> CommandInfos { get; }
        public string ProjectHome { get; }

        public Application(string subCommandName, string toolDisplayName, string projectHome, IList<ICommandInfo> commands)
        {
            this.SubCommandName = subCommandName;
            this.ToolDisplayName = toolDisplayName;
            this.CommandInfos = commands;
            this.ProjectHome = projectHome;
        }

        public int Execute(string[] args)
        {
            try
            {
                PrintToolTitle();

                if (args.Length == 0)
                {
                    PrintUsage();
                    return -1;
                }

                if (IsHelpSwitch(args[0]))
                {
                    if (args.Length > 1)
                        PrintUsage(args[1]);
                    else
                        PrintUsage();

                    return 0;
                }
                else if(args.Length > 1 && IsHelpSwitch(args[1]))
                {
                    PrintUsage(args[0]);
                    return 0;
                }

                var commandInfo = FindCommandInfo(args[0]);


                if (commandInfo != null)
                {
                    var command = commandInfo.CreateCommand(new ConsoleToolLogger(), Directory.GetCurrentDirectory(), args.Skip(1).ToArray());
                    var success = command.ExecuteAsync().Result;
                    if (!success)
                    {
                        return -1;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Unknown command: {args[0]}\n");
                    PrintUsage();
                    return -1;
                }
            }
            catch (ToolsException e)
            {
                Console.Error.WriteLine(e.Message);
                return -1;
            }
            catch(TargetInvocationException e) when (e.InnerException is ToolsException)
            {
                Console.Error.WriteLine(e.InnerException.Message);
                return -1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unknown error: {e.Message}");
                Console.Error.WriteLine(e.StackTrace);

                return -1;
            }

            return 0;
        }

        private bool IsHelpSwitch(string value)
        {
            if (value == "--help" || value == "--h" || value == "help")
                return true;

            return false;
        }


        private ICommandInfo FindCommandInfo(string name)
        {
            foreach (var commandInfo in this.CommandInfos)
            {
                if (commandInfo == null)
                    continue;

                if (string.Equals(commandInfo.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return commandInfo;
                }
            }

            return null;
        }

        private void PrintToolTitle()
        {
            var sb = new StringBuilder(this.ToolDisplayName);
            var version = Utilities.DetermineToolVersion();
            if (!string.IsNullOrEmpty(version))
            {
                sb.Append($" ({version})");
            }
            Console.WriteLine(sb.ToString());
            Console.WriteLine($"Project Home: {this.ProjectHome}");
            Console.WriteLine("\t");
        }

        private void PrintUsage()
        {

            const int NAME_WIDTH = 23;
            Console.WriteLine("\t");
            foreach(var command in this.CommandInfos)
            {
                if (command == null)
                {
                    Console.WriteLine("\t");
                }
                else if (command is GroupHeaderInfo)
                {
                    Console.WriteLine("\t");
                    Console.WriteLine(command.Name);
                    Console.WriteLine("\t");
                }
                else
                {
                    Console.WriteLine($"\t{command.Name.PadRight(NAME_WIDTH)} {command.Description}");
                }
            }

            Console.WriteLine("\t");
            Console.WriteLine("To get help on individual commands execute:");
            Console.WriteLine($"\tdotnet {this.SubCommandName} help <command>");
            Console.WriteLine("\t");
        }


        private void PrintUsage(string command)
        {
            var commandInfo = FindCommandInfo(command);
            if (commandInfo != null)
                PrintUsage(commandInfo.Name, commandInfo.Description, commandInfo.CommandOptions, commandInfo.Argument);
            else
            {
                Console.Error.WriteLine($"Unknown command {command}");
                PrintUsage();
            }
        }

        private void PrintUsage(string command, string description, IList<CommandOption> options, string argument)
        {
            const int INDENT = 3;

            Console.WriteLine($"{command}: ");
            Console.WriteLine($"{new string(' ', INDENT)}{description}");
            Console.WriteLine("\t");


            if (!string.IsNullOrEmpty(argument))
            {
                Console.WriteLine($"{new string(' ', INDENT)}dotnet {this.SubCommandName} {command} [arguments] [options]");
                Console.WriteLine($"{new string(' ', INDENT)}Arguments:");
                Console.WriteLine($"{new string(' ', INDENT * 2)}{argument}");
            }
            else
            {
                Console.WriteLine($"{new string(' ', INDENT)}dotnet  {this.SubCommandName} {command} [options]");
            }

            const int SWITCH_COLUMN_WIDTH = 40;

            Console.WriteLine($"{new string(' ', INDENT)}Options:");
            foreach (var option in options)
            {
                StringBuilder sb = new StringBuilder();
                if (option.ShortSwitch != null)
                    sb.Append($"{option.ShortSwitch.PadRight(6)} | ");

                sb.Append($"{option.Switch}");
                if (sb.Length < SWITCH_COLUMN_WIDTH)
                    sb.Append(new string(' ', SWITCH_COLUMN_WIDTH - sb.Length));

                sb.Append(option.Description);

                Console.WriteLine($"{new string(' ', INDENT * 2)}{sb.ToString()}");
            }

        }
    }
}
