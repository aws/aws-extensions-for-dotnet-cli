# Layers for .NET Core Lambda Functions

**[Lambda Layers](https://docs.aws.amazon.com/lambda/latest/dg/configuration-layers.html)** allow you to 
provide additional code and content to your Lambda function. A layer is a zip file that is extracted
into the **/opt** directory in the Lambda compute environment.

This document describes the specific use cases the **Amazon.Lambda.Tools** .NET Core global tool supports in 
creating Layers and exposing them to your .NET Core Lambda function. **Currently this support is not included in the AWS Toolkit for Visual Studio**.

## The Quick Tutorial

To create a layer of the dependent NuGet packages of a .NET Core Lambda function execute the 
following command in the function's directory.
```bash
dotnet lambda publish-layer <layer-name> --layer-type runtime-package-store --s3-bucket <bucket-name>
```

The output of this command will be the arn of the new layer as shown below where the
new layer arn is **arn:aws:lambda:us-west-2:123412341234:layer:doc-test-layer:1**.

```bash
...
...
Create zip file of package store directory
... zipping: doc-test-layer-636833019503061325\x64\netcoreapp2.1\artifact.xml
... zipping: doc-test-layer-636833019503061325\x64\netcoreapp2.1\amazon.lambda.core\1.0.0\lib\netstandard1.3\Amazon.Lambda.Core.dll
... zipping: doc-test-layer-636833019503061325\x64\netcoreapp2.1\amazon.lambda.serialization.json\1.4.0\lib\netstandard1.3\Amazon.Lambda.Serialization.Json.dll
... zipping: doc-test-layer-636833019503061325\x64\netcoreapp2.1\newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll
Uploading layer input zip file to S3
Uploading to S3. (Bucket: normj-west2 Key: doc-test-layer-636833019502996089/packages.zip)
... Progress: 53%
... Progress: 100%
Upload complete to s3://normj-west2/doc-test-layer-636833019502996089/packages.zip
Layer publish with arn arn:aws:lambda:us-west-2:123412341234:layer:doc-test-layer:1
```

To use the new layer when you deploy set the **--function-layers** switch for the **deploy-function** command
with the layer arn returned from the publish-layer command.

```bash
dotnet lambda deploy-function <function-name> --function-layers <layer-arn>
```
The layer can also be set in the **aws-lambda-tools-defaults.json** file with the **function-layers** property. 
This avoids having to pass in the layer on the commandline for every redeployment.

You should see lines similar to this in the start of the output indicating the layer was used for publishing.
```bash
...

Inspecting Lambda layers for runtime package store manifests                                                                  
... arn:aws:lambda:us-west-2:123412341234:layer:doc-test-layer:1: Downloaded package manifest for runtime package store layer 
Executing publish command                                                                                                     
...
```

To use the layer with the **deploy-serverless** command set the layers property in the serverless.template.
```json
  "Resources" : {

    "TheFunction" : {
      "Type" : "AWS::Serverless::Function",
      "Properties": {
        "Handler": "TestLayerServerless::TestLayerServerless.Functions::ToUpper",
        "Runtime": "dotnetcore2.1",
        "CodeUri": "",
        "MemorySize": 256,
        "Timeout": 30,
        "Role": null,
        "Policies": [ "AWSLambdaBasicExecutionRole" ],
        "Layers" : [ "arn:aws:lambda:us-west-2:123412341234:layer:doc-test-layer:1" ],
        "Events": {
        }
      }
    }

  },

```


## What is a Layer Type?

During the tutorial above when the **publish-layer** command was executed the 
switch `--layer-type runtime-package-store` was used. This controls how the **publish-layer** command 
will create the layer.

Currently the only valid value for **--layer-type** is **runtime-package-store**. Other options may be
added in the future as .NET Core evolves and the tooling supports other use cases. 
This is a required parameter for future proofing of the publish-layer command.

### What is Runtime Package Store?

One of the major goals of Lambda layers was to bundle up your function's dependencies separately so
that the dependencies didn't have to be upload every time you changed your code and deployed a new 
Lambda function version.

.NET Core is not designed to simply load assemblies from a directory outside of the deployment bundle.
All dependencies have to be known about during the `dotnet publish` point and the .NET Core runtime
needs to know about all the directories with assemblies at start up time.

To solve these challenges for .NET Core the AWS .NET tooling uses .NET Core's 
**[Runtime package stores](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)** feature.

Runtime package stores were added to .NET Core 2.0. In fact .NET Core 2.0 used the default runtime package 
store to distribute the ASP<span>.NET Core 2.0 packages.

Custom runtime package stores can be created using the `dotnet store` command which the **publish-layer** 
command relies on.

#### Store Input Manifest

The `dotnet store` command takes in a manifest identifying the NuGet packages to put into the store.
The manifest is a msbuild project file like a csproj file of a Lambda Function project. 

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="1.0.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.Json" Version="1.4.0" />
    <PackageReference Include="AWSSDK.S3" Version="3.3.31.10" />
  </ItemGroup>

</Project>
```

When the manifest is read all NuGet packages listed in **PackageReference** elements along with their dependencies will be copied to the store.

The manifest can be set for the **publish-layer** command with the `--package-manifest` switch. If the
switch is not set then the command will look for a csproj of fsproj in the current directory.


#### Store Output Manifest

When a runtime package store is created by the `dotnet store` command an **artifact.xml** file
is written which identifies all of the NuGet packages in the store. This file must be passed
into the `dotnet publish` command which **deploy-function** and **deploy-serverless** uses.
The `dotnet publish` command uses this file to filter out packages in the store from the 
output publish folder. When the deployment bundle is created by zipping up the publish folder it won't
contain any of the assemblies from the NuGet packages in the store.

The **publish-layer** command will upload the artifact.xml file to S3. The file is retrieved from S3 during 
the **deploy-function**, **deploy-serverless** and **package-ci** commands and passed down in the `dotnet publish` command to filter out the NuGet packages
that are in the layer.

**Warning:**  If the artifact.xml file is deleted from S3 then the **deploy-function** and **deploy-serverless** commands will no longer
be able to use the layer during publishing. The artifact.xml file **does not** affect any deployed functions.

#### Layer Description

To support the workflow of having the artifact.xml file, created by the **publish-layer** command, be
accessible to the **deploy-function**, **deploy-serverless** and **package-ci** commands the description of the layer is used to store a JSON document.
The document identifies the location of the artifact.xml file in S3 and the sub directory under **/opt**
directory the packages will be placed in the Lambda compute environment.

*Note: The layer description field has a limited size. The JSON property names are kept short making the
document less readable. The description filled is large enough to cover common name lengths but
it is recommend to not use long bucket names, layer names or S3 prefixes*

#### Configuring the Lambda .NET Core Runtime

The runtime package store layer is placed under the `/opt/runtime-package-store` directory by default which can be changed using the `--opt-directory` switch for the **publish-layer** command. The function
deployment bundle is placed under the `/var/task` directory which is the working directory for the Lambda process.

The .NET Core runtime in Lambda needs to be configured to look for the NuGet assemblies from the layer.
This is done by setting the **DOTNET_SHARED_STORE** environment variable to the paths of where the 
runtime package store layer will be extracted. If multiple layers are used with different opt directories then each directory should be added to the environment variable separated by colons. A trailing `/` is required for the **DOTNET_SHARED_STORE** .NET Core environment variable.

The **deploy-function**, **deploy-serverless** and **package-ci** commands will take care of setting the **DOTNET_SHARED_STORE** environment variable.

The **package** command will output the value that **DOTNET_SHARED_STORE** must be set to when the
deployment bundle is created by the package command. When the deploymnet bundle is deployed ensure the **DOTNET_SHARED_STORE** environment variable is set to the value the package command outputted.

#### Optimizing Packages

A feature of a runtime package store is the .NET assemblies placed into the the store can be optimized for
the target runtime by pre-jitting the assemblies. Pre-jitting is the process
of compiling the platform agnostic machine code of an assembly, known as MSIL, into native 
machine code. Without pre-jitting the assemblies are compiled into native machine code when they are
first loaded into the .NET Core Process. Enabling the optimization can significantly reduce cold
start times in Lambda.

**In order to create an optimized runtime package store layer you must run the publish-layer command in
an Amazon Linux environment.** Attempting to create an optimized runtime package store layer on Windows 
or macOS will result in an error. If creating the layer on Linux be sure the distribution is 
**Amazon Linux**.

To tell the **publish-layer** command on to optimize the layer set the 
`--enable-package-optimization` switch to `true`.

#### ASP<span>.NET Core Layers

In an ASP<span>.Core project file the root Project element sets the Sdk attribute to **Microsoft<span>.NET<span>.Sdk<span>.Web** and there is a **PackageReference** to `Microsoft.AspNetCore.App`. These settings inform the `dotnet publish` command to not include ASP.NET Core dependencies.

If an ASP<span>.NET Core project is passed to the `dotnet store` command as the input manifest the command will fail. The ASP<span>.NET Core project has to be converted to a **Microsoft<span>.NET<span>.Sdk** project before being used with `dotnet store`. The `publish-layer` command will automatically perform the transformation if it detects the **Sdk** attribute is set to **Microsoft<span>.NET<span>.Sdk<span>.Web**.

For example if the following project was passed to `publish-layer`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.App" Version="2.1.5" />
    <PackageReference Include="AWSSDK.S3" Version="3.3.31.15" />
    <PackageReference Include="AWSSDK.Extensions.NETCore.Setup" Version="3.3.6" />
    <PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="3.0.1" />
  </ItemGroup>
</Project>
```

It will be converted into the following manifest:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.App"/>
    <PackageReference Update="Microsoft.NETCore.App" Publish="false"/>

    <PackageReference Include="AWSSDK.S3" Version="3.3.31.15" />
    <PackageReference Include="AWSSDK.Extensions.NETCore.Setup" Version="3.3.6" />
    <PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="3.0.1" />
  </ItemGroup>
</Project>
```
By removing the version field from `Microsoft.AspNetCore.App` and adding the `Microsoft.NETCore.App` with **Publish** set to **false** the `dotnet store` command will know to not add any NuGet packages that are include with the meta package `Microsoft.AspNetCore.App` into the runtime package store. This is also true for the ASP<span>.NET Core dependencies that **Amazon.Lambda.AspNetCoreServer** depends on.

**WARNING:** The .NET Core 2.1 SDK has a reported issue that causes even the transformed manifest to not work correctly. The issue was fixed with the .NET Core 2.2 SDK. To create an ASP.NET Core 2.1 layer both .NET Core 2.1 and .NET Core 2.2 SDKs must be installed. If the .NET Core 2.2 SDK is not installed when creating an ASP<span>.NET Core layer the `publish-layer` command will fail.

### Managing Layers

As well as the **publish-layer** command there are 4 other layer commands to help manage layers from **Amazon.Lambda.Tools**.

| Command | Description |
| ------- | ----------- |
| list-layers | List the available layers|
| list-layer-versions | List the versions for a layer |
| get-layer-version | Gets the details of a version layer including the manifest for a runtime package store layer. |
| delete-layer-version | Deletes a version of a layer |


### FAQ

#### Can I use the package command with layers?

The **package** command is used to create the Lambda deployment bundle but not deploy it.
This is commonly used when the actual deployment is done through some other tool/process.

To use layers the `--function-layers` switch must be set when executing the package command. This is needed so that the
store manifest can be passed to the `dotnet publish` command which will filter 
out the NuGet packages from the layer.

The **DOTNET_SHARED_STORE** must be set with whatever tool/process is deploying the 
deployment bundle. The package command will output the value **DOTNET_SHARED_STORE**
must be set to. By default **DOTNET_SHARED_STORE** will be to `/opt/runtime-package-store/`

#### Can I create a common layer to share with multiple projects?

Yes, and this can be really useful to have a common layer created on Amazon Linux so it can be optimized. Then the
the layer arn can be shared with the other developers using Windows and Macs and get the benefit of the optimized layer.

In the tutorial above a Lambda project file was used as the manifest but that is not a requirement. An example workflow
could be a developer creates a file called **common-core.xml** that list the NuGet packages they expects their team to use
across the org.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="1.0.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.Json" Version="1.4.0" />
    <PackageReference Include="AWSSDK.Core" Version="3.3.31.2" />
    <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.3.17" />
    <PackageReference Include="AWSSDK.S3" Version="3.3.31.10" />
  </ItemGroup>

</Project>
```

The developer launches an EC2 Amazon Linux instance and installs the Amazon.Lambda.Tools
```bash
dotnet tool install -g Amazon.Lambda.Tools
```

Then creates the layer using the **publish-layer** command.
```bash
dotnet lambda publish-layer common-core --package-manifest ./common-core.xml --layer-type runtime-package-store --framework netcoreapp2.1 --s3-bucket <bucket-name> --enable-package-optimization true
```

Once the layer is created the developer shares the new layer arn to the rest of the developers to set for the `--function-layers` switch or in their **aws-lambda-tools-defaults.json** file.

#### Should I create a layer with every possible dependency?

Large layers can affect cold start time as they contribute to the amount of bits that have to be downloaded to the Lambda compute environment. Best practice
would be to use layers to store the common dependencies your Lambda functions will use. Avoid creating layers with every possible dependency. For example
creating a layer with all 100+ NuGet packages of the AWS SDK for .NET when you are likely only using a handful in Lambda is not advised.    

#### Can I use runtime package store layers with other tools besides Amazon.Lambda.Tools?

Currently only **Amazon.Lambda.Tools** has the .NET specific logic to manage the artifact.xml file and DOTNET_SHARED_STORE environment variable.
Eventually the version of Amazon.Lambda.Tools that ships with AWS Toolkit for Visual Studio will be updated to include the 
layer functionality.

#### How do I see what packages are in a runtime package store layer?

The `get-layer-version` command will give the full details for a layer version. If the layer version is
a runtime package store layer the command will also display the manifest for the runtime package store.

For example:
```bash
$ dotnet lambda get-layer-version arn:aws:lambda:us-west-2:123412341234:layer:common-core:1
Amazon Lambda Tools for .NET Core applications (3.2.0)
Project Home: https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet
	
Layer ARN:               arn:aws:lambda:us-west-2:123412341234:layer:common-core
Version Number:          1
Created:                 1/17/19 2:02 PM
License Info:            
Compatible Runtimes:     dotnetcore2.1
Layer Type:              .NET Runtime Package Store

.NET Runtime Package Store Details:
Manifest Location:       s3://normj-west2/common-core-636833593081300649/artifact.xml
Packages Optimized:      True
Packages Directory:      /opt/runtime-package-store

Manifest Contents
-----------------------
<StoreArtifacts>
  <Package Id="Amazon.Lambda.Core" Version="1.0.0" />
  <Package Id="Amazon.Lambda.Serialization.Json" Version="1.4.0" />
  <Package Id="Newtonsoft.Json" Version="9.0.1" />
  <Package Id="AWSSDK.Core" Version="3.3.31.2" />
  <Package Id="AWSSDK.DynamoDBv2" Version="3.3.17" />
  <Package Id="AWSSDK.S3" Version="3.3.31.10" />
</StoreArtifacts>

```

#### Why do I get the error message "The dotnet CLI failed to start with the provided deployment package."?

If the CloudWatch Log has a message like the following it most likely means the **DOTNET_SHARED_STORE** environment variable 
is not set correctly on the Lambda function.

```bash
2019-01-17 13:24:08:   An assembly specified in the application dependencies manifest (TheFunction.deps.json) was not found:
2019-01-17 13:24:08:     package: 'AWSSDK.Core', version: '3.3.31.2'
2019-01-17 13:24:08:     path: 'lib/netstandard1.3/AWSSDK.Core.dll'
2019-01-17 13:24:08:   This assembly was expected to be in the local runtime store as the application was published using the following target manifest files:
2019-01-17 13:24:08:     tmpD01A.tmp
```

The **deploy-function** command will set this value so the easiest way to fix the issue may be to redeploy the function.

The format of **DOTNET_SHARED_STORE** is `/opt/<layer1-directory>/:/opt/<layer2-directory>/` with
the colon used to separate the different layer paths. The trailing '/' is required for each layer path.
