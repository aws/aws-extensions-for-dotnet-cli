using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.ECS.Tools.Commands
{
    public abstract class ECSBaseDeployCommand : ECSBaseCommand
    {
        public ECSBaseDeployCommand(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        public ECSBaseDeployCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
        {
        }

        PushDockerImageProperties _pushProperties;
        public PushDockerImageProperties PushDockerImageProperties
        {
            get
            {
                if (this._pushProperties == null)
                {
                    this._pushProperties = new PushDockerImageProperties();
                }

                return this._pushProperties;
            }
            set { this._pushProperties = value; }
        }

        TaskDefinitionProperties _taskDefinitionProperties;
        public TaskDefinitionProperties TaskDefinitionProperties
        {
            get
            {
                if (this._taskDefinitionProperties == null)
                {
                    this._taskDefinitionProperties = new TaskDefinitionProperties();
                }

                return this._taskDefinitionProperties;
            }
            set { this._taskDefinitionProperties = value; }
        }

        ClusterProperties _clusterProperties;
        public ClusterProperties ClusterProperties
        {
            get
            {
                if (this._clusterProperties == null)
                {
                    this._clusterProperties = new ClusterProperties();
                }

                return this._clusterProperties;
            }
            set { this._clusterProperties = value; }
        }

    }
}
