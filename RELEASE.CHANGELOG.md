### Release 2023-03-15
* **Amazon.Lambda.Tools (5.6.4)**
  * Fixed an issue which caused upgrading from an End-of-Life .NET version to a supported version to fail.

### Release 2023-01-18
* **Amazon.Lambda.Tools (5.6.3)**
  * Pull Request [#257](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/257) supporting non-root users when doing container based builds. Thanks [Jason T](https://github.com/jasonterando)
  * Pull Request [#260](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/260) fixed typo in Security Groups option name. Thanks [Mohammad Sadegh Shad](https://github.com/m-sadegh-sh)

### Release 2022-11-14
* **Amazon.Lambda.Tools (5.6.2)**
  * Fixed regression in 5.6.0 that prevented the package command for non managed .NET runtime like .NET 5 and 7. This feature is required for SAM container image builds.
  
### Release 2022-11-08
* **Amazon.Lambda.Tools (5.6.1)**
  * Fixed regression in 5.6.0 that excluded pdb files from being packaged in deployment bundle breaking SAM debugger experience.
  
### Release 2022-11-08
* **Amazon.Lambda.Tools (5.6.0)**
  * Added support for deploying Native AOT .NET 7 Lambda functions. To enable Native AOT set the PublishAot property in project file to true.
  * Added support for container builds when creating deployment bundle.

### Release 2022-10-26
* **Amazon.Lambda.Tools (5.5.0)**
  * Add new --resolve-s3 switch that can replace the --s3-bucket switch. When --resolve-s3 is set true the tool will ensure a default bucket exists and use that bucket for storing deployment bundles.
  
### Release 2022-08-18
* **Amazon.Common.DotNetCli.Tools (3.1.0.1)**
  * Fixes an issue where exception could occur while expanding null policy name and attaching it to a role.
* **Amazon.Lambda.Tools (5.4.5)**
  * Fixes an issue where Lambda deploy-function fails when choosing option to add permissions later.
* **Amazon.ECS.Tools (3.5.2)**
  * Updated to reference the latest version of Amazon.Common.DotNetCli.Tools.
* **Amazon.ElasticBeanstalk.Tools (4.3.2)**
  * Updated to reference the latest version of Amazon.Common.DotNetCli.Tools.

### Release 2022-06-27
* **Amazon.Lambda.Tools (5.4.4)**
  * Bump Newtonsoft.Json to 13.0.1
  
### Release 2022-06-21
* **Amazon.Lambda.Tools (5.4.3)**
  * Added ability to use DockerBuildArgs in Amazon.Lambda.Tools serverless template.

### Release 2022-06-02
* **Amazon.Lambda.Tools (5.4.2)**
  * Only modify Function Url if `--function-url-enable` flag is set.
  * Fixed an issue where lambda push-image command was ignoring Docker options.
  
### Release 2022-04-25
* **Amazon.Lambda.Tools (5.4.1)**
  * Fixed issue when `--function-url-enable` is absent the function url config was unintendedly removed. 

### Release 2022-04-25
* **Amazon.Lambda.Tools (5.4.0)**
  * Added `--function-url-enable` and `--function-url-auth` switches to configure Lambda Function Url.
  * Added `--ephemerals-storage-size` switch to configure the size of writable the `/tmp` folder.
  * Fixed issue with removing all values from the following collection properties: Environment Variables, Layers and VPC subnets and security groups.

### Release 2022-02-14
* **Amazon.Lambda.Tools (5.3.0)**
  * Package the tool targeting .NET 6 as well as the previous .NET Core 3.1 to support Mac M1 developers.
  * Add .NET 6 target framework moniker to .NET 6 Lambda runtime enum mapping.
* **Amazon.ECS.Tools (3.5.0)**
  * Package the tool targeting .NET 6 as well as the previous .NET Core 3.1 to support Mac M1 developers.
* **Amazon.ElasticBeanstalk.Tools (4.3.0)**
  * Package the tool targeting .NET 6 as well as the previous .NET Core 3.1 to support Mac M1 developers.
  
### Release 2021-09-29
* **Amazon.Lambda.Tools (5.2.0)**
  * Added support for deploying ARM based Lambda functions with the new `--function-architecture` switch.
  
### Release 2021-09-28
* **Amazon.ECS.Tools (3.4.3)**
  * Fixed an issue where ECS log configuration argument is overwritten with awslogs defaults.

### Release 2021-06-17
* **Amazon.Lambda.Tools (5.1.4)**
  * Added reference to AWSSDK.SSO and AWSSDK.SSOOIDC for SSO flow.
* **Amazon.ECS.Tools (3.4.2)**
  * Added reference to AWSSDK.SSO and AWSSDK.SSOOIDC for SSO flow.
* **Amazon.ElasticBeanstalk.Tools (4.2.2)**
  * Added reference to AWSSDK.SSO and AWSSDK.SSOOIDC for SSO flow.

### Release 2021-06-02
* **Amazon.Lambda.Tools (5.1.3)**
  * Updated to version 3.7.0.27 of AWSSDK.Core
  * Updated to version 3.7.1.15 of AWSSDK.SecurityToken
* **Amazon.ECS.Tools (3.4.1)**
  * Updated to version 3.7.0.27 of AWSSDK.Core
  * Updated to version 3.7.1.15 of AWSSDK.SecurityToken
* **Amazon.ElasticBeanstalk.Tools (4.2.1)**
  * Updated to version 3.7.0.27 of AWSSDK.Core
  * Updated to version 3.7.1.15 of AWSSDK.SecurityToken

### Release 2021-05-03
* **Amazon.Lambda.Tools (5.1.2)**
  * Pull request [#170](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/170) Fixed issue with unnecessary function config update when using VPC settings. Thanks [Abubaker Bashir](https://github.com/AbubakerB)
  * Pull request [#169](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/169) Fixed issue with runtime and handler fields not being updated. Thanks [Abubaker Bashir](https://github.com/AbubakerB)
  
### Release 2021-04-14
* **Amazon.Lambda.Tools (5.1.1)**
  * Fixed an issue where relative paths in package-ci command were not working.

### Release 2021-04-02
* **Amazon.Lambda.Tools (5.1.0)**
  * Update to latest version of the AWS SDK for .NET.
* **Amazon.ECS.Tools (3.4.0)**
  * Update to latest version of the AWS SDK for .NET.
* **Amazon.ElasticBeanstalk.Tools (4.2.0)**
  * Update to latest version of the AWS SDK for .NET.

### Release 2021-03-24
* **Amazon.Lambda.Tools (5.01.0)**
  * Updated to version 3.7 of the AWS SDK for .NET
* **Amazon.ECS.Tools (3.4.0)**
  * Updated to version 3.7 of the AWS SDK for .NET
* **Amazon.ElasticBeanstalk.Tools (4.2.0)**
  * Updated to version 3.7 of the AWS SDK for .NET
  
### Release 2021-03-24
* **Amazon.Lambda.Tools (5.0.2)**
  * Updated version of the AWS SDK for .NET used to include support for SSO.
  * Pull request [#163](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/163) Fixed random manifest names causing zip package hash refresh on every build. Thanks [aohotnik](https://github.com/aohotnik)
  * Pull request [#152](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/152) Pass OriginalCommandLineArguments to Command constructor. Thanks [Vickram Ravichandran](https://github.com/vickramravichandran)
* **Amazon.ECS.Tools (3.3.1)**
  * Updated version of the AWS SDK for .NET used to include support for SSO.
* **Amazon.ElasticBeanstalk.Tools (4.1.1)**
  * Updated version of the AWS SDK for .NET used to include support for SSO.

### Release 2021-01-21
* **Amazon.Lambda.Tools (5.0.1)**
  * Fixed issue with handling Lambda projects that were multi targeting .NET versions
* **Amazon.ECS.Tools (3.3.0)**
  * Added support for deploying scheduled tasks using AWS Fargate.
  * The docker image tag will be used from either the newer `--image-tag` switch or the deprecated `--tag` switch.

### Release 2020-12-01
* **Amazon.Lambda.Tools (5.0.0)**
  * Updated deploy-function to have the following switches to support Lambda functions packaged as container images.
    * `--package-type`: Determines the format for packaging Lambda function. Valid values are `zip` and `image`. Default is `zip`.
    * `--image-entrypoint`: Overrides the image's ENTRYPOINT when package type is set `image`
    * `--image-command`: Overrides the image's CMD when package type is set `image`
    * `--image-working-directory`: Overrides the image's working directory when package type is set `image`
    * `--image-tag`: Name and optionally a tag in the 'name:tag' format
    * `--local-docker-image`: If set the docker build command is skipped and the indicated local image is pushed to ECR
    * `--dockerfile`: The docker file used build image. Default value is "Dockerfile"
    * `--docker-build-options`: Additional options passed to the "docker build" command
    * `--docker-build-working-dir`: The directory to execute the "docker build" command from
    * `--docker-host-build-output-dir`: If set a "dotnet publish" command is executed on the host machine before executing "docker build". The output can be copied into image being built.
  * Updated `deploy-serverless` command to build and push Lambda functions as container images if CloudFormation resource has `PackageType` set to `image`
  * Updated `package` command to build container image if `--package-type` is set to `image`. The image can later be used with `deploy-function` using the `--local-docker-image`
  * Added push-image command to build .NET Lambda project and push to ECR

### Release 2020-10-19
* **Amazon.Lambda.Tools (4.3.0)**
  * Update to latest version of the AWS SDK for .NET.
* **Amazon.ECS.Tools (3.2.0)**
  * Update to latest version of the AWS SDK for .NET.
* **Amazon.ElasticBeanstalk.Tools (4.1.0)**
  * Update to latest version of the AWS SDK for .NET.

### Release 2020-10-15
* **Amazon.Lambda.Tools (4.2.0)**
  * Add support for creating .NET Lambda layers for .NET Core 3.1.

### Release 2020-07-22
* **Amazon.Lambda.Tools (4.1.0)**
  * Pull Request [$120](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/120): Echo the full `dotnet publish` command using during lambda deployment. Thanks [Tom Makin](https://github.com/tmakin)
  * Fixed issue when publish PowerShell Lambda functions with PowerShell unable to find system modules folder when deployed to Lambda.

### Release 2020-06-23
* **Amazon.ElasticBeanstalk.Tools (4.0.0)**
  * Added support to to deploy to the new Beanstalk ".NET Core for Linux" platform.
  * Added ability to enable sticky sessions.
  * Added switch to do a self contained publish

### Release 2020-03-31
* **Amazon.Lambda.Tools (4.0.0)**
  * Added support to deploy to .NET Core 3.1 Lambda runtime
  * Switch RID to linux-x64 when packaging runtimes on Amazon Linux 2. Currently that is only .NET Core 3.1.
  * If `--runtime` is set by the user via `--msbuild-parameters` switch then Amazon.Lambda.Tools will not set the `--runtime` switch itself when calling `dotnet package`.
  * Disable creating of Lambda layers for .NET Core 3.1 due to an issue in `dotnet store` command. Read here on the issue. https://github.com/dotnet/sdk/issues/10973

### Release 2019-09-17
* **Amazon.Lambda.Tools (3.3.0)**
  * Fixed issue [#90](https://github.com/aws/aws-extensions-for-dotnet-cli/issues/90): Error parsing layer description while listing layers
  * Fixed issue [#30](https://github.com/aws/aws-extensions-for-dotnet-cli/issues/30): Parsed yaml CloudFormaion template failure if there was no Properties node.
  * Pull request [#89](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/89) Fixed typo warning how to set the DOTNET_SHARED_STORE environment varaible. Thanks [Oleg Kosmakov](https://github.com/kosmakoff)
### Release 2019-08-16
* **Amazon.Lambda.Tools (3.3.0)**
  * Added MFA support
  * Add runtime config setting to roll forward to major versions of .NET Core if 2.X is not installed.
* **Amazon.ECS.Tools (3.1.0)**
  * Added MFA support
  * Add runtime config setting to roll forward to major versions of .NET Core if 2.X is not installed.
* **Amazon.ElasticBeanstalk.Tools (3.2.0)**
  * Added MFA support
  * Add runtime config setting to roll forward to major versions of .NET Core if 2.X is not installed.


### Release 2019-05-02
* **Amazon.Lambda.Tools (3.2.3)**
    * Fixed issue filename or extension is too long when passing a large number of file arguments to the zip utility.

### Release 2019-04-19
* **Amazon.Lambda.Tools (3.2.2)**
    * Fixed issue with package not being able to installed on non-windows platforms.

### Release 2019-04-18
* **Amazon.Lambda.Tools (3.2.1)**
	* Removed ASP.NET Core version check. This is no longer needed now that the .NET Core SDK no longer sets the runtime version to the latest patched version that is installed on the machine that is creating the deployment package.
	* Fixed issue of not handling embedded node.js or python code in CloudFormation template.

### Release 2019-03-25
* **Amazon.Lambda.Tools (3.2.0)**
    * Added support for using .NET Core runtime package stores as Lambda layers. For a full description checkout the [.NET Lambda Layer docs](https://github.com/aws/aws-extensions-for-dotnet-cli/blob/master/docs/Layers.md).
    * Fixed issue with Windows line ending when deploy a Custom Runtime Lambda function.

### Release 2019-03-18
* **Amazon.Lambda.Tools (3.1.4)**
    * Make `--framework` switch optional. If it is not set then the project file will be inspected to determine framework.
	* Add deprecation warning message when using .NET Core 2.0 Lambda runtime.

### Release 2019-03-07
* **Amazon.ElasticBeanstalk.Tools (3.1.0)**
  * Pull request [#55](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/55) add **package** command to package an application as a zip file to later be deployed to Beanstalk. Thanks [Anthony Abate](https://github.com/abbotware)
  * Pull Request [#57](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/57) allows string parameters to point to environment variables. Thanks [Anthony Abate](https://github.com/abbotware)
    * For example the in the following **aws-beanstalk-tools-defaults.json** file the Beanstalk application name will come from the EB_APP 
   environment variable and the environment name will come from EB_ENV.
```json
{                                                                                 
    "application" : "$(EN_APP)",                                                        
    "environment" : "$(EB_ENV)"
}                                                                                 
```

### Release 2019-03-06
* **Amazon.Lambda.Tools (3.1.3)**
    * Changes to get this tool ready for the upcoming ability to use a custom .NET Core runtimes. 
Follow [#405](https://github.com/aws/aws-lambda-dotnet/issues/405) GitHub issue for the upcoming **Amazon.Lambda.RuntimeSupport** library.
        * Zipping the deployment bundle on Windows was switch to use a new Go executable to 
allow setting linux file permisisons. The Go executable is distributed with this tool so this change should be transparent to users.
    * Fixed issue with config files specified with the `--config-file` not being found when the `--project-location` 
switch was used.

### Release 2019-01-04
* **Amazon.Lambda.Tools (3.1.2)**
    * Fixed issue with failed deployments when CloudFormation template was greater then 50,000 .
    * Added support for CAPABILITY_AUTO_EXPAND for deploy-serverless command.

### Release 2018-11-19
* **Amazon.Lambda.Tools (3.1.1)**
    * Fix issue looking for Lambda runtime from CloudFormation template when runtime specified in the Globals section.
* **Amazon.ElasticBeanstalk.Tools (3.0.1)**
    * Pull request [#43](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/43), fixing issue with wrong directory separater when creating zip file. Thanks [bartoszsiekanski](https://github.com/bartoszsiekanski)

### Release 2018-10-12
* **Amazon.Lambda.Tools (3.1.0)**
    * Updated the `deploy-serverless` and `package-ci` command to support deploying multiple projects.
Each `AWS::Lambda::Function` or `AWS::Serverless::Function` can now point to different .NET projects locally using the CloudFormation resource's code properties. 
If the code property is not set then the current directory assumed.
    * Pull request [#39](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/39), fixing issue related to yaml templates containing intrinsic functions in the short form. Thanks to [Albert Szilvasy](https://github.com/szilvaa)
    * Added `--tags` property to `deploy-serverless` command to apply AWS Tags to the CloudFormation stack and the resources the stack creates.

### Release 2018-09-11
* **Amazon.Lambda.Tools (3.0.1)**
    * Fixed issue incorrectly checking if deployment command was being executed in a project directory when using a precompiled package zip file.

### Release 2018-09-10
* **Amazon.Lambda.Tools (3.0.0)**
    * Switch to Global Tool.
    * Made the **--apply-defaults** switch **obsolete**. Defaults from config file are now always applied.
    * Added new **--append-environment-variables** switch to add new environment variables without overwriting existing environment variables.
    * Added validation that if a config file is explicitly set and the file can not be found then throw an exception
    * Improve error reporting when failed to parse command line arguments.
    * Pull request [#29](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/29) changing publishing RID to rhel.7.2-x64 the closest match to Amazon Linux.
    * **PreserveCompilationContext** in the **--msbuild-parameters** switch overrides this tool's default behavior of setting /p:PreserveCompilationContext=false.
    * Fixed bug incorrectly executing chmod on a file with spaces in the name. 
    * Add ability to pass AWS credentials using the switches --aws-access-key-id, --aws-secret-key and --aws-session-token
* **Amazon.ECS.Tools (3.0.0)**
    * Switch to Global Tool.
* **Amazon.ElasticBeanstalk.Tools (3.0.0)**
    * Switch to Global Tool.

### Release 2018-07-09
* **Amazon.Lambda.Tools (2.2.0)**
    * Added support for the .NET Core 2.1 AWS Lambda runtime.
    * Fixed issue with not correct determining CloudFormation parameters when using YAML.
    * Fixed issue handling CloudFormation parameter renames.
* **Amazon.ECS.Tools (1.2.0)**
    * Improve detection for when the `docker build` command should run from the solution folder.
    * Added new switch `--docker-build-working-dir` to set the directory where `docker build` should run. This is useful when this tool can't detect whether the build should run from the project or the solution.
    * Added new switch `--docker-build-options` to pass additional options to the `docker build` command.

### Relesae 2018-05-29
* **Amazon.Lambda.Tools (2.1.4)**
    * Change AWS credential lookup logic to continue searching if the profile specified cannot be found. This allows 
easier switching between development environment and CI/CD environments.
    * Pull request [#11](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/11). Fixed issue with `deploy-serverless` breaking Swagger definitions in yaml.
    *  Fixed issue when validating version of Microsoft.AspNetCore.All for F# project files.
    *  Switch to warning when validating S3 bucket in same region as target deployment region if the region can not be determined. This is commonly due to lack of 
S3 permission to get the region for a bucket.

* **Amazon.ECS.Tools (1.1.5)**
    * Change AWS credential lookup logic to continue searching if the profile specified cannot be found. This allows 
easier switching between development environment and CI/CD environments.
    * Add `--publish-options` switch to allow passing additional parameters to the `dotnet publish` command.
* **Amazon.ElasticBeanstalk.Tools (1.1.4)**
    * Change AWS credential lookup logic to continue searching if the profile specified cannot be found. This allows 
easier switching between development environment and CI/CD environments.
    * Add `--publish-options` switch to allow passing additional parameters to the `dotnet publish` command.
    * Fixed issue with instance profile not being persisted when the flat to save configuration is set.

### Release 2018-04-30
* **Amazon.Lambda.Tools (2.1.2)**
    * If a CloudFormation parameter's NoEcho property is to true then output **** when displaying the template parameters set for the deployment.
    * Stop persisting **--stack-wait** switch when saving config file because it will always be set to false when called from Visual Studio.

### Release 2018-03-26
* **Amazon.Lambda.Tools (2.1.2)**
  * Moved here from the [AWS Lambda for .NET Core](https://github.com/aws/aws-lambda-dotnet) repository
* **Amazon.ElasticBeanstalk.Tools (1.1.3)**
  * Fixed issue with setting the IAM service role for new Beanstalk environments
  * Fixed issue with Beanstalk Solution Stack not being persisted in defaults file.
  * All commands can now persist the settings used with the **-pcfg true** flag.
* **Amazon.ECS.Tools (1.1.4)**
  * All commands can now persist the settings used with the **-pcfg true** flag.

### Release 2018-03-13

* **Amazon.ECS.Tools (1.1.3)**
  * Fixed issue detecting docker build working directory for latest VS 2017 created Dockerfile.
  * Fixed issue not detected when a cluster should be created because of inactive cluster with the same name. 
* **Amazon.ElasticBeanstalk.Tools (1.1.2)**
  * Pull request [#8](https://github.com/aws/aws-extensions-for-dotnet-cli/pull/8). Add **--version-label** switch to set a version label when deploying. Thanks to [kalexii](https://github.com/kalexii).

### Release 2018-02-25

* **Amazon.ECS.Tools (1.1.2)**
    * Fixed issue with docker tag incorrectly being written out to the aws-beanstalk-tools-defaults.json.
    * Fixed error handling when searching for the solution file for the project being deployed.

### Release 2018-02-14

* **Amazon.ECS.Tools (1.1.1)**
    * Added dependency to **AWSSDK.SecurityToken** to support profiles that use assume role features of Security Token Service.
    * Allow task defintion cpu and memory to be read from **aws-ecs-tools-defaults.json** either as a string or number. Previously only string was supported.
    * Fixed issue with reading desired count from **aws-ecs-tools-defaults.json**.
    * Fixed issue persisting last settings for scheduled task to **aws-ecs-tools-defaults.json**.

* **Amazon.ElasticBeanstalk.Tools (1.1.1)**
    * Added dependency to **AWSSDK.SecurityToken** to support profiles that use assume role features of Security Token Service.

### Release 2018-02-02
* **Amazon.ElasticBeanstalk.Tools (1.1.0)**
    * Add **--enable-xray** switch to enable the AWS X-Ray daemon in the environment

### Release 2018-01-21
* **Amazon.ECS.Tools (1.1.0)**
  * Use default subnets if no subnets provided for Fargate deployments
  * Inspect Docker file to see if **dotnet publish** needs to run before **docker build**
  * If redeploying to an existing Fargate service reuse network configuration if one is not provided
  * Fix issue with docker image name being asked for multiple times
* **Amazon.ElasticBeanstalk.Tools (1.0.1)**
    * Set description for NuGet package

### Release 2017-11-29
* **Amazon.ECS.Tools (1.0.0)**
    * Added command **deploy-service**
    * Added command **deploy-task**
    * Added command **deploy-task**
    * Added command **deploy-scheduled-task**
    * Added command **push-image**
* **Amazon.ElasticBeanstalk.Tools (1.0.0)**
    * Added command **deploy-environment**
    * Added command **delete-environment**
    * Added command **list-environments**
