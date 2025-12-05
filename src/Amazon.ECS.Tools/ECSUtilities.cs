using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.ECR.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.ECS.Model;
using Amazon.IdentityManagement.Model;
using Amazon.ECS.Tools.Commands;
using Amazon.CloudWatchLogs.Model;
using Amazon.CloudWatchLogs;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace Amazon.ECS.Tools
{
    public static class ECSUtilities
    {
        public static List<PlacementConstraint> ConvertPlacementConstraint(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;

            var list = new List<PlacementConstraint>();

            foreach(var value in values)
            {
                var tokens = value.Split('=');
                var constraint = new PlacementConstraint
                {
                    Type = tokens[0]
                };

                if (tokens.Length > 1)
                    constraint.Expression = tokens[1];

                list.Add(constraint);
            }

            return list;
        }

        public static List<PlacementStrategy> ConvertPlacementStrategy(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;

            var list = new List<PlacementStrategy>();

            foreach (var value in values)
            {
                var tokens = value.Split('=');
                var constraint = new PlacementStrategy
                {
                    Type = tokens[0]
                };

                if (tokens.Length > 1)
                    constraint.Field = tokens[1];

                list.Add(constraint);
            }

            return list;
        }

        public static async Task<string> ExpandImageTagIfNecessary(IToolLogger logger, IAmazonECR ecrClient, string dockerImageTag)
        {
            try
            {
                if (dockerImageTag.Contains(".amazonaws."))
                    return dockerImageTag;

                string repositoryName = dockerImageTag;
                if (repositoryName.Contains(":"))
                    repositoryName = repositoryName.Substring(0, repositoryName.IndexOf(':'));

                DescribeRepositoriesResponse describeResponse = null;
                try
                {
                    describeResponse = await ecrClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { repositoryName }
                    });
                }
                catch (Exception e)
                {
                    if (!(e is RepositoryNotFoundException))
                    {
                        throw;
                    }
                }

                // Not found in ECR, assume pulling Docker Hub
                if (describeResponse == null || describeResponse.Repositories == null || describeResponse.Repositories.Count == 0)
                {
                    return dockerImageTag;
                }

                var fullPath = describeResponse.Repositories[0].RepositoryUri + dockerImageTag.Substring(dockerImageTag.IndexOf(':'));
                logger?.WriteLine($"Determined full image name to be {fullPath}");
                return fullPath;
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error determining full repository path for the image {dockerImageTag}: {e.Message}", DockerToolsException.ECSErrorCode.FailedToExpandImageTag);
            }
        }

        public static async System.Threading.Tasks.Task EnsureClusterExistsAsync(IToolLogger logger, IAmazonECS ecsClient, string clusterName)
        {
            try
            {
                logger.WriteLine($"Checking to see if cluster {clusterName} exists");
                var response = await ecsClient.DescribeClustersAsync(new DescribeClustersRequest { Clusters = new List<string> { clusterName } });
                if (response.Clusters == null || response.Clusters.Count == 0 || string.Equals(response.Clusters[0].Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    logger.WriteLine($"... Cluster does not exist, creating cluster {clusterName}");
                    await ecsClient.CreateClusterAsync(new CreateClusterRequest { ClusterName = clusterName });
                }
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error ensuring ECS cluster {clusterName} exists: {e.Message}", DockerToolsException.ECSErrorCode.EnsureClusterExistsFail);
            }
        }

        public static async Task<string> CreateOrUpdateTaskDefinition(IToolLogger logger, IAmazonECS ecsClient, ECSBaseCommand command,
            TaskDefinitionProperties properties, string dockerImageTag, bool isFargate)
        {
            var ecsContainer = command.GetStringValueOrDefault(properties.ContainerName, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, true);
            var ecsTaskDefinition = command.GetStringValueOrDefault(properties.TaskDefinitionName, ECSDefinedCommandOptions.ARGUMENT_TD_NAME, true);

            try
            {
                DescribeTaskDefinitionResponse response = null;
                try
                {
                    response = await ecsClient.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest
                    {
                        TaskDefinition = ecsTaskDefinition
                    });
                }
                catch (Exception e)
                {
                    if (!(e is ClientException))
                    {
                        throw;
                    }
                }

                var registerRequest = new RegisterTaskDefinitionRequest()
                {
                    Family = ecsTaskDefinition
                };

                if (response == null || response.TaskDefinition == null)
                {
                    logger?.WriteLine("Creating new task definition");
                }
                else
                {
                    logger?.WriteLine("Updating existing task definition");

                    registerRequest.ContainerDefinitions = response.TaskDefinition.ContainerDefinitions;
                    registerRequest.Cpu = response.TaskDefinition.Cpu;
                    registerRequest.ExecutionRoleArn = response.TaskDefinition.ExecutionRoleArn;
                    registerRequest.Memory = response.TaskDefinition.Memory;
                    registerRequest.NetworkMode = response.TaskDefinition.NetworkMode;
                    registerRequest.PlacementConstraints = response.TaskDefinition.PlacementConstraints;
                    registerRequest.RequiresCompatibilities = response.TaskDefinition.RequiresCompatibilities;
                    registerRequest.TaskRoleArn = response.TaskDefinition.TaskRoleArn;
                    registerRequest.Volumes = response.TaskDefinition.Volumes;
                }

                var networkMode = command.GetStringValueOrDefault(properties.TaskDefinitionNetworkMode, ECSDefinedCommandOptions.ARGUMENT_TD_NETWORK_MODE, false);
                if (!string.IsNullOrEmpty(networkMode))
                {
                    registerRequest.NetworkMode = networkMode;
                }

                {
                    var volumes = command.GetJsonValueOrDefault(properties.TaskDefinitionVolumes, ECSDefinedCommandOptions.ARGUMENT_TD_VOLUMES);
                    if (volumes.HasValue)
                    {
                        registerRequest.Volumes = new List<Model.Volume>();
                        foreach (JsonElement item in volumes.Value.EnumerateArray())
                        {
                            var volume = new Amazon.ECS.Model.Volume();

                            if (item.TryGetProperty("host", out JsonElement host))
                            {
                                volume.Host = new HostVolumeProperties();
                                if (host.TryGetProperty("sourcePath", out JsonElement sourcePath))
                                    volume.Host.SourcePath = sourcePath.GetString();
                            }
                            if (item.TryGetProperty("efsVolumeConfiguration", out JsonElement efsConfig))
                            {
                                volume.EfsVolumeConfiguration = new EFSVolumeConfiguration();
                                if (efsConfig.TryGetProperty("fileSystemId", out JsonElement fileSystemId))
                                    volume.EfsVolumeConfiguration.FileSystemId = fileSystemId.GetString();
                                if (efsConfig.TryGetProperty("rootDirectory", out JsonElement rootDirectory))
                                    volume.EfsVolumeConfiguration.RootDirectory = rootDirectory.GetString();
                                if (efsConfig.TryGetProperty("transitEncryption", out JsonElement transitEncryption))
                                {
                                    var transitEncryptionValue = transitEncryption.GetString();
                                    if (transitEncryptionValue != null)
                                        volume.EfsVolumeConfiguration.TransitEncryption = EFSTransitEncryption.FindValue(transitEncryptionValue);
                                }
                                if (efsConfig.TryGetProperty("transitEncryptionPort", out JsonElement transitEncryptionPort) && transitEncryptionPort.ValueKind == JsonValueKind.Number)
                                {
                                    volume.EfsVolumeConfiguration.TransitEncryptionPort = transitEncryptionPort.GetInt32();
                                }
                                if (efsConfig.TryGetProperty("authorizationConfig", out JsonElement authConfig))
                                {
                                    volume.EfsVolumeConfiguration.AuthorizationConfig = new EFSAuthorizationConfig();
                                    if (authConfig.TryGetProperty("accessPointId", out JsonElement accessPointId))
                                        volume.EfsVolumeConfiguration.AuthorizationConfig.AccessPointId = accessPointId.GetString();
                                    if (authConfig.TryGetProperty("iam", out JsonElement iam))
                                    {
                                        var iamValue = iam.GetString();
                                        if (iamValue != null)
                                            volume.EfsVolumeConfiguration.AuthorizationConfig.Iam = EFSAuthorizationConfigIAM.FindValue(iamValue);
                                    }
                                }
                            }
                            if (item.TryGetProperty("name", out JsonElement name))
                                volume.Name = name.GetString();
                            
                            registerRequest.Volumes.Add(volume);
                        }
                    }
                }

                var taskIAMRole = command.GetStringValueOrDefault(properties.TaskDefinitionRole, ECSDefinedCommandOptions.ARGUMENT_TD_ROLE, false);
                if (!string.IsNullOrWhiteSpace(taskIAMRole))
                {
                    registerRequest.TaskRoleArn = taskIAMRole;
                }

                if (registerRequest.ContainerDefinitions == null)
                {
                    registerRequest.ContainerDefinitions = new List<ContainerDefinition>();
                }

                var containerDefinition = registerRequest.ContainerDefinitions.FirstOrDefault(x => string.Equals(x.Name, ecsContainer, StringComparison.Ordinal));

                if (containerDefinition == null)
                {
                    logger?.WriteLine("Creating new container definition");

                    containerDefinition = new ContainerDefinition
                    {
                        Name = ecsContainer
                    };
                    registerRequest.ContainerDefinitions.Add(containerDefinition);
                }

                containerDefinition.Image = dockerImageTag;

                {
                    var containerCommands = command.GetStringValuesOrDefault(properties.ContainerCommands, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_COMMANDS, false);
                    if (containerCommands != null && containerCommands.Length > 0)
                    {
                        containerDefinition.Command = new List<string>(containerCommands);
                    }
                }
                {
                    var disableNetworking = command.GetBoolValueOrDefault(properties.ContainerDisableNetworking, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DISABLE_NETWORKING, false);
                    if (disableNetworking.HasValue)
                    {
                        containerDefinition.DisableNetworking = disableNetworking.Value;
                    }
                }
                {
                    var dnsSearchServers = command.GetStringValuesOrDefault(properties.ContainerDNSSearchDomains, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS, false);
                    if (dnsSearchServers != null && dnsSearchServers.Length > 0)
                    {
                        containerDefinition.DnsSearchDomains = new List<string>(dnsSearchServers);
                    }
                }
                {
                    var dnsServers = command.GetStringValuesOrDefault(properties.ContainerDNSServers, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SERVERS, false);
                    if (dnsServers != null && dnsServers.Length > 0)
                    {
                        containerDefinition.DnsServers = new List<string>(dnsServers);
                    }
                }
                {
                    var labels = command.GetKeyValuePairOrDefault(properties.ContainerDockerLabels, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_LABELS, false);
                    if (labels != null && labels.Count > 0)
                    {
                        containerDefinition.DockerLabels = new Dictionary<string, string>(labels);
                    }
                }
                {
                    var options = command.GetStringValuesOrDefault(properties.ContainerDockerSecurityOptions, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS, false);
                    if (options != null && options.Length > 0)
                    {
                        containerDefinition.DockerSecurityOptions = new List<string>(options);
                    }
                }
                {
                    var entrypoints = command.GetStringValuesOrDefault(properties.ContainerEntryPoint, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENTRY_POINT, false);
                    if (entrypoints != null && entrypoints.Length > 0)
                    {
                        containerDefinition.EntryPoint = new List<string>(entrypoints);
                    }
                }
                {
                    var environmentVariables = command.GetKeyValuePairOrDefault(properties.ContainerEnvironmentVariables, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES, false);
                    if (environmentVariables != null && environmentVariables.Count > 0)
                    {
                        var listEnv = new List<Amazon.ECS.Model.KeyValuePair>();
                        foreach (var e in environmentVariables)
                        {
                            listEnv.Add(new Amazon.ECS.Model.KeyValuePair { Name = e.Key, Value = e.Value });
                        }
                        containerDefinition.Environment = listEnv;
                    }
                }
                {
                    var essential = command.GetBoolValueOrDefault(properties.ContainerEssential, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ESSENTIAL, false);
                    if (essential.HasValue)
                    {
                        containerDefinition.Essential = essential.Value;
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerExtraHosts, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_EXTRA_HOSTS);
                    if (data.HasValue)
                    {
                        if (containerDefinition.ExtraHosts == null)
                            containerDefinition.ExtraHosts = new List<Amazon.ECS.Model.HostEntry>();
                        containerDefinition.ExtraHosts.Clear();
                        foreach (JsonElement item in data.Value.EnumerateArray())
                        {
                            var obj = new Amazon.ECS.Model.HostEntry();

                            if (item.TryGetProperty("hostname", out JsonElement hostname))
                                obj.Hostname = hostname.GetString();
                            if (item.TryGetProperty("ipAddress", out JsonElement ipAddress))
                                obj.IpAddress = ipAddress.GetString();
                            containerDefinition.ExtraHosts.Add(obj);
                        }
                    }
                }
                {
                    var hostname = command.GetStringValueOrDefault(properties.ContainerHostname, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_HOSTNAME, false);
                    if (!string.IsNullOrEmpty(hostname))
                    {
                        containerDefinition.Hostname = hostname;
                    }
                }
                {
                    var links = command.GetStringValuesOrDefault(properties.ContainerLinks, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINKS, false);
                    if (links != null && links.Length > 0)
                    {
                        containerDefinition.Links = new List<string>(links);
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerLinuxParameters, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINUX_PARAMETERS);
                    if (data.HasValue)
                    {
                        var linuxParameter = new LinuxParameters();

                        if (data.Value.TryGetProperty("capabilities", out JsonElement capabilities))
                        {
                            linuxParameter.Capabilities = new KernelCapabilities();
                            if (capabilities.TryGetProperty("drop", out JsonElement drop))
                            {
                                if (linuxParameter.Capabilities.Drop == null)
                                    linuxParameter.Capabilities.Drop = new List<string>();
                                foreach (JsonElement item in drop.EnumerateArray())
                                {
                                    linuxParameter.Capabilities.Drop.Add(item.GetString());
                                }
                            }
                            if (capabilities.TryGetProperty("add", out JsonElement add))
                            {
                                if (linuxParameter.Capabilities.Add == null)
                                    linuxParameter.Capabilities.Add = new List<string>();
                                foreach (JsonElement item in add.EnumerateArray())
                                {
                                    linuxParameter.Capabilities.Add.Add(item.GetString());
                                }
                            }
                        }
                        if (data.Value.TryGetProperty("devices", out JsonElement devices))
                        {
                            if (linuxParameter.Devices == null)
                                linuxParameter.Devices = new List<Device>();
                            linuxParameter.Devices.Clear();
                            foreach (JsonElement item in devices.EnumerateArray())
                            {
                                var device = new Device();
                                if (item.TryGetProperty("containerPath", out JsonElement containerPath))
                                    device.ContainerPath = containerPath.GetString();
                                if (item.TryGetProperty("hostPath", out JsonElement hostPath))
                                    device.HostPath = hostPath.GetString();
                                if (item.TryGetProperty("permissions", out JsonElement permissions))
                                {
                                    if (device.Permissions == null)
                                        device.Permissions = new List<string>();
                                    foreach (JsonElement permission in permissions.EnumerateArray())
                                    {
                                        device.Permissions.Add(permission.GetString());
                                    }
                                }

                                linuxParameter.Devices.Add(device);
                            }
                        }

                        if (data.Value.TryGetProperty("initProcessEnabled", out JsonElement initProcessEnabled) && initProcessEnabled.ValueKind == JsonValueKind.True)
                            linuxParameter.InitProcessEnabled = initProcessEnabled.GetBoolean();

                        containerDefinition.LinuxParameters = linuxParameter;
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerLogConfiguration, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LOG_CONFIGURATION);
                    if (data.HasValue)
                    {
                        containerDefinition.LogConfiguration = new LogConfiguration();
                        containerDefinition.LogConfiguration.LogDriver = null;

                        // Added support for logDriver JSON key as fix for legacy bug where JSON key containerPath was used. 
                        if (data.Value.TryGetProperty("logDriver", out JsonElement logDriver))
                        {
                            containerDefinition.LogConfiguration.LogDriver = logDriver.GetString();
                        }
                        else if (data.Value.TryGetProperty("containerPath", out JsonElement containerPath)) // Retained legacy containerPath JSON key support. Removing this would break existing customers.
                        {
                            containerDefinition.LogConfiguration.LogDriver = containerPath.GetString();
                        }
                        
                        if (data.Value.TryGetProperty("options", out JsonElement options))
                        {
                            if (containerDefinition.LogConfiguration.Options == null)
                                containerDefinition.LogConfiguration.Options = new Dictionary<string, string>();
                            foreach (JsonProperty prop in options.EnumerateObject())
                            {
                                containerDefinition.LogConfiguration.Options[prop.Name] = prop.Value.GetString();
                            }
                        }
                    }
                }
                {
                    var hardLimit = command.GetIntValueOrDefault(properties.ContainerMemoryHardLimit, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT, false);
                    if (hardLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container hard memory limit {hardLimit.Value}MiB");
                        containerDefinition.Memory = hardLimit.Value;
                    }
                }
                {
                    var softLimit = command.GetIntValueOrDefault(properties.ContainerMemorySoftLimit, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT, false);
                    if (softLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container soft memory limit {softLimit.Value}MiB");
                        containerDefinition.MemoryReservation = softLimit.Value;
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerMountPoints, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MOUNT_POINTS);
                    if (data.HasValue)
                    {
                        if (containerDefinition.MountPoints == null)
                            containerDefinition.MountPoints = new List<MountPoint>();
                        containerDefinition.MountPoints.Clear();
                        foreach (JsonElement item in data.Value.EnumerateArray())
                        {
                            var mountPoint = new MountPoint();
                            if (item.TryGetProperty("containerPath", out JsonElement containerPath))
                                mountPoint.ContainerPath = containerPath.GetString();
                            if (item.TryGetProperty("sourceVolume", out JsonElement sourceVolume))
                                mountPoint.SourceVolume = sourceVolume.GetString();
                            if (item.TryGetProperty("readOnly", out JsonElement readOnly) && (readOnly.ValueKind == JsonValueKind.True || readOnly.ValueKind == JsonValueKind.False))
                            {
                                mountPoint.ReadOnly = readOnly.GetBoolean();
                            }

                            containerDefinition.MountPoints.Add(mountPoint);
                        }
                    }
                }
                {
                    var portMappings = command.GetStringValuesOrDefault(properties.ContainerPortMappings, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING, false);
                    if (portMappings != null)
                    {
                        containerDefinition.PortMappings = new List<PortMapping>();
                        foreach (var mapping in portMappings)
                        {
                            var tokens = mapping.Split(':');
                            if (tokens.Length != 2)
                            {
                                throw new DockerToolsException($"Port mapping {mapping} is invalid. Format should be <host-port>:<container-port>,<host-port>:<container-port>,...", DockerToolsException.CommonErrorCode.CommandLineParseError);
                            }
                            int hostPort = !isFargate ? int.Parse(tokens[0]) : int.Parse(tokens[1]);

                            logger?.WriteLine($"Adding port mapping host {hostPort} to container {tokens[1]}");
                            containerDefinition.PortMappings.Add(new PortMapping
                            {
                                HostPort = hostPort,
                                ContainerPort = int.Parse(tokens[1])
                            });
                        }
                    }
                }
                {
                    var privileged = command.GetBoolValueOrDefault(properties.ContainerPrivileged, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PRIVILEGED, false);
                    if (privileged.HasValue)
                    {
                        containerDefinition.Privileged = privileged.Value;
                    }
                }
                {
                    var readonlyFilesystem = command.GetBoolValueOrDefault(properties.ContainerReadonlyRootFilesystem, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_READONLY_ROOT_FILESYSTEM, false);
                    if (readonlyFilesystem.HasValue)
                    {
                        containerDefinition.ReadonlyRootFilesystem = readonlyFilesystem.Value;
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerUlimits, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ULIMITS);
                    if (data.HasValue)
                    {
                        if (containerDefinition.Ulimits == null)
                            containerDefinition.Ulimits = new List<Ulimit>();
                        containerDefinition.Ulimits.Clear();
                        foreach (JsonElement item in data.Value.EnumerateArray())
                        {
                            var ulimit = new Ulimit();

                            if (item.TryGetProperty("name", out JsonElement name))
                                ulimit.Name = name.GetString();
                            if (item.TryGetProperty("hardLimit", out JsonElement hardLimit) && hardLimit.ValueKind == JsonValueKind.Number)
                            {
                                ulimit.HardLimit = hardLimit.GetInt32();
                            }
                            if (item.TryGetProperty("softLimit", out JsonElement softLimit) && softLimit.ValueKind == JsonValueKind.Number)
                            {
                                ulimit.SoftLimit = softLimit.GetInt32();
                            }

                            containerDefinition.Ulimits.Add(ulimit);
                        }
                    }
                }
                {
                    var containerUser = command.GetStringValueOrDefault(properties.ContainerUser, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_USER, false);
                    if (!string.IsNullOrEmpty(containerUser))
                    {
                        containerDefinition.User = containerUser;
                    }
                }
                {
                    var data = command.GetJsonValueOrDefault(properties.ContainerVolumesFrom, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_VOLUMES_FROM);
                    if (data.HasValue)
                    {
                        if (containerDefinition.VolumesFrom == null)
                            containerDefinition.VolumesFrom = new List<VolumeFrom>();
                        containerDefinition.VolumesFrom.Clear();
                        foreach (JsonElement item in data.Value.EnumerateArray())
                        {
                            var volume = new VolumeFrom();
                            if (item.TryGetProperty("sourceContainer", out JsonElement sourceContainer))
                                volume.SourceContainer = sourceContainer.GetString();
                            if (item.TryGetProperty("readOnly", out JsonElement readOnly) && (readOnly.ValueKind == JsonValueKind.True || readOnly.ValueKind == JsonValueKind.False))
                            {
                                volume.ReadOnly = readOnly.GetBoolean();
                            }

                            containerDefinition.VolumesFrom.Add(volume);
                        }
                    }
                }
                {
                    var workingDirectory = command.GetStringValueOrDefault(properties.ContainerWorkingDirectory, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_WORKING_DIRECTORY, false);
                    if (!string.IsNullOrEmpty(workingDirectory))
                    {
                        containerDefinition.WorkingDirectory = workingDirectory;
                    }
                }


                if (isFargate)
                {
                    if (registerRequest.NetworkMode != null && registerRequest.NetworkMode != NetworkMode.Awsvpc)
                    {
                        logger?.WriteLine("Setting network mode to \"awsvpc\" which is required to launch fargate based tasks");
                    }

                    registerRequest.NetworkMode = NetworkMode.Awsvpc;

                    if (registerRequest.RequiresCompatibilities == null)
                        registerRequest.RequiresCompatibilities = new List<string>();

                    if (!registerRequest.RequiresCompatibilities.Contains("FARGATE"))
                        registerRequest.RequiresCompatibilities.Add("FARGATE");

                    registerRequest.Memory = command.GetStringValueOrDefault(properties.TaskMemory, ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY, true);
                    registerRequest.Cpu = command.GetStringValueOrDefault(properties.TaskCPU, ECSDefinedCommandOptions.ARGUMENT_TD_CPU, true);

                    var taskExecutionRole = command.GetStringValueOrDefault(properties.TaskDefinitionExecutionRole, ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE, false);
                    if (!string.IsNullOrEmpty(taskExecutionRole))
                    {
                        registerRequest.ExecutionRoleArn = taskExecutionRole;
                    }
                    else if (string.IsNullOrEmpty(registerRequest.ExecutionRoleArn)) // If this is a redeployment check to see if the role was already set in a previous deployment.
                    {
                        if (string.IsNullOrEmpty(registerRequest.ExecutionRoleArn))
                        {
                            registerRequest.ExecutionRoleArn = await EnsureTaskExecutionRoleExists(logger, command);
                        }
                    }
                }
                else
                {
                    registerRequest.ExecutionRoleArn = null;
                }

                if (registerRequest.NetworkMode != NetworkMode.Awsvpc)
                {
                    if (registerRequest.RequiresCompatibilities != null)
                        registerRequest.RequiresCompatibilities.Clear();
                }

                if (containerDefinition.LogConfiguration == null || containerDefinition.LogConfiguration.LogDriver == LogDriver.Awslogs)
                {
                    string defaultLogGroup = "/ecs/" + ecsTaskDefinition + "/" + containerDefinition.Name;

                    if (containerDefinition.LogConfiguration == null)
                        containerDefinition.LogConfiguration = new LogConfiguration() { LogDriver = "awslogs" };

                    if (containerDefinition.LogConfiguration.Options == null)
                        containerDefinition.LogConfiguration.Options = new Dictionary<string, string>();

                    var options = containerDefinition.LogConfiguration.Options;
                    options["awslogs-group"] = (options.ContainsKey("awslogs-group") && !string.IsNullOrWhiteSpace(options["awslogs-group"]) ? options["awslogs-group"] : defaultLogGroup);
                    options["awslogs-region"] = (options.ContainsKey("awslogs-region") && !string.IsNullOrWhiteSpace(options["awslogs-region"]) ? options["awslogs-region"] : command.DetermineAWSRegion().SystemName);
                    options["awslogs-stream-prefix"] = (options.ContainsKey("awslogs-stream-prefix") && !string.IsNullOrWhiteSpace(options["awslogs-stream-prefix"]) ? options["awslogs-stream-prefix"] : "ecs");

                    await EnsureLogGroupExistsAsync(logger, command.CWLClient, options["awslogs-group"]);

                    logger?.WriteLine("Configured ECS to log to the CloudWatch Log Group " + options["awslogs-group"]);
                }

                var registerResponse = await ecsClient.RegisterTaskDefinitionAsync(registerRequest);
                logger?.WriteLine($"Registered new task definition revision {registerResponse.TaskDefinition.Revision}");
                return registerResponse.TaskDefinition.TaskDefinitionArn;
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error updating ECS task definition {ecsTaskDefinition}: {e.Message}", DockerToolsException.ECSErrorCode.FailedToUpdateTaskDefinition);
            }
        }

        public static async System.Threading.Tasks.Task EnsureLogGroupExistsAsync(IToolLogger logger, IAmazonCloudWatchLogs cwlClient, string logGroup)
        {
            try
            {
                var response = await cwlClient.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
                {
                    LogGroupNamePrefix = logGroup
                });

                if (response.LogGroups != null && response.LogGroups.FirstOrDefault(x => string.Equals(logGroup, x.LogGroupName, StringComparison.Ordinal)) != null)
                {
                    logger?.WriteLine("Found existing log group " + logGroup + " for container");
                    return;
                }
            }
            catch (Exception e)
            {
                throw new DockerToolsException("Error checking log group " + logGroup + " existed for the container: " + e.Message, DockerToolsException.ECSErrorCode.LogGroupDescribeFailed);
            }

            try
            {
                await cwlClient.CreateLogGroupAsync(new CreateLogGroupRequest
                {
                    LogGroupName = logGroup
                });
                logger?.WriteLine("Created log group " + logGroup + " for the container");
            }
            catch (Exception e)
            {
                throw new DockerToolsException("Failed to create log group " + logGroup + " for the container: " + e.Message, DockerToolsException.ECSErrorCode.LogGroupCreateFailed);
            }
        }

        private const string DEFAULT_ECS_TASK_EXECUTION_ROLE = "ecsTaskExecutionRole";
        public static async Task<string> EnsureTaskExecutionRoleExists(IToolLogger logger, ECSBaseCommand command)
        {
            bool roleExists = false;
            try
            {
                var request = new GetRoleRequest { RoleName = DEFAULT_ECS_TASK_EXECUTION_ROLE };
                var response = await command.IAMClient.GetRoleAsync(request).ConfigureAwait(false);
                roleExists = true;
                logger.WriteLine("Task Execution role \"{0}\" already exists.", DEFAULT_ECS_TASK_EXECUTION_ROLE);
            }
            catch (Amazon.IdentityManagement.Model.NoSuchEntityException)
            {
                roleExists = false;
            }
            catch (Exception e)
            {
                logger.WriteLine("Error checking to make sure role \"ecsTaskExecutionRole\" exists, continuing on assuming the role exists: " + e.Message);
            }

            if (roleExists)
                return DEFAULT_ECS_TASK_EXECUTION_ROLE;

            logger.WriteLine("Creating default \"{0}\" IAM role.", DEFAULT_ECS_TASK_EXECUTION_ROLE);
            RoleHelper.CreateRole(command.IAMClient, DEFAULT_ECS_TASK_EXECUTION_ROLE, Amazon.Common.DotNetCli.Tools.Constants.ECS_TASKS_ASSUME_ROLE_POLICY, "CloudWatchLogsFullAccess", "AmazonEC2ContainerRegistryReadOnly");
            return DEFAULT_ECS_TASK_EXECUTION_ROLE;
        }


        public static async System.Threading.Tasks.Task SetupAwsVpcNetworkConfigurationAsync(ECSBaseDeployCommand command, NetworkConfiguration networkConfiguration)
        {
            if (networkConfiguration.AwsvpcConfiguration == null)
                networkConfiguration.AwsvpcConfiguration = new AwsVpcConfiguration();

            string defaultVpcId = null;

            bool noExistingSubnets = networkConfiguration.AwsvpcConfiguration.Subnets == null || networkConfiguration.AwsvpcConfiguration.Subnets.Count == 0;

            var vpcSubnetWrapper = await SetupAwsVpcNetworkConfigurationSubnets(command, defaultVpcId, noExistingSubnets);
            var subnets = vpcSubnetWrapper.Subnets;
            defaultVpcId = vpcSubnetWrapper.VpcId;

            if (subnets != null)
            {
                networkConfiguration.AwsvpcConfiguration.Subnets = new List<string>(subnets);
            }

            bool noExistingSecurityGroups = networkConfiguration.AwsvpcConfiguration.SecurityGroups == null || networkConfiguration.AwsvpcConfiguration.SecurityGroups.Count == 0;
            var securityGroups = await SetupAwsVpcNetworkConfigurationSecurityGroups(command, defaultVpcId, noExistingSecurityGroups);

            if (securityGroups != null)
            {
                networkConfiguration.AwsvpcConfiguration.SecurityGroups = new List<string>(securityGroups);
            }

            var assignPublicIp = command.GetBoolValueOrDefault(command.ClusterProperties.AssignPublicIpAddress, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, false);
            if (assignPublicIp.HasValue)
            {
                networkConfiguration.AwsvpcConfiguration.AssignPublicIp =
                    assignPublicIp.Value ? AssignPublicIp.ENABLED : AssignPublicIp.DISABLED;
            }
            else if (networkConfiguration?.AwsvpcConfiguration?.AssignPublicIp == null)
            {
                // Enable by default if not set to make the common case easier
                networkConfiguration.AwsvpcConfiguration.AssignPublicIp = AssignPublicIp.ENABLED;
                command.Logger?.WriteLine("Enabling \"Assign Public IP\" for tasks");
            }
        }

        public static async System.Threading.Tasks.Task SetupAwsVpcNetworkConfigurationCloudwatchEventAsync(
            ECSBaseDeployCommand command, Amazon.CloudWatchEvents.Model.NetworkConfiguration networkConfiguration)
        {
            if (networkConfiguration.AwsvpcConfiguration == null)
                networkConfiguration.AwsvpcConfiguration = new Amazon.CloudWatchEvents.Model.AwsVpcConfiguration();

            string defaultVpcId = null;

            bool noExistingSubnets = networkConfiguration.AwsvpcConfiguration.Subnets == null || networkConfiguration.AwsvpcConfiguration.Subnets.Count == 0;
            var vpcSubnetWrapper = await SetupAwsVpcNetworkConfigurationSubnets(command, defaultVpcId, noExistingSubnets);
            var subnets = vpcSubnetWrapper.Subnets;
            defaultVpcId = vpcSubnetWrapper.VpcId;

            if (subnets != null)
            {
                networkConfiguration.AwsvpcConfiguration.Subnets = new List<string>(subnets);
            }
            
            bool noExistingSecurityGroups = networkConfiguration.AwsvpcConfiguration.SecurityGroups == null || networkConfiguration.AwsvpcConfiguration.SecurityGroups.Count == 0;
            var securityGroups = await SetupAwsVpcNetworkConfigurationSecurityGroups(command, defaultVpcId, noExistingSecurityGroups);

            if (securityGroups != null)
            {
                networkConfiguration.AwsvpcConfiguration.SecurityGroups = new List<string>(securityGroups);
            }

            var assignPublicIp = command.GetBoolValueOrDefault(command.ClusterProperties.AssignPublicIpAddress, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, false);
            if (assignPublicIp.HasValue)
            {
                networkConfiguration.AwsvpcConfiguration.AssignPublicIp = assignPublicIp.Value ? Amazon.CloudWatchEvents.AssignPublicIp.ENABLED : Amazon.CloudWatchEvents.AssignPublicIp.DISABLED;
            }
            else if(networkConfiguration?.AwsvpcConfiguration?.AssignPublicIp == null)
            {
                // Enable by default if not set to make the common case easier
                networkConfiguration.AwsvpcConfiguration.AssignPublicIp = Amazon.CloudWatchEvents.AssignPublicIp.ENABLED;
                command.Logger?.WriteLine("Enabling \"Assign Public IP\" for tasks");
            }
        }


        private static async Task<VpcSubnetWrapper> SetupAwsVpcNetworkConfigurationSubnets(ECSBaseDeployCommand command,
            string defaultVpcId, bool noExistingSubnets)
        {
            var subnets = command.GetStringValuesOrDefault(command.ClusterProperties.SubnetIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, false);
            if ((subnets == null || subnets.Length == 0) && noExistingSubnets)
            {
                command.Logger?.WriteLine("No subnets specified, looking for default VPC and subnets");
                var defaultSubnets = new List<string>();
                try
                {
                    var describeSubnetResponse = await command.EC2Client.DescribeSubnetsAsync();
                    if (describeSubnetResponse.Subnets != null)
                    {
                        foreach (var subnet in describeSubnetResponse.Subnets)
                        {
                            if (subnet.DefaultForAz.GetValueOrDefault())
                            {
                                if (defaultVpcId == null)
                                {
                                    command.Logger?.WriteLine("Default VPC: " + subnet.VpcId);
                                    defaultVpcId = subnet.VpcId;
                                }

                                command.Logger?.WriteLine($"... Using subnet {subnet.SubnetId} ({subnet.AvailabilityZone})");
                                defaultSubnets.Add(subnet.SubnetId);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    command.Logger?.WriteLine("Warning: Unable to determine default subnets for VPC: " + e.Message);
                }

                if (defaultSubnets.Count != 0)
                {
                    subnets = defaultSubnets.ToArray();
                }
                else
                {
                    subnets = command.GetStringValuesOrDefault(command.ClusterProperties.SubnetIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, true);
                }
            }

            return new VpcSubnetWrapper {VpcId = defaultVpcId, Subnets = subnets};
        }


        private static async Task<string[]> SetupAwsVpcNetworkConfigurationSecurityGroups(ECSBaseDeployCommand command, 
            string defaultVpcId, bool noExistingSecurityGroups)
        {
            var securityGroups = command.GetStringValuesOrDefault(command.ClusterProperties.SecurityGroupIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, false);
            if ((securityGroups == null || securityGroups.Length==0) && noExistingSecurityGroups)
            {
                command.Logger?.WriteLine("No security group specified, looking for default VPC and security group");
                if (defaultVpcId == null)
                {
                    try
                    {
                        var describeVpcResponse = await command.EC2Client.DescribeVpcsAsync();
                        var defaultVpc = describeVpcResponse.Vpcs?.FirstOrDefault(x => x.IsDefault.GetValueOrDefault());
                        if (defaultVpc != null)
                        {
                            command.Logger?.WriteLine("Default VPC: " + defaultVpc.VpcId);
                            defaultVpcId = defaultVpc.VpcId;
                        }
                        else
                        {
                            command.Logger?.WriteLine("Unable to determine default VPC");
                        }
                    }
                    catch (Exception e)
                    {
                        command.Logger?.WriteLine("Warning: Unable to determine default VPC: " + e.Message);
                    }
                }


                if (defaultVpcId != null)
                {
                    try
                    {
                        var describeSecurityGroupResponse = await command.EC2Client.DescribeSecurityGroupsAsync(new DescribeSecurityGroupsRequest
                            {
                                Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { defaultVpcId } } }
                            });

                        var defaultSecurityGroup = describeSecurityGroupResponse.SecurityGroups?.FirstOrDefault(x => string.Equals(x.GroupName, "default", StringComparison.OrdinalIgnoreCase));

                        if (defaultSecurityGroup != null)
                        {
                            securityGroups = new string[] { defaultSecurityGroup.GroupId };
                            command.Logger?.WriteLine("Using default security group " + defaultSecurityGroup.GroupId);
                        }
                        else
                        {
                            command.Logger?.WriteLine("Unable to determine default security group for VPC");
                        }
                    }
                    catch (Exception e)
                    {
                        command.Logger?.WriteLine("Warning: Unable to determine default security group for VPC: " + e.Message);
                    }
                }

                if (securityGroups == null)
                {
                    securityGroups = command.GetStringValuesOrDefault(command.ClusterProperties.SecurityGroupIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, true);
                }
            }

            return securityGroups;
        }
    }

    public class VpcSubnetWrapper
    {
        public string VpcId { get; set; }
        public string[] Subnets { get; set; }
    }
}