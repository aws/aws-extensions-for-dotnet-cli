using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.ElasticBeanstalk.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public class DeleteEnvironmentCommand : EBBaseCommand
    {
        public const string COMMAND_NAME = "delete-environment";
        public const string COMMAND_DESCRIPTION = "Delete an AWS Elastic Beanstalk environment.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,

            EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION,
            EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT
        });

        DeleteEnvironmentProperties _deleteEnvironmentProperties;
        public DeleteEnvironmentProperties DeleteEnvironmentProperties
        {
            get
            {
                if (this._deleteEnvironmentProperties == null)
                {
                    this._deleteEnvironmentProperties = new DeleteEnvironmentProperties();
                }

                return this._deleteEnvironmentProperties;
            }
            set { this._deleteEnvironmentProperties = value; }
        }

        public DeleteEnvironmentCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            this.DeleteEnvironmentProperties.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
        }

        protected override async Task<bool> PerformActionAsync()
        {

            string environment = this.GetStringValueOrDefault(this.DeleteEnvironmentProperties.Environment, EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT, true);
            if (!this.ConfirmDeletion("Elastic Beanstalk environment " + environment))
                return true;

            try
            {
                await this.EBClient.TerminateEnvironmentAsync(new TerminateEnvironmentRequest
                {
                    EnvironmentName = environment
                });
                this.Logger?.WriteLine("Environment {0} deleted", environment);
            }
            catch(Exception e)
            {
                throw new ElasticBeanstalkExceptions(string.Format("Error deleting environment {0}: {1}", environment, e.Message), ElasticBeanstalkExceptions.EBCode.FailedToDeleteEnvironment);
            }

            return true;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.DeleteEnvironmentProperties.PersistSettings(this, data);
        }
    }
}
