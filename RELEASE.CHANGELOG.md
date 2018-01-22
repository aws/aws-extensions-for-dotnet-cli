### Release 2018-01-21
* **Amazon.ECS.Tools (1.0.0)**
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
