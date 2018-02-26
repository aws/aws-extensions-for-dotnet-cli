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
