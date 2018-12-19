# AWS Extensions for .NET CLI ![Build Status](https://codebuild.us-east-1.amazonaws.com/badges?uuid=eyJlbmNyeXB0ZWREYXRhIjoiMWdDdVRndE96VXVCMHU0WnI1YWNGYUE1MW9iMnhhVThIYlVBNEQzMXNJdkxnT1o3cmlrL2lXKzVLZHZjZWdjRlFWV0JSbWpMNE9ZVnQ2czJNTExpMVc0PSIsIml2UGFyYW1ldGVyU3BlYyI6IlMzdlh6ZUk0SWNVZkcwOFUiLCJtYXRlcmlhbFNldFNlcmlhbCI6MX0%3D&branch=master)

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

The extension will prompt you for misssing required parameters. To disable the extension from prompting, set the 
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

The following AWS services each have their own .NET CLI tool extension to make easy to deploy a .NET Core Application
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

This tool extension takes care of builindg a Docker image from a .NET application and then deploying 
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

##### Deploy Task

```
dotnet ecs deploy-task
```

Deploys the .NET Core application as task on an ECS Cluster. This is good for batch processsing and similar jobs
where once the process identified in the dockerfile exits the ECS task should end.

##### Deploy Scheduled Task

```
dotnet ecs deploy-scheduled-task
```

Creates a new ECS task definition and then configures a Amazon CloudWatch Event rule to run a task using
the new task definition and a scheduled interval.


##### Push Image

```
dotnet ecs push-image
```

Builds the Docker image from the .NET Core application and pushes it to Amazon Elastic Container Registery (ECR).
The other ECS deployment tasks first run this command before continuing on with deployment.

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

##### Delete Environment
```
dotnet eb delete-environment
```

Deletes an environment.

##### List Environments
```
dotnet eb list-environments
```

Lists all of the current running environments along with the URL to access the environment.

### AWS Lambda ([Amazon.Lambda.Tools](https://www.nuget.org/packages/Amazon.Lambda.Tools/))
---

This tool extension deploys AWS Lambda .NET Core functions. 

This tool extension was originally released with the
.NET Core 1.0 Lambda release and the code for it is currently in the [aws-lambda-dotnet](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.Tools)
repository. The plan is to bring the project into this repository and use the same framework as the
other tool extensions which will make it more consistent with the other tool extensions.

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

##### Invoke Function
```
dotnet lambda invoke-function MyFunction --payload "The Function Payload"
```

Invokes the Lambda function in AWS Lambda passing in the value of **--payload** as the input parameter to the Lambda function.

##### List Functions
```
dotnet lambda list-functions
```

List all of the currently deployed Lambda functions.

##### Delete Function
```
dotnet lambda delete-function
```

Delete a Lambda function

##### Get Function Configuration
```
dotnet lambda get-function-config
```

Get the Lambda function's configuration like memory limit and timeout.

##### Update Function Configuration
```
dotnet lambda update-function-config
```

Update the Lambda function's configuration without uploading new code.
##### Deploy Serverless
```
dotnet lambda deploy-serverless
```

Deploys one or more Lambda functions from the Lambda project through CloudFormation. The project uses the 
**serverless.template** CloudFormation template to deploy the serverless app along with any additional 
AWS resources defined in the **serverless.template**. 

CloudFormation stacks created with this command are tagged with the **AWSServerlessAppNETCore** tag.

##### List Serverless
```
dotnet lambda list-serverless
```

Lists the .NET Core Serverless applications which are identified by looking for the **AWSServerlessAppNETCore** tag 
on existing CloudFormation Stacks.

##### Delete Serverless
```
dotnet lambda delete-serverless
```

Deletes the serverless application by deleting the CloudFormation stack.

##### Package CI
```
dotnet lambda package-ci
```

Used for serverless applications. It creates the Lambda application bundle and uploads it to Amazon S3. It then writes 
a new version of the serverless.template with the location of the Lambda function code updated to 
where the application bundle was uploaded. In an AWS CodePipeline this command can be executed as part of a **CodeBuild** 
stage returning the transformed template as the build artifact. Later in the pipeline that transformed serverless.template can
be used with a CloudFormation stage to deploy the application.

##### Package
```
dotnet lambda package
```

Creates the Lambda application bundle that can later be deployed to Lambda.
