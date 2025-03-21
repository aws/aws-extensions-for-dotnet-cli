# AWS Extensions for .NET CLI

This repository contains AWS tool extensions to the .NET CLI. These tool extensions are focused on building 
.NET Core and ASP.NET Core applications and deploying them to AWS services. Many of these deployment 
commands are the same commands the [AWS Toolkit for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2017)
uses to perform its deployment features. This allows you to do initial deployment in Visual Studio
and then easily transition from Visual Studio to the command line and automate the deployment.

For example with the AWS Lambda .NET CLI tool extension configured you can deploy a Lambda function from the 
command line in the Lambda function's project root directory.

```
dotnet lambda deploy-function MyFunction
```

The extension will prompt you for missing required parameters. To disable the extension from prompting, set the 
command line switch **--disable-interactive** to **true**.


For a history of releases view the [release change log](RELEASE.CHANGELOG.md)



## Installing Extensions

As of September 10th, 2018 these extensions have migrated to be .NET Core [Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).
As part of the migration each of these tools version number was set to 3.0.0.0

To install these tools use the **dotnet tool install** command.
```
dotnet tool install -g Amazon.Lambda.Tools
```

To update to the latest version of one of these tools use the **dotnet tool update** command.
```
dotnet tool update -g Amazon.Lambda.Tools
```

### Migrating from DotNetCliToolReference

To migrate an existing project away from the older project tool, you need to edit your project file and remove the **DotNetCliToolReference** for the tool package. For example, let's look at an existing Lambda project file.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>

    <-- The new property indicating to AWS Toolkit for Visual Studio this is a Lambda project -->
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  
  <ItemGroup>
    <-- This line needs to be removed -->
    <DotNetCliToolReference Include="Amazon.Lambda.Tools" Version="2.2.0" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="1.0.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.Json" Version="1.3.0" />
  </ItemGroup>
</Project>
```
To migrate this project, you need to delete the **DotNetCliToolReference** element, including **Amazon.Lambda.Tools**. If you don't remove this line, the older project tool version of **Amazon.Lambda.Tools** will be used instead of an installed Global Tool.

The AWS Toolkit for Visual Studio before .NET Core 2.1 would look for the presence of **Amazon.Lambda.Tools** in the project file to determine whether to show the Lambda deployment menu item. Because we knew we were going to switch to Global Tools, and the reference to **Amazon.Lambda.Tools** in the project was going away, we added the **AWSProjectType** property to the project file. The current version of the AWS Toolkit for Visual Studio now looks for either the presence of **Amazon.Lambda.Tools** or the **AWSProjectType** set to **Lambda**. Make sure when removing the **DotNetCliToolReference** that your project file has the **AWSProjectType** property to continue deploying with the AWS Toolkit for Visual Studio.

## Supported AWS Services

The following AWS services each have their own .NET CLI tool extension to make it easy to deploy a .NET Core Application
to them.

* [Amazon Elastic Container Service](#amazon-elastic-container-service-amazonecstools)
* [AWS Elastic Beanstalk](#aws-elastic-beanstalk-amazonelasticbeanstalktools)
* [AWS Lambda](#aws-lambda-amazonlambdatools)


## Defaults File

Each tool extension supports a defaults JSON file that is used to preset values for all of the command line switches.
When a command is executed it will look for values for the command line switches in this file if they 
are not specified on the command line. The file is a JSON document where each property name matches the full 
command line switch excluding the -- prefix. 

To avoid confusing missing properties from different tool extensions, each tool extension looks for a different named
file in the root of the project.


| Tool Extension | Defaults File Name |
| -------------- | ---------------|
| Amazon.ECS.Tools | aws-ecs-tools-defaults.json |
| Amazon.ElasticBeanstalk.Tools | aws-beanstalk-tools-defaults.json |
| Amazon.Lambda.Tools | aws-lambda-tools-defaults.json |

When deploying with the AWS Toolkit for Visual Studio, you can choose to have the deployment wizard save
chosen values into the defaults file. This makes it easy to switch to the command line.

For example, the following **aws-ecs-tools-defaults.json** has values for the AWS region, 
AWS credential profile and build configuration. If you use it with an ECS command, you will
not need to enter those values.

```json
{
    "region": "us-west-2",
    "profile": "default",
    "configuration": "Release"
}
```

Use the **--config-file** switch to use an alternative file. Set the **--persist-config-file** switch 
is set to true to persist all of its settings in the defaults file.



### Amazon Elastic Container Service ([Amazon.ECS.Tools](https://www.nuget.org/packages/Amazon.ECS.Tools/))
---

This tool extension takes care of building a Docker image from a .NET application and then deploying 
the Docker image to Amazon Elastic Container Service (**ECS**). The application must contain a **dockerfile** 
instructing this tool and the Docker CLI which this tool uses to build the Docker image.

You must install Docker before using this extension to deploy your application.

#### Install

To install the extension run the following command.

```
dotnet tool install -g Amazon.ECS.Tools
```


#### Available Commands


##### Deploy Service

```
dotnet ecs deploy-service ...
```

Deploys the .NET Core application as service on an ECS cluster. Services are for long lived process like
web applications. Services have a desired number of tasks that will run the application. If a task instance 
dies for whatever reason the service will spawn a new task instance. Services can also be associated with
an Elastic Load Balancer so that each of the tasks in the services will be registered as targets for the load balancer.

For list of supported options, use `dotnet ecs deploy-service --help`:
```
> dotnet ecs deploy-service --help
Amazon EC2 Container Service Tools for .NET Core applications (3.5.6)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

