// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.AspNetCoreServer;

namespace TestNativeAotNet8WebApp;

public class LambdaFunction : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseLambdaServer()
            .UseContentRoot(Directory.GetCurrentDirectory());
    }
}