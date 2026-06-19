// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.PowerShellHost;

namespace TestPowerShellParallelTest
{
    public class Bootstrap : PowerShellFunctionHost
    {
        public Bootstrap() : base("TestPowerShellParallelTest.ps1")
        {
        }
    }
}