deploy-service: 
   Push the application to ECR and runs the application as a long lived service on the ECS Cluster.

   dotnet  ecs deploy-service [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      -t     | --tag                          Name and optionally a tag in the 'name:tag' format.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
      -sip   | --skip-image-push              Skip building and push an image to Amazon ECR.
      -lt    | --launch-type                  The launch type on which to run tasks. Valid values EC2 | FARGATE.
      -ls    | --launch-subnets               Comma delimited list of subnet ids used when launch type is FARGATE
      -lsg   | --launch-security-groups       Comma delimited list of security group ids used when launch type is FARGATE
      --assign-public-ip                      If true a public IP address is assigned to the task when launch type is FARGATE
      -ec    | --cluster                      Name of the ECS Cluster to run the docker image.
      -cs    | --cluster-service              Name of the service to run on the ECS Cluster.
      -dc    | --desired-count                The number of instantiations of the task to place and keep running in your service. Default is 1.
      --deployment-maximum-percent            The upper limit of the number of tasks that are allowed in the RUNNING or PENDING state in a service during a deployment.
      --deployment-minimum-healthy-percent    The lower limit of the number of running tasks that must remain in the RUNNING state in a service during a deployment.
      --placement-constraints                 Placement constraint to use for tasks in service. Format is <type>=<optional expression>,...
      --placement-strategy                    Placement strategy to use for tasks in service. Format is <type>=<optional field>,...
      --elb-service-role                      The name or (ARN) of the IAM role that allows ECS to make calls to the load balancer.
      -etg   | --elb-target-group             The full Amazon Resource Name (ARN) of the Elastic Load Balancing target group associated with a service.
      -ecp   | --elb-container-port           The port on the container to associate with the load balancer.
      --task-definition-name                  Name of the ECS Task Defintion to be created or updated.
      --task-definition-network-mode          The Docker networking mode to use for the containers in the task.
      --task-definition-task-role             The IAM role that will provide AWS credentials for the containers in the Task Definition.
      --task-execution-role                   The IAM role ECS assumes to pull images from ECR and publish logs to CloudWatch Logs. Fargate only.
      --task-cpu                              The amount of cpu to allocate for the task definition. Fargate only.
      --task-memory                           The amount of memory to allocated for the task definition. Fargate only.
      --task-definition-volumes               Volume definitions that containers in your task may use. Format is JSON string.
      --platform-version                      The platform version selected for the task. Fargate only.
      --container-command                     A comma delimited list of commands to pass to the container.
      --container-cpu                         The number of cpu units reserved for the container.
      --container-disable-networking          When this parameter is true, networking is disabled within the container.
      --container-dns-search-domains          A comma delimited of DNS search domains that are presented to the container.
      --container-dns-servers                 A comma delimited of DNS servers that are presented to the container.
      --container-docker-labels               Labels to add to the container. Format is <key1>=<value1>;<key2>=<value2>.
      --container-docker-security-options     A list of strings to provide custom labels for SELinux and AppArmor multi-level security systems.
      --container-entry-point                 The entry point that is passed to the container.
      --container-environment-variables       Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>.
      --container-is-essential                If true, and that container fails, all other containers that are part of the task are stopped.
      --container-extra-hosts                 Hostnames and IP address entries that are added to the /etc/hosts file of a container. Format is JSON string.
      --container-hostname                    The hostname to use for your container.
      --container-links                       Comma delimited list of container names to communicate without the need for port mapping.
      --container-linux-parameters            The Linux capabilities for the container that are added to or dropped. Format is JSON string.
      --container-log-configuration           The log driver to use for the container. Format is JSON string.
      --container-memory-hard-limit           The hard limit (in MiB) of memory to present to the container.
      --container-memory-soft-limit           The soft limit (in MiB) of memory to reserve for the container.
      --container-mount-points                The mount points for data volumes in your container. Format is JSON string.
      --container-name                        Name of the Container in a Task Definition to be created/updated.
      --container-port-mapping                The mapping of ports. Format is <host-port>:<container-port>,...
      --container-privileged                  If true, the container is given elevated privileges on the host container instance
      --container-readonly-root-filesystem    If true, the container is given read-only access to its root file system.
      --container-ulimits                     The ulimit settings to pass to the container. Format is JSON string.
      --container-user                        The user name to use inside the container.
      --container-working-directory           The working directory in which to run commands inside the container.
```

##### Deploy Task

```
dotnet ecs deploy-task
```

Deploys the .NET Core application as task on an ECS Cluster. This is good for batch processing and similar jobs
where once the process identified in the dockerfile exits the ECS task should end.

For list of supported options, use `dotnet ecs deploy-task --help`:
```
> dotnet ecs deploy-task --help
Amazon EC2 Container Service Tools for .NET Core applications (3.5.6)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

deploy-task: 
   Push the application to ECR and then runs it as a task on the ECS Cluster.

   dotnet  ecs deploy-task [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      -t     | --tag                          Name and optionally a tag in the 'name:tag' format.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
      -sip   | --skip-image-push              Skip building and push an image to Amazon ECR.
      -lt    | --launch-type                  The launch type on which to run tasks. Valid values EC2 | FARGATE.
      -ls    | --launch-subnets               Comma delimited list of subnet ids used when launch type is FARGATE
      -lsg   | --launch-security-groups       Comma delimited list of security group ids used when launch type is FARGATE
      --assign-public-ip                      If true a public IP address is assigned to the task when launch type is FARGATE
      -tc    | --task-count                   The number of instantiations of the task to place and keep running in your service. Default is 1.
      --task-group                            The task group to associate with the task. The default value is the family name of the task definition.
      --placement-constraints                 Placement constraint to use for tasks in service. Format is <type>=<optional expression>,...
      --placement-strategy                    Placement strategy to use for tasks in service. Format is <type>=<optional field>,...
      --task-definition-name                  Name of the ECS Task Defintion to be created or updated.
      --task-definition-network-mode          The Docker networking mode to use for the containers in the task.
      --task-definition-task-role             The IAM role that will provide AWS credentials for the containers in the Task Definition.
      --task-execution-role                   The IAM role ECS assumes to pull images from ECR and publish logs to CloudWatch Logs. Fargate only.
      --task-cpu                              The amount of cpu to allocate for the task definition. Fargate only.
      --task-memory                           The amount of memory to allocated for the task definition. Fargate only.
      --task-definition-volumes               Volume definitions that containers in your task may use. Format is JSON string.
      --platform-version                      The platform version selected for the task. Fargate only.
      --container-command                     A comma delimited list of commands to pass to the container.
      --container-cpu                         The number of cpu units reserved for the container.
      --container-disable-networking          When this parameter is true, networking is disabled within the container.
      --container-dns-search-domains          A comma delimited of DNS search domains that are presented to the container.
      --container-dns-servers                 A comma delimited of DNS servers that are presented to the container.
      --container-docker-labels               Labels to add to the container. Format is <key1>=<value1>;<key2>=<value2>.
      --container-docker-security-options     A list of strings to provide custom labels for SELinux and AppArmor multi-level security systems.
      --container-entry-point                 The entry point that is passed to the container.
      --container-environment-variables       Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>.
      --container-is-essential                If true, and that container fails, all other containers that are part of the task are stopped.
      --container-extra-hosts                 Hostnames and IP address entries that are added to the /etc/hosts file of a container. Format is JSON string.
      --container-hostname                    The hostname to use for your container.
      --container-links                       Comma delimited list of container names to communicate without the need for port mapping.
      --container-linux-parameters            The Linux capabilities for the container that are added to or dropped. Format is JSON string.
      --container-log-configuration           The log driver to use for the container. Format is JSON string.
      --container-memory-hard-limit           The hard limit (in MiB) of memory to present to the container.
      --container-memory-soft-limit           The soft limit (in MiB) of memory to reserve for the container.
      --container-mount-points                The mount points for data volumes in your container. Format is JSON string.
      --container-name                        Name of the Container in a Task Definition to be created/updated.
      --container-port-mapping                The mapping of ports. Format is <host-port>:<container-port>,...
      --container-privileged                  If true, the container is given elevated privileges on the host container instance
      --container-readonly-root-filesystem    If true, the container is given read-only access to its root file system.
      --container-ulimits                     The ulimit settings to pass to the container. Format is JSON string.
      --container-user                        The user name to use inside the container.
      --container-working-directory           The working directory in which to run commands inside the container.
```

##### Deploy Scheduled Task

```
dotnet ecs deploy-scheduled-task
```

Creates a new ECS task definition and then configures a Amazon CloudWatch Event rule to run a task using the new task definition and a scheduled interval.

For list of supported options, use `dotnet ecs deploy-scheduled-task --help`:
```
> dotnet ecs deploy-scheduled-task --help
Amazon EC2 Container Service Tools for .NET Core applications (3.5.6)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

deploy-scheduled-task:
   Push the application to ECR and then sets up CloudWatch Event Schedule rule to run the application.

   dotnet  ecs deploy-scheduled-task [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      -t     | --tag                          Name and optionally a tag in the 'name:tag' format.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
      -sip   | --skip-image-push              Skip building and push an image to Amazon ECR.
      --rule                                  The name of the CloudWatch Event Schedule rule.
      --rule-target                           The name of the target that will be assigned to the rule and point to the ECS task definition.
      --schedule-expression                   The scheduling expression. For example, "cron(0 20 * * ? *)" or "rate(5 minutes)".
      --cloudwatch-event-role                 The role that IAM will assume to invoke the target.
      -dc    | --desired-count                The number of instantiations of the task to place and keep running in your service. Default is 1.
      --task-definition-name                  Name of the ECS Task Defintion to be created or updated.
      --task-definition-network-mode          The Docker networking mode to use for the containers in the task.
      --task-definition-task-role             The IAM role that will provide AWS credentials for the containers in the Task Definition.
      --task-execution-role                   The IAM role ECS assumes to pull images from ECR and publish logs to CloudWatch Logs. Fargate only.
      --task-cpu                              The amount of cpu to allocate for the task definition. Fargate only.
      --task-memory                           The amount of memory to allocated for the task definition. Fargate only.
      --task-definition-volumes               Volume definitions that containers in your task may use. Format is JSON string.
      --platform-version                      The platform version selected for the task. Fargate only.
      --container-command                     A comma delimited list of commands to pass to the container.
      --container-cpu                         The number of cpu units reserved for the container.
      --container-disable-networking          When this parameter is true, networking is disabled within the container.
      --container-dns-search-domains          A comma delimited of DNS search domains that are presented to the container.
      --container-dns-servers                 A comma delimited of DNS servers that are presented to the container.
      --container-docker-labels               Labels to add to the container. Format is <key1>=<value1>;<key2>=<value2>.
      --container-docker-security-options     A list of strings to provide custom labels for SELinux and AppArmor multi-level security systems.
      --container-entry-point                 The entry point that is passed to the container.
      --container-environment-variables       Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>.
      --container-is-essential                If true, and that container fails, all other containers that are part of the task are stopped.
      --container-extra-hosts                 Hostnames and IP address entries that are added to the /etc/hosts file of a container. Format is JSON string.
      --container-hostname                    The hostname to use for your container.
      --container-links                       Comma delimited list of container names to communicate without the need for port mapping.
      --container-linux-parameters            The Linux capabilities for the container that are added to or dropped. Format is JSON string.
      --container-log-configuration           The log driver to use for the container. Format is JSON string.
      --container-memory-hard-limit           The hard limit (in MiB) of memory to present to the container.
      --container-memory-soft-limit           The soft limit (in MiB) of memory to reserve for the container.
      --container-mount-points                The mount points for data volumes in your container. Format is JSON string.
      --container-name                        Name of the Container in a Task Definition to be created/updated.
      --container-port-mapping                The mapping of ports. Format is <host-port>:<container-port>,...
      --container-privileged                  If true, the container is given elevated privileges on the host container instance
      --container-readonly-root-filesystem    If true, the container is given read-only access to its root file system.
      --container-ulimits                     The ulimit settings to pass to the container. Format is JSON string.
      --container-user                        The user name to use inside the container.
      --container-working-directory           The working directory in which to run commands inside the container.
```

##### Push Image

```
dotnet ecs push-image
```

Builds the Docker image from the .NET Core application and pushes it to Amazon Elastic Container Registery (ECR).
The other ECS deployment tasks first run this command before continuing on with deployment.

For list of supported options, use `dotnet ecs push-image --help`:
```
> dotnet ecs push-image --help
Amazon EC2 Container Service Tools for .NET Core applications (3.5.6)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

push-image:
   Build Docker image and push the image to Amazon ECR.

   dotnet  ecs push-image [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      -po    | --publish-options              Additional options passed to the "dotnet publish" command.
      --docker-host-build-output-dir          If set a "dotnet publish" command is executed on the host machine before executing "docker build". The output can be copied into image being built.
      -ldi   | --local-docker-image           If set the docker build command is skipped and the indicated local image is pushed to ECR.
      -df    | --dockerfile                   The docker file used to build the image. Default value is "Dockerfile".
      -it    | --image-tag                    Name and optionally a tag in the 'name:tag' format.
      -t     | --tag                          Obsolete. This has been replaced with the --image-tag switch.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
```

### AWS Elastic Beanstalk ([Amazon.ElasticBeanstalk.Tools](https://www.nuget.org/packages/Amazon.ElasticBeanstalk.Tools/))
---

This tool extension deploys ASP.NET Core applications to AWS Elastic Beanstalk environment.

#### Install

To install the extension run the following command.

```
dotnet tool install -g Amazon.ElasticBeanstalk.Tools
```



#### Available Commands


##### Deploy Environment
```
dotnet eb deploy-environment
```

Deploys the ASP.NET Core application to a Elastic Beanstalk environment after building and packaging up the application.
If the Elastic Beanstalk environment does not exist then the command will create the environment.

For list of supported options, use `dotnet eb deploy-environment --help`:
```
> dotnet eb deploy-environment --help
Amazon Elastic Beanstalk Tools for .NET Core applications (4.4.0)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

deploy-environment:
   Deploy the application to an AWS Elastic Beanstalk environment.

   dotnet  eb deploy-environment [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      --self-contained                        If true a self contained deployment bundle including the targeted .NET runtime will be created.
      -po    | --publish-options              Additional options passed to the "dotnet publish" command.
      -app   | --application                  The name of the Elastic Beanstalk application.
      -env   | --environment                  The name of the Elastic Beanstalk environment.
      --version-label                         Version label that will be assigned to the uploaded version of code. The default is current tick count.
      --tags                                  Tags to assign to the Elastic Beanstalk environment. Format is <tag1>=<value1>;<tag2>=<value2>.
      --app-path                              The application path. The default is '/'.
      --iis-website                           The IIS WebSite for the web application. The default is 'Default Web Site'
      --additional-options                    Additional options for the environment. Format is <option-namespace>,<option-name>=<option-value>;...
      --cname                                 CNAME prefix for a new environment.
      --solution-stack                        The type of environment to create. For example "64bit Windows Server 2016 v1.2.0 running IIS 10.0".
      --environment-type                      Type of the environment to launch "LoadBalanced" or "SingleInstance". The default is "LoadBalanced".
      --key-pair                              EC2 Key pair assigned to the EC2 instances for the environment.
      --instance-type                         Type of the EC2 instances launched for the environment. The default is "t2.micro" for Linux and "t3a.medium" for Windows.
      --health-check-url                      Health Check URL.
      --enable-xray                           If set to true then the AWS X-Ray daemon will be enabled on EC2 instances running the application.
      --disable-imds-v1                       If set to true then the IMDSv1 will be disabled on EC2 instances running the application.
      --enhanced-health-type                  The type of enhanced health to be enabled. Valid values: enhanced, basic
      --instance-profile                      Instance profile that provides AWS Credentials to access AWS services.
      --service-role                          IAM role to allow Beanstalk to make calls to AWS services.
      -pac   | --package                      Application package to use for deployment, skips building the project
      --loadbalancer-type                     LoadBalancer type for the environment. If no value set then a single instance environment type is created. Valid values: application, network, classic
      --enable-sticky-sessions                If set to true sticky sessions will be enabled for the load balancer of the environment.
      --proxy-server                          The reverse proxy server used on Linux EC2 instances. Valid values: nginx, none. The default is "nginx".
      --application-port                      The application port that will be redirect to port 80. The default is port 5000.
      --wait                                  Wait for the environment update to complete before exiting.
```

##### Delete Environment
```
dotnet eb delete-environment
```

Deletes an environment.

For list of supported options, use `dotnet eb delete-environment --help`:
```
> dotnet eb delete-environment --help
Amazon Elastic Beanstalk Tools for .NET Core applications (4.4.0)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

delete-environment: 
   Delete an AWS Elastic Beanstalk environment.

   dotnet  eb delete-environment [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -app   | --application                  The name of the Elastic Beanstalk application.
      -env   | --environment                  The name of the Elastic Beanstalk environment.
```

##### List Environments
```
dotnet eb list-environments
```

Lists all of the current running environments along with the URL to access the environment.

For list of supported options, use `dotnet eb list-environments --help`:
```
> dotnet eb list-environments --help
Amazon Elastic Beanstalk Tools for .NET Core applications (4.4.0)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli

list-environments:
   List the AWS Elastic Beanstalk environments.

   dotnet  eb list-environments [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
```

### AWS Lambda ([Amazon.Lambda.Tools](https://www.nuget.org/packages/Amazon.Lambda.Tools/))
---

This tool extension deploys AWS Lambda .NET Core functions. 

#### Install

To install the extension run the following command.

```
dotnet tool install -g Amazon.Lambda.Tools
```


#### Available Commands

##### Deploy Function
```
dotnet lambda deploy-function
```

Deploys the .NET Core Lambda project directly to the AWS Lambda service. The function is created if
this is the first deployment. If the Lambda function already exists then the function code is updated.
If any of the function configuration properties specified on the command line are different, the existing 
function configuration is updated. To avoid accidental function configuration changes during a redeployment, 
only default values explicitly set on the command line are used. The defaults file is not used.

For list of supported options, use `dotnet lambda deploy-function --help`:
```
> dotnet lambda deploy-function --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

deploy-function:
   Command to deploy the project to AWS Lambda

   dotnet lambda deploy-function [arguments] [options]
   Arguments:
      <FUNCTION-NAME> The name of the function to be updated
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      --msbuild-parameters                    Additional msbuild parameters passed to the 'dotnet publish' command. Add quotes around the value if the value contains spaces.
      -pac   | --package                      Application package to use for deployment, skips building the project
      -fn    | --function-name                AWS Lambda function name
      -fd    | --function-description         AWS Lambda function description
      -pt    | --package-type                 The deployment package type for Lambda function. Valid values: image, zip
      -fp    | --function-publish             Publish a new version as an atomic operation
      -fms   | --function-memory-size         The amount of memory, in MB, your Lambda function is given
      -frole | --function-role                The IAM role that Lambda assumes when it executes your function
      -ft    | --function-timeout             The function execution timeout in seconds
      -fh    | --function-handler             Handler for the function <assembly>::<type>::<method>
      -frun  | --function-runtime             The runtime environment for the Lambda function
      -farch | --function-architecture        The architecture of the Lambda function. Valid values: x86_64 or arm64. Default is x86_64
      -fl    | --function-layers              Comma delimited list of Lambda layer version arns
      -ie    | --image-entrypoint             Overrides the image's ENTRYPOINT when package type is set "image".
      -ic    | --image-command                Overrides the image's CMD when package type is set "image".
      -iwd   | --image-working-directory      Overrides the image's working directory when package type is set "image".
      -it    | --image-tag                    Name and optionally a tag in the 'name:tag' format.
      --ephemerals-storage-size               The size of the function's /tmp directory in MB. The default value is 512, but can be any whole number between 512 and 10240 MB
      --function-url-enable                   Enable function URL. A function URL is a dedicated HTTP(S) endpoint for your Lambda function.
      --function-url-auth                     The type of authentication that your function URL uses, default value is NONE. Valid values: NONE or AWS_IAM
      --tags                                  AWS tags to apply. Format is <name1>=<value1>;<name2>=<value2>
      -fsub  | --function-subnets             Comma delimited list of subnet ids if your function references resources in a VPC
      -fsec  | --function-security-groups     Comma delimited list of security group ids if your function references resources in a VPC
      -dlta  | --dead-letter-target-arn       Target ARN of an SNS topic or SQS Queue for the Dead Letter Queue
      -tm    | --tracing-mode                 Configures when AWS X-Ray should trace the function. Valid values: PassThrough or Active
      -ev    | --environment-variables        Environment variables set for the function. For existing functions this replaces the current environment variables. Format is <key1>=<value1>;<key2>=<value2>
      -aev   | --append-environment-variables Append environment variables to the existing set of environment variables for the function. Format is <key1>=<value1>;<key2>=<value2>
      -kk    | --kms-key                      KMS Key ARN of a customer key used to encrypt the function's environment variables
      --apply-defaults                        Obsolete: as of version 3.0.0.0 defaults are always applied.
      -rs    | --resolve-s3                   If set to true a bucket with the name format of "aws-dotnet-lambda-tools-<region>-<account-id>" will be configured to store build outputs
      -sb    | --s3-bucket                    S3 bucket to upload the build output
      -sp    | --s3-prefix                    S3 prefix for for the build output
      -dvc   | --disable-version-check        Disable the .NET Core version check. Only for advanced usage.
      -ldi   | --local-docker-image           If set the docker build command is skipped and the indicated local image is pushed to ECR.
      -df    | --dockerfile                   The docker file used to build the image. Default value is "Dockerfile".
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      --docker-host-build-output-dir          If set a "dotnet publish" command is executed on the host machine before executing "docker build". The output can be copied into image being built.
      -ucfb  | --use-container-for-build      Use a local container to build the Lambda binary. A default image will be provided if none is supplied.
      -cifb  | --container-image-for-build    The container image tag (with version) to be used for building the Lambda binary.
      -cmd   | --code-mount-directory         Path to the directory to mount to the build container. Otherwise, look upward for a solution folder.
      -lf    | --log-format                   The log format used by the Lambda function. Valid values are: Text or JSON. Default is Text
      -lal   | --log-application-level        The log level. Valid values are: TRACE, DEBUG, INFO, WARN, ERROR or FATAL. Default is INFO.
      -lsl   | --log-system-level             The log system level. Valid values are: DEBUG, INFO, WARN. Default is INFO.
      -lg    | --log-group                    The name of the Amazon CloudWatch log group the function sends logs to. Default is /aws/lambda/<function name>.
      -sa    | --snap-start-apply-on          Configure when a snapshot of the initialized execution environment should be taken. Valid values are: PublishedVersions, None. Default is None.
```

##### Invoke Function
```
dotnet lambda invoke-function MyFunction --payload "The Function Payload"
```

Invokes the Lambda function in AWS Lambda passing in the value of **--payload** as the input parameter to the Lambda function.

For list of supported options, use `dotnet lambda invoke-function --help`:
```
> dotnet lambda invoke-function --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

invoke-function:
   Command to invoke a function in Lambda with an optional input

   dotnet lambda invoke-function [arguments] [options]
   Arguments:
      <FUNCTION-NAME> The name of the function to invoke
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -fn    | --function-name                AWS Lambda function name
      -p     | --payload                      The input payload to send to the Lambda function
```

##### List Functions
```
dotnet lambda list-functions
```

List all of the currently deployed Lambda functions.

For list of supported options, use `dotnet lambda list-functions --help`:
```
> dotnet lambda list-functions --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

list-functions:
   Command to list all your Lambda functions

   dotnet  lambda list-functions [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
```

##### Delete Function
```
dotnet lambda delete-function
```

Delete a Lambda function

For list of supported options, use `dotnet lambda delete-function --help`:
```
> dotnet lambda delete-function --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

delete-function:
   Command to delete a Lambda function

   dotnet lambda delete-function [arguments] [options]
   Arguments:
      <FUNCTION-NAME> The name of the function to delete
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -fn    | --function-name                AWS Lambda function name
```

##### Get Function Configuration
```
dotnet lambda get-function-config
```

Get the Lambda function's configuration like memory limit and timeout.

For list of supported options, use `dotnet lambda get-function-config --help`:
```
> dotnet lambda get-function-config --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

get-function-config:
   Command to get the current runtime configuration for a Lambda function

   dotnet lambda get-function-config [arguments] [options]
   Arguments:
      <FUNCTION-NAME> The name of the function to get the configuration for
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -fn    | --function-name                AWS Lambda function name
```

##### Update Function Configuration
```
dotnet lambda update-function-config
```

Update the Lambda function's configuration without uploading new code.

For list of supported options, use `dotnet lambda update-function-config --help`:
```
> dotnet lambda update-function-config --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

update-function-config:
   Command to update the runtime configuration for a Lambda function

   dotnet lambda update-function-config [arguments] [options]
   Arguments:
      <FUNCTION-NAME> The name of the function to be updated
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -fn    | --function-name                AWS Lambda function name
      -fd    | --function-description         AWS Lambda function description
      -fp    | --function-publish             Publish a new version as an atomic operation
      -fms   | --function-memory-size         The amount of memory, in MB, your Lambda function is given
      -frole | --function-role                The IAM role that Lambda assumes when it executes your function
      -ft    | --function-timeout             The function execution timeout in seconds
      -frun  | --function-runtime             The runtime environment for the Lambda function
      -fh    | --function-handler             Handler for the function <assembly>::<type>::<method>
      -fl    | --function-layers              Comma delimited list of Lambda layer version arns
      --ephemerals-storage-size               The size of the function's /tmp directory in MB. The default value is 512, but can be any whole number between 512 and 10240 MB
      --function-url-enable                   Enable function URL. A function URL is a dedicated HTTP(S) endpoint for your Lambda function.
      --function-url-auth                     The type of authentication that your function URL uses, default value is NONE. Valid values: NONE or AWS_IAM
      -ie    | --image-entrypoint             Overrides the image's ENTRYPOINT when package type is set "image".
      -ic    | --image-command                Overrides the image's CMD when package type is set "image".
      -iwd   | --image-working-directory      Overrides the image's working directory when package type is set "image".
      --tags                                  AWS tags to apply. Format is <name1>=<value1>;<name2>=<value2>
      -fsub  | --function-subnets             Comma delimited list of subnet ids if your function references resources in a VPC
      -fsec  | --function-security-groups     Comma delimited list of security group ids if your function references resources in a VPC
      -dlta  | --dead-letter-target-arn       Target ARN of an SNS topic or SQS Queue for the Dead Letter Queue
      -tm    | --tracing-mode                 Configures when AWS X-Ray should trace the function. Valid values: PassThrough or Active
      -ev    | --environment-variables        Environment variables set for the function. For existing functions this replaces the current environment variables. Format is <key1>=<value1>;<key2>=<value2>
      -aev   | --append-environment-variables Append environment variables to the existing set of environment variables for the function. Format is <key1>=<value1>;<key2>=<value2>
      -kk    | --kms-key                      KMS Key ARN of a customer key used to encrypt the function's environment variables
      --apply-defaults                        Obsolete: as of version 3.0.0.0 defaults are always applied.
      -lf    | --log-format                   The log format used by the Lambda function. Valid values are: Text or JSON. Default is Text
      -lal   | --log-application-level        The log level. Valid values are: TRACE, DEBUG, INFO, WARN, ERROR or FATAL. Default is INFO.
      -lsl   | --log-system-level             The log system level. Valid values are: DEBUG, INFO, WARN. Default is INFO.
      -lg    | --log-group                    The name of the Amazon CloudWatch log group the function sends logs to. Default is /aws/lambda/<function name>.
      -sa    | --snap-start-apply-on          Configure when a snapshot of the initialized execution environment should be taken. Valid values are: PublishedVersions, None. Default is None.
```

##### Deploy Serverless
```
dotnet lambda deploy-serverless
```

Deploys one or more Lambda functions from the Lambda project through CloudFormation. The project uses the 
**serverless.template** CloudFormation template to deploy the serverless app along with any additional 
AWS resources defined in the **serverless.template**. 

CloudFormation stacks created with this command are tagged with the **AWSServerlessAppNETCore** tag.

For list of supported options, use `dotnet lambda deploy-serverless --help`:
```
> dotnet lambda deploy-serverless --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

deploy-serverless:
   Command to deploy an AWS Serverless application

   dotnet lambda deploy-serverless [arguments] [options]
   Arguments:
      <STACK-NAME> The name of the CloudFormation stack used to deploy the AWS Serverless application
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      --msbuild-parameters                    Additional msbuild parameters passed to the 'dotnet publish' command. Add quotes around the value if the value contains spaces.
      -pac   | --package                      Application package to use for deployment, skips building the project
      -rs    | --resolve-s3                   If set to true a bucket with the name format of "aws-dotnet-lambda-tools-<region>-<account-id>" will be configured to store build outputs
      -sb    | --s3-bucket                    S3 bucket to upload the build output
      -sp    | --s3-prefix                    S3 prefix for for the build output
      -t     | --template                     Path to the CloudFormation template
      -tp    | --template-parameters          CloudFormation template parameters. Format is <key1>=<value1>;<key2>=<value2>
      -ts    | --template-substitutions       JSON based CloudFormation template substitutions. Format is <JSONPath>=<Substitution>;<JSONPath>=...
      -cfrole | --cloudformation-role         Optional role that CloudFormation assumes when creating or updated CloudFormation stack.
      -sn    | --stack-name                   CloudFormation stack name for an AWS Serverless application
      -dc    | --disable-capabilities         Comma delimited list of capabilities to disable when creating a CloudFormation Stack.
      --tags                                  AWS tags to apply. Format is <name1>=<value1>;<name2>=<value2>
      -sw    | --stack-wait                   If true wait for the Stack to finish updating before exiting. Default is true.
      -dvc   | --disable-version-check        Disable the .NET Core version check. Only for advanced usage.
      -pd    | --stack-polling-delay          The time interval in seconds between each check for stack updates. Default is 3 seconds.
```

##### List Serverless
```
dotnet lambda list-serverless
```

Lists the .NET Core Serverless applications which are identified by looking for the **AWSServerlessAppNETCore** tag 
on existing CloudFormation Stacks.

For list of supported options, use `dotnet lambda list-serverless --help`:
```
> dotnet lambda list-serverless --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

list-serverless:
   Command to list all your AWS Serverless applications

   dotnet  lambda list-serverless [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
```

##### Delete Serverless
```
dotnet lambda delete-serverless
```

Deletes the serverless application by deleting the CloudFormation stack.

For list of supported options, use `dotnet lambda delete-serverless --help`:
```
> dotnet lambda delete-serverless --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

delete-serverless:
   Command to delete an AWS Serverless application

   dotnet lambda delete-serverless [arguments] [options]
   Arguments:
      <STACK-NAME> The CloudFormation stack for the AWS Serverless application
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -sn    | --stack-name                   CloudFormation stack name for an AWS Serverless application
```

##### Package CI
```
dotnet lambda package-ci
```

Used for serverless applications. It creates the Lambda application bundle and uploads it to Amazon S3. It then writes 
a new version of the serverless.template with the location of the Lambda function code updated to 
where the application bundle was uploaded. In an AWS CodePipeline this command can be executed as part of a **CodeBuild** 
stage returning the transformed template as the build artifact. Later in the pipeline that transformed serverless.template can
be used with a CloudFormation stage to deploy the application.

For list of supported options, use `dotnet lambda package-ci --help`:
```
> dotnet lambda package-ci --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

package-ci:
   Command to use as part of a continuous integration system.

   dotnet  lambda package-ci [options]
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      --msbuild-parameters                    Additional msbuild parameters passed to the 'dotnet publish' command. Add quotes around the value if the value contains spaces.
      -t     | --template                     Path to the CloudFormation template
      -ts    | --template-substitutions       JSON based CloudFormation template substitutions. Format is <JSONPath>=<Substitution>;<JSONPath>=...
      -ot    | --output-template              Path to write updated serverless template with CodeURI fields updated to the location of the packaged build artifacts in S3.
      -rs    | --resolve-s3                   If set to true a bucket with the name format of "aws-dotnet-lambda-tools-<region>-<account-id>" will be configured to store build outputs
      -sb    | --s3-bucket                    S3 bucket to upload the build output
      -sp    | --s3-prefix                    S3 prefix for for the build output
      -dvc   | --disable-version-check        Disable the .NET Core version check. Only for advanced usage.
```

##### Notes

`dotnet lambda package-ci` inspects and uses the [`Architectures` property of `AWS::Serverless::Function`](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-function.html#sam-function-architectures) to determine the runtime of the package.

For example:

```
      "Architectures": [
        "arm64"
      ],
```

will execute `dotnet publish` with a `--runtime` argument of value `linux-arm64`:

The default is `linux-x64` (which will execute `dotnet publish` with a `--runtime` argument of value `linux-x64`: )

The `Architectures` array can be specified either by:
1. Directly with the path `AWS::Serverless::Function` 
2. Within the [AWS SAM Template syntax `Globals`](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-specification-template-anatomy-globals.html)

Example of directly with the path `AWS::Serverless::Function` in a `serverless.template`:

```
  ...
  "Resources": {
    "ApiFnMFS3hGenerated": {
      "Type": "AWS::Serverless::Function",
      "Properties": {
        "Architectures": [
          "arm64"
        ],
        "CodeUri": ".",
        "MemorySize": 10240,
        "Timeout": 900,
        "Role": {
          "Ref": "LambdaExecutionRole"
        },
        "PackageType": "Zip",
        "Handler": "MyAssembly.MyNamespace::MyAssembly.MyNamespace.MyClass::MyFunction"
      }
    }
  },
  "Parameters" ...
```

Example of within the AWS SAM Template syntax `Globals` in a `serverless.template`:

```
...
  "Globals": {
    "Function": {
      "Runtime": "dotnet6",
      "Architectures": [
        "arm64"
      ]
    }
  },
  "Resources": {
    "ApiFnMFS3hGenerated": {
      "Type": "AWS::Serverless::Function",
      "Properties": {
        "Architectures": [
          "arm64"
        ],
        "CodeUri": ".",
        "MemorySize": 10240,
        "Timeout": 900,
        "Role": {
          "Ref": "LambdaExecutionRole"
        },
        "PackageType": "Zip",
        "Handler": "MyAssembly.MyNamespace::MyAssembly.MyNamespace.MyClass::MyFunction"
      }
    }
  },
  "Parameters" ...
```

##### Package
```
dotnet lambda package
```

Creates the Lambda application bundle that can later be deployed to Lambda.

For list of supported options, use `dotnet lambda package --help`:
```
> dotnet lambda package --help
Amazon Lambda Tools for .NET Core applications (5.12.4)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet

package:
   Command to package a Lambda project either into a zip file or docker image if --package-type is set to "image". The output can later be deployed to Lambda with either deploy-function command or with another tool.  

   dotnet lambda package [arguments] [options]
   Arguments:
      <ZIP-FILE> The name of the zip file to package the project into
   Options:
      --disable-interactive                   When set to true missing required parameters will not be prompted for.
      --region                                The region to connect to AWS services, if not set region will be detected from the environment.
      --profile                               Profile to use to look up AWS credentials, if not set environment credentials will be used.
      --profile-location                      Optional override to the search location for Profiles, points at a shared credentials file.
      --aws-access-key-id                     The AWS access key id. Used when setting credentials explicitly instead of using --profile.
      --aws-secret-key                        The AWS secret key. Used when setting credentials explicitly instead of using --profile.
      --aws-session-token                     The AWS session token. Used when setting credentials explicitly instead of using --profile.
      -pl    | --project-location             The location of the project, if not set the current directory will be assumed.
      -cfg   | --config-file                  Configuration file storing default values for command line arguments.
      -pcfg  | --persist-config-file          If true the arguments used for a successful deployment are persisted to a config file.
      -c     | --configuration                Configuration to build with, for example Release or Debug.
      -f     | --framework                    Target framework to compile, for example netcoreapp3.1.
      -farch | --function-architecture        The architecture of the Lambda function. Valid values: x86_64 or arm64. Default is x86_64
      --msbuild-parameters                    Additional msbuild parameters passed to the 'dotnet publish' command. Add quotes around the value if the value contains spaces.
      -fl    | --function-layers              Comma delimited list of Lambda layer version arns
      -o     | --output-package               The zip file that will be created with compiled and packaged Lambda function.
      -dvc   | --disable-version-check        Disable the .NET Core version check. Only for advanced usage.
      -pt    | --package-type                 The deployment package type for Lambda function. Valid values: image, zip
      -it    | --image-tag                    Docker image name and tag in the 'name:tag' format.
      -df    | --dockerfile                   The docker file used to build the image. Default value is "Dockerfile".
      -dbo   | --docker-build-options         Additional options passed to the "docker build" command.
      -dbwd  | --docker-build-working-dir     The directory to execute the "docker build" command from.
      --docker-host-build-output-dir          If set a "dotnet publish" command is executed on the host machine before executing "docker build". The output can be copied into image being built.
      -ucfb  | --use-container-for-build      Use a local container to build the Lambda binary. A default image will be provided if none is supplied.
      -cifb  | --container-image-for-build    The container image tag (with version) to be used for building the Lambda binary.
      -cmd   | --code-mount-directory         Path to the directory to mount to the build container. Otherwise, look upward for a solution folder.
```
