### Release 2019-01-04
* **Amazon.Lambda.Tools (3.1.2)**
    * Fixed issue with failed deployments when CloudFormation template was greater then 50,000 .
    * Added support for CAPABILITY_AUTO_EXPAND for deploy-serverless command.

### Release 2018-11-19
* **Amazon.Lambda.Tools (3.1.1)**
    * Fix issue looking for Lambda runtime from CloudFormation template when runtime specified in the Globals section.
* **Amazon.ElasticBeanstalk.Tools (3.0.0)**
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
    * Fixed issue incorrectly checking being executed in a project directory when using a precompiled package zip file.

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
