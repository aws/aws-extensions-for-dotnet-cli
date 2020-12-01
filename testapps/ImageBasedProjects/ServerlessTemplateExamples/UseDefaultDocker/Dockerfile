FROM public.ecr.aws/lambda/dotnet:5.0

WORKDIR /var/task

# This field should match where the .NET Lambda project built its output at. If this is being built as part of 
# Amazon.Lambda.Tools this source in the copy command below should match the `--docker-host-build-output-dir` switch.
COPY "bin/Release/net5.0/linux-x64/publish"  .

# Defining the entry  point in the Lambda function is optional. If not set then the base image
# will execute a shell script to determine the parameters for starting the .NET process.
# You can define the entry point in this file to avoid the base image running the shell script and potentially reduce 
# cold start times.
# ENTRYPOINT ["/var/lang/dotnet/dotnet", "exec", "--depsfile", "/var/task/TestSimpleImageProject.deps.json", "--runtimeconfig", "/var/task/TestSimpleImageProject.runtimeconfig.json", "/var/runtime/Amazon.Lambda.RuntimeSupport.dll"]