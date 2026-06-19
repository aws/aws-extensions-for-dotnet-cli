// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Lambda.Tools.Commands
{
    public class PushLambdaImageResult
    {
        public bool Success { get; set; }
        public Exception LastException { get; set; }
        public string ImageUri { get; set; }
    }
}
