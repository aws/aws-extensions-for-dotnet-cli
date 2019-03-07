using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using Amazon.ElasticBeanstalk.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Moq;

namespace Amazon.ElasticBeanstalk.Tools.Test
{
    public class DeployTests
    {
        [Fact]
        public async Task CreateEnvironmentTest()
        {
            var application = "TestApp";
            var environment = "TestEnv";
            var solutionStack = "TestWindowsStack";
            var iamProfile = "arn:aws:fake-profile";

            var mockS3Client = new Mock<IAmazonS3>();

            var calls = new Dictionary<string, int>();
            Action<string> addCall = x =>
            {
                if (calls.ContainsKey(x))
                    calls[x] = calls[x] + 1;
                else
                    calls[x] = 1;

            };
            var mockEbClient = new Mock<IAmazonElasticBeanstalk>();
            mockEbClient.Setup(client => client.DescribeApplicationsAsync(It.IsAny<DescribeApplicationsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeApplicationsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationNames[0]);
                })
                .Returns((DescribeApplicationsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeApplicationsAsync");
                    return Task.FromResult(new DescribeApplicationsResponse());
                });
            mockEbClient.Setup(client => client.CreateStorageLocationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) =>
                {
                    addCall("CreateStorageLocationAsync");
                    return Task.FromResult(new CreateStorageLocationResponse {S3Bucket = "TestBucket" });
                });
            mockEbClient.Setup(client => client.DescribeEventsAsync(It.IsAny<DescribeEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeEventsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);
                })
                .Returns((DescribeEventsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEventsAsync");
                    var response = new DescribeEventsResponse
                    {
                        Events = new List<EventDescription>
                        {
                            new EventDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                Message = "Dummy Message",
                                EventDate = DateTime.Now
                            }
                        }
                    };
                    return Task.FromResult(response);
                });
            mockEbClient.Setup(client => client.CreateApplicationAsync(It.IsAny<CreateApplicationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateApplicationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                })
                .Returns((CreateApplicationRequest r, CancellationToken token) =>
                {
                    addCall("CreateApplicationAsync");
                    return Task.FromResult(new CreateApplicationResponse());
                });
            mockEbClient.Setup(client => client.CreateEnvironmentAsync(It.IsAny<CreateEnvironmentRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateEnvironmentRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);
                    Assert.Equal(solutionStack, request.SolutionStackName);

                    var iamSetting = request.OptionSettings.FirstOrDefault(x => string.Equals(x.OptionName, "IamInstanceProfile") && string.Equals(x.Namespace, "aws:autoscaling:launchconfiguration"));
                    Assert.Equal(iamSetting.Value, iamProfile);

                    var xraySetting = request.OptionSettings.FirstOrDefault(x => string.Equals(x.OptionName, "XRayEnabled") && string.Equals(x.Namespace, "aws:elasticbeanstalk:xray"));
                    Assert.Equal(xraySetting.Value.ToLower(), "true");
                })
                .Returns((CreateEnvironmentRequest r, CancellationToken token) =>
                {
                    addCall("CreateEnvironmentAsync");
                    return Task.FromResult(new CreateEnvironmentResponse());
                });
            mockEbClient.Setup(client => client.DescribeEnvironmentsAsync(It.IsAny<DescribeEnvironmentsRequest>(), It.IsAny<CancellationToken>()))
                .Returns((DescribeEnvironmentsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEnvironmentsAsync");

                    return Task.FromResult(new DescribeEnvironmentsResponse
                    {
                        Environments = new List<EnvironmentDescription>
                        {
                            new EnvironmentDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                DateCreated = DateTime.Now.AddMinutes(-1),
                                DateUpdated = DateTime.Now,
                                Status = EnvironmentStatus.Ready
                            }
                        }
                    });
                });


            var deployCommand = new DeployEnvironmentCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath, 
                new string[] { "-app", application, "-env", environment, "--solution-stack", solutionStack,
                    "--instance-profile", iamProfile, "--region", "us-moq-1", "--enable-xray", "true" });
            deployCommand.DisableInteractive = true;
            deployCommand.EBClient = mockEbClient.Object;
            deployCommand.S3Client = mockS3Client.Object;

            await deployCommand.ExecuteAsync();
            Assert.Null(deployCommand.LastToolsException);

            Assert.True(calls.ContainsKey("CreateApplicationAsync"));
            Assert.True(calls.ContainsKey("CreateEnvironmentAsync"));
        }

        [Fact]
        public async Task CreateEnvironmentWithPackageTest()
        {
            var application = "TestApp";
            var environment = "TestEnv";
            var solutionStack = "TestWindowsStack";
            var iamProfile = "arn:aws:fake-profile";

            var mockS3Client = new Mock<IAmazonS3>();

            var calls = new Dictionary<string, int>();
            Action<string> addCall = x =>
            {
                if (calls.ContainsKey(x))
                    calls[x] = calls[x] + 1;
                else
                    calls[x] = 1;

            };
            var mockEbClient = new Mock<IAmazonElasticBeanstalk>();
            mockEbClient.Setup(client => client.DescribeApplicationsAsync(It.IsAny<DescribeApplicationsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeApplicationsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationNames[0]);
                })
                .Returns((DescribeApplicationsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeApplicationsAsync");
                    return Task.FromResult(new DescribeApplicationsResponse());
                });
            mockEbClient.Setup(client => client.CreateStorageLocationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) =>
                {
                    addCall("CreateStorageLocationAsync");
                    return Task.FromResult(new CreateStorageLocationResponse { S3Bucket = "TestBucket" });
                });
            mockEbClient.Setup(client => client.DescribeEventsAsync(It.IsAny<DescribeEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeEventsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);
                })
                .Returns((DescribeEventsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEventsAsync");
                    var response = new DescribeEventsResponse
                    {
                        Events = new List<EventDescription>
                        {
                            new EventDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                Message = "Dummy Message",
                                EventDate = DateTime.Now
                            }
                        }
                    };
                    return Task.FromResult(response);
                });
            mockEbClient.Setup(client => client.CreateApplicationAsync(It.IsAny<CreateApplicationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateApplicationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                })
                .Returns((CreateApplicationRequest r, CancellationToken token) =>
                {
                    addCall("CreateApplicationAsync");
                    return Task.FromResult(new CreateApplicationResponse());
                });
            mockEbClient.Setup(client => client.CreateEnvironmentAsync(It.IsAny<CreateEnvironmentRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateEnvironmentRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);
                    Assert.Equal(solutionStack, request.SolutionStackName);

                    var iamSetting = request.OptionSettings.FirstOrDefault(x => string.Equals(x.OptionName, "IamInstanceProfile") && string.Equals(x.Namespace, "aws:autoscaling:launchconfiguration"));
                    Assert.Equal(iamSetting.Value, iamProfile);

                    var xraySetting = request.OptionSettings.FirstOrDefault(x => string.Equals(x.OptionName, "XRayEnabled") && string.Equals(x.Namespace, "aws:elasticbeanstalk:xray"));
                    Assert.Equal(xraySetting.Value.ToLower(), "true");
                })
                .Returns((CreateEnvironmentRequest r, CancellationToken token) =>
                {
                    addCall("CreateEnvironmentAsync");
                    return Task.FromResult(new CreateEnvironmentResponse());
                });
            mockEbClient.Setup(client => client.DescribeEnvironmentsAsync(It.IsAny<DescribeEnvironmentsRequest>(), It.IsAny<CancellationToken>()))
                .Returns((DescribeEnvironmentsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEnvironmentsAsync");

                    return Task.FromResult(new DescribeEnvironmentsResponse
                    {
                        Environments = new List<EnvironmentDescription>
                        {
                            new EnvironmentDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                DateCreated = DateTime.Now.AddMinutes(-1),
                                DateUpdated = DateTime.Now,
                                Status = EnvironmentStatus.Ready
                            }
                        }
                    });
                });

            var outputPackage = Path.GetTempFileName().Replace(".tmp", ".zip");
            var packageCommand = new PackageCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath,
                new string[] { "--output-package", outputPackage, "--iis-website", "The WebSite", "--app-path", "/child" });
            packageCommand.DisableInteractive = true;
            await packageCommand.ExecuteAsync();


            var deployCommand = new DeployEnvironmentCommand(new ConsoleToolLogger(), Path.GetTempPath(),
                new string[] { "-app", application, "-env", environment, "--solution-stack", solutionStack,
                    "--instance-profile", iamProfile, "--region", "us-moq-1", "--enable-xray", "true",
                    "--package", outputPackage});
            deployCommand.DisableInteractive = true;
            deployCommand.EBClient = mockEbClient.Object;
            deployCommand.S3Client = mockS3Client.Object;

            await deployCommand.ExecuteAsync();
            Assert.Null(deployCommand.LastToolsException);

            Assert.True(calls.ContainsKey("CreateApplicationAsync"));
            Assert.True(calls.ContainsKey("CreateEnvironmentAsync"));
        }

        [Fact]
        public async Task UpdateEnvironmentTest()
        {
            var application = "TestApp";
            var environment = "TestEnv";
            var solutionStack = "TestWindowsStack";
            var iamProfile = "arn:aws:fake-profile";

            var mockS3Client = new Mock<IAmazonS3>();

            var calls = new Dictionary<string, int>();
            Action<string> addCall = x =>
            {
                if (calls.ContainsKey(x))
                    calls[x] = calls[x] + 1;
                else
                    calls[x] = 1;

            };
            var mockEbClient = new Mock<IAmazonElasticBeanstalk>();
            mockEbClient.Setup(client => client.DescribeApplicationsAsync(It.IsAny<DescribeApplicationsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeApplicationsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationNames[0]);
                })
                .Returns((DescribeApplicationsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeApplicationsAsync");
                    return Task.FromResult(new DescribeApplicationsResponse
                    {
                        Applications = new List<ApplicationDescription>
                        {
                            new ApplicationDescription
                            {
                                ApplicationName = application
                            }
                        }
                    });
                });
            mockEbClient.Setup(client => client.CreateStorageLocationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) =>
                {
                    addCall("CreateStorageLocationAsync");
                    return Task.FromResult(new CreateStorageLocationResponse { S3Bucket = "TestBucket" });
                });
            mockEbClient.Setup(client => client.DescribeEventsAsync(It.IsAny<DescribeEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DescribeEventsRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);
                })
                .Returns((DescribeEventsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEventsAsync");
                    var response = new DescribeEventsResponse
                    {
                        Events = new List<EventDescription>
                        {
                            new EventDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                Message = "Dummy Message",
                                EventDate = DateTime.Now
                            }
                        }
                    };
                    return Task.FromResult(response);
                });
            mockEbClient.Setup(client => client.CreateApplicationAsync(It.IsAny<CreateApplicationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateApplicationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                })
                .Returns((CreateApplicationRequest r, CancellationToken token) =>
                {
                    addCall("CreateApplicationAsync");
                    return Task.FromResult(new CreateApplicationResponse());
                });
            mockEbClient.Setup(client => client.CreateEnvironmentAsync(It.IsAny<CreateEnvironmentRequest>(), It.IsAny<CancellationToken>()))
                .Returns((CreateEnvironmentRequest r, CancellationToken token) =>
                {
                    addCall("CreateEnvironmentAsync");
                    return Task.FromResult(new CreateEnvironmentResponse());
                });
            mockEbClient.Setup(client => client.UpdateEnvironmentAsync(It.IsAny<UpdateEnvironmentRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateEnvironmentRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(application, request.ApplicationName);
                    Assert.Equal(environment, request.EnvironmentName);

                    var xraySetting = request.OptionSettings.FirstOrDefault(x => string.Equals(x.OptionName, "XRayEnabled") && string.Equals(x.Namespace, "aws:elasticbeanstalk:xray"));
                    Assert.Equal(xraySetting.Value.ToLower(), "true");
                })
                .Returns((UpdateEnvironmentRequest r, CancellationToken token) =>
                {
                    addCall("UpdateEnvironmentAsync");
                    return Task.FromResult(new UpdateEnvironmentResponse());
                });
            mockEbClient.Setup(client => client.DescribeEnvironmentsAsync(It.IsAny<DescribeEnvironmentsRequest>(), It.IsAny<CancellationToken>()))
                .Returns((DescribeEnvironmentsRequest r, CancellationToken token) =>
                {
                    addCall("DescribeEnvironmentsAsync");

                    return Task.FromResult(new DescribeEnvironmentsResponse
                    {
                        Environments = new List<EnvironmentDescription>
                        {
                            new EnvironmentDescription
                            {
                                ApplicationName = application,
                                EnvironmentName = environment,
                                DateCreated = DateTime.Now.AddMinutes(-1),
                                DateUpdated = DateTime.Now,
                                Status = EnvironmentStatus.Ready
                            }
                        }
                    });
                });


            var deployCommand = new DeployEnvironmentCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath,
                new string[] { "-app", application, "-env", environment, "--solution-stack", solutionStack,
                    "--instance-profile", iamProfile, "--region", "us-moq-1", "--enable-xray", "true" });
            deployCommand.DisableInteractive = true;
            deployCommand.EBClient = mockEbClient.Object;
            deployCommand.S3Client = mockS3Client.Object;

            await deployCommand.ExecuteAsync();
            Assert.Null(deployCommand.LastToolsException);

            Assert.False(calls.ContainsKey("CreateApplicationAsync"));
            Assert.False(calls.ContainsKey("CreateEnvironmentAsync"));
            Assert.True(calls.ContainsKey("UpdateEnvironmentAsync"));
        }
    }
}
