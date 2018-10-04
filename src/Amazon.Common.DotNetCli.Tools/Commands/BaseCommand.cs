using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;

namespace Amazon.Common.DotNetCli.Tools.Commands
{
    public abstract class BaseCommand<TDefaultConfig> : ICommand
        where TDefaultConfig : DefaultConfigFile, new()
    {
        public ToolsException LastToolsException { get; protected set; }

        public string[] OriginalCommandLineArguments { get; private set; }

        public BaseCommand(IToolLogger logger, string workingDirectory)
        {
            this.Logger = logger;
            this.WorkingDirectory = workingDirectory;
        }

        public BaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : this(logger, workingDirectory)
        {
            args = args ?? new string[0];
            this.OriginalCommandLineArguments = args;
            var values = CommandLineParser.ParseArguments(possibleOptions, args);
            ParseCommandArguments(values);
        }

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                var success = await PerformActionAsync();
                if (!success) 
                    return false;
            
            
                if (this.GetBoolValueOrDefault(this.PersistConfigFile,
                    CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE, false).GetValueOrDefault())
                {
                    this.SaveConfigFile();
                }
            }
            catch (ToolsException e)
            {
                this.Logger?.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger?.WriteLine($"Unknown error executing command: {e.Message}");
                this.Logger?.WriteLine(e.StackTrace);
                return false;
            }
 

            return true;
        }

        protected abstract Task<bool> PerformActionAsync();

        protected abstract string ToolName
        {
            get;
        }

        /// <summary>
        /// The common options used by every command
        /// </summary>
        protected static readonly IList<CommandOption> CommonOptions = new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE,
            CommonDefinedCommandOptions.ARGUMENT_AWS_REGION,
            CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE,
            CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_AWS_ACCESS_KEY_ID,
            CommonDefinedCommandOptions.ARGUMENT_AWS_SECRET_KEY,
            CommonDefinedCommandOptions.ARGUMENT_AWS_SESSION_TOKEN,
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE,
            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE
        };

        /// <summary>
        /// Used to combine the command specific command options with the common options.
        /// </summary>
        /// <param name="optionCollections"></param>
        /// <returns></returns>
        protected static IList<CommandOption> BuildLineOptions(params IList<CommandOption>[] optionCollections)
        {
            var list = new List<CommandOption>();
            list.AddRange(CommonOptions);
            if(optionCollections != null)
            {
                foreach (var options in optionCollections)
                    list.AddRange(options);
            }

            return list;
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected virtual void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DISABLE_INTERACTIVE.Switch)) != null)
                this.DisableInteractive = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.Switch)) != null)
                this.Profile = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.Switch)) != null)
                this.ProfileLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION.Switch)) != null)
                this.Region = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.Switch)) != null)
                this.ProjectLocation = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE.Switch)) != null)
            {
                this.ConfigFile = tuple.Item2.StringValue;
                if (!File.Exists(this.ConfigFile))
                {
                    throw new ToolsException($"Config file {this.ConfigFile} can not be found.", ToolsException.CommonErrorCode.MissingConfigFile);
                }
            }
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_ACCESS_KEY_ID.Switch)) != null)
                this.AWSAccessKeyId = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_SECRET_KEY.Switch)) != null)
                this.AWSSecretKey = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_AWS_SESSION_TOKEN.Switch)) != null)
                this.AWSSessionToken = tuple.Item2.StringValue;

            if (string.IsNullOrEmpty(this.ConfigFile))
            {
                this.ConfigFile = new TDefaultConfig().DefaultConfigFileName;
            }
        }




        TDefaultConfig _defaultConfig;
        public TDefaultConfig DefaultConfig
        {
            get
            {
                if (this._defaultConfig == null)
                {
                    var config = new TDefaultConfig();
                    config.LoadDefaults(Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation), this.ConfigFile);
                    this._defaultConfig = config;
                }
                return this._defaultConfig;
            }
        }



        public string Region { get; set; }
        public string Profile { get; set; }
        public string ProfileLocation { get; set; }
        public string AWSAccessKeyId { get; set; }
        public string AWSSecretKey { get; set; }
        public string AWSSessionToken { get; set; }
        public AWSCredentials Credentials { get; set; }
        public string ProjectLocation { get; set; }
        public string ConfigFile { get; set; }
        public bool? PersistConfigFile { get; set; }


        /// <summary>
        /// Disable all Console.Read operations to make sure the command is never blocked waiting for input. This is 
        /// used by the AWS Visual Studio Toolkit to make sure it never gets blocked.
        /// </summary>
        public bool DisableInteractive { get; set; } = false;

        protected AWSCredentials DetermineAWSCredentials()
        {
            AWSCredentials credentials;
            if (this.Credentials != null)
            {
                credentials = this.Credentials;
            }
            else
            {
                var awsAccessKeyId = GetStringValueOrDefault(this.AWSAccessKeyId, CommonDefinedCommandOptions.ARGUMENT_AWS_ACCESS_KEY_ID, false);
                var profile = this.GetStringValueOrDefault(this.Profile, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE, false);

                if(!string.IsNullOrEmpty(awsAccessKeyId))
                {
                    var awsSecretKey = GetStringValueOrDefault(this.AWSSecretKey, CommonDefinedCommandOptions.ARGUMENT_AWS_SECRET_KEY, false);
                    var awsSessionToken = GetStringValueOrDefault(this.AWSSessionToken, CommonDefinedCommandOptions.ARGUMENT_AWS_SESSION_TOKEN, false);

                    if (string.IsNullOrEmpty(awsSecretKey))
                        throw new ToolsException("An AWS access key id was specified without a required AWS secret key. Either set an AWS secret key or remove the AWS access key id and use profiles for credentials.", ToolsException.CommonErrorCode.InvalidCredentialConfiguration);

                    if(string.IsNullOrEmpty(awsSessionToken))
                    {
                        credentials = new BasicAWSCredentials(awsAccessKeyId, awsSecretKey);
                    }
                    else
                    {
                        credentials = new SessionAWSCredentials(awsAccessKeyId, awsSecretKey, awsSessionToken);
                    }
                }
                else if (!string.IsNullOrEmpty(profile))
                {
                    var chain = new CredentialProfileStoreChain(this.ProfileLocation);
                    if (!chain.TryGetAWSCredentials(profile, out credentials))
                    {
                        credentials = FallbackCredentialsFactory.GetCredentials();
                    }
                }
                else
                {
                    credentials = FallbackCredentialsFactory.GetCredentials();
                }
            }

            return credentials;
        }

        public RegionEndpoint DetermineAWSRegion()
        {
            // See if a region has been set but don't prompt if not set.
            var regionName = this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false);
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            // See if we can find a region using the region fallback logic.
            if (string.IsNullOrWhiteSpace(regionName))
            {
                var region = FallbackRegionFactory.GetRegionEndpoint(true);
                if (region != null)
                {
                    return region;
                }
            }

            // If we still don't have a region prompt the user for a region.
            regionName = this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, true);
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                return RegionEndpoint.GetBySystemName(regionName);
            }

            throw new ToolsException("Can not determine AWS region. Either configure a default region or use the --region option.", ToolsException.CommonErrorCode.RegionNotConfigured);
        }

        /// <summary>
        /// Gets the value for the CommandOption either through the property value which means the 
        /// user explicity set the value or through defaults for the project. 
        /// 
        /// If no value is found in either the property value or the defaults and the value
        /// is required the user will be prompted for the value if we are running in interactive
        /// mode.
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public string GetStringValueOrDefault(string propertyValue, CommandOption option, bool required)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                return configDefault;
            }
            else if(DefaultConfig[option.Switch] is int)
            {
                var configDefault = (int)DefaultConfig[option.Switch];
                return configDefault.ToString();
            }
            else if (required && !this.DisableInteractive)
            {
                return PromptForValue(option);
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue;
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return propertyValue;
        }

        public string GetRoleValueOrDefault(string propertyValue, CommandOption option, string assumeRolePrincipal, string awsManagedPolicyPrefix, Dictionary<string, string> knownManagedPolicyDescription, bool required)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                return RoleHelper.ExpandRoleName(this.IAMClient, propertyValue);
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                return RoleHelper.ExpandRoleName(this.IAMClient, configDefault);
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue;
            }
            else if (required && !this.DisableInteractive)
            {
                var promptInfo = new RoleHelper.PromptRoleInfo
                {
                    AssumeRolePrincipal = assumeRolePrincipal,
                    AWSManagedPolicyNamePrefix = awsManagedPolicyPrefix,
                    KnownManagedPolicyDescription = knownManagedPolicyDescription
                };

                var role = RoleHelper.PromptForRole(this.IAMClient, promptInfo);
                if(!string.IsNullOrEmpty(role))
                {
                    _cachedRequestedValues[option] = role;
                }

                return role;
            }

            return null;
        }

        /// <summary>
        /// Complex parameters are formatted as a JSON string. This method parses the string into the JsonData object
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public JsonData GetJsonValueOrDefault(string propertyValue, CommandOption option)
        {
            string jsonContent = GetStringValueOrDefault(propertyValue, option, false);
            if (string.IsNullOrWhiteSpace(jsonContent))
                return null;

            try
            {
                var data = JsonMapper.ToObject(jsonContent);
                return data;
            }
            catch(Exception e)
            {
                throw new ToolsException($"Error parsing JSON string for parameter {option.Switch}: {e.Message}", ToolsException.CommonErrorCode.CommandLineParseError);
            }
        }

        /// <summary>
        /// String[] version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public string[] GetStringValuesOrDefault(string[] propertyValue, CommandOption option, bool required)
        {
            if (propertyValue != null)
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                if (string.IsNullOrEmpty(configDefault))
                    return null;

                return configDefault.SplitByComma();
            }
            else if (required && !this.DisableInteractive)
            {
                var response = PromptForValue(option);
                if (string.IsNullOrEmpty(response))
                    return null;

                return response.SplitByComma();
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue?.SplitByComma();
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        public Dictionary<string, string> GetKeyValuePairOrDefault(Dictionary<string, string> propertyValue, CommandOption option, bool required)
        {
            if (propertyValue != null)
            {
                return propertyValue;
            }
            else if (!string.IsNullOrEmpty(DefaultConfig[option.Switch] as string))
            {
                var configDefault = DefaultConfig[option.Switch] as string;
                if (string.IsNullOrEmpty(configDefault))
                    return null;

                return Utilities.ParseKeyValueOption(configDefault);
            }
            else if (required && !this.DisableInteractive)
            {
                var response = PromptForValue(option);
                if (string.IsNullOrEmpty(response))
                    return null;

                return Utilities.ParseKeyValueOption(response);
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue == null ? null : Utilities.ParseKeyValueOption(cachedValue);
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        /// <summary>
        /// Int version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public int? GetIntValueOrDefault(int? propertyValue, CommandOption option, bool required)
        {
            if (propertyValue.HasValue)
            {
                return propertyValue.Value;
            }
            else if (DefaultConfig[option.Switch] is int)
            {
                var configDefault = (int)DefaultConfig[option.Switch];
                return configDefault;
            }
            else if (required && !this.DisableInteractive)
            {
                var userValue = PromptForValue(option);
                if (string.IsNullOrWhiteSpace(userValue))
                    return null;

                int i;
                if (!int.TryParse(userValue, out i))
                {
                    throw new ToolsException($"{userValue} cannot be parsed into an integer for {option.Name}", ToolsException.CommonErrorCode.CommandLineParseError);
                }
                return i;
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                int i;
                if (int.TryParse(cachedValue, out i))
                {
                    return i;
                }
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        /// <summary>
        /// bool version of GetStringValueOrDefault
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <param name="option"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public bool? GetBoolValueOrDefault(bool? propertyValue, CommandOption option, bool required)
        {
            if (propertyValue.HasValue)
            {
                return propertyValue.Value;
            }
            else if (DefaultConfig[option.Switch] is bool)
            {
                var configDefault = (bool)DefaultConfig[option.Switch];
                return configDefault;
            }
            else if (required && !this.DisableInteractive)
            {
                var userValue = PromptForValue(option);
                if (string.IsNullOrWhiteSpace(userValue))
                    return null;

                bool i;
                if (bool.TryParse(userValue, out i))
                {
                    throw new ToolsException($"{userValue} cannot be parsed into a boolean for {option.Name}", ToolsException.CommonErrorCode.CommandLineParseError);
                }
                return i;
            }
            else if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                bool i;
                if (bool.TryParse(cachedValue, out i))
                {
                    return i;
                }
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        public string GetInstanceProfileOrDefault(string propertyValue, CommandOption option, bool required, string newRoleName)
        {
            var value = GetStringValueOrDefault(propertyValue, option, false);
            if (!string.IsNullOrEmpty(value))
            {
                value = RoleHelper.ExpandInstanceProfile(this.IAMClient, value);
                return value;
            }
            else if (required && !this.DisableInteractive)
            {
                var existingProfiles = RoleHelper.FindExistingInstanceProfilesAsync(this.IAMClient, 20).Result;
                var selections = new List<string>();
                foreach (var profile in existingProfiles)
                    selections.Add(profile.InstanceProfileName);

                selections.Add("*** Create new Instance Profile ***");
                var chosenIndex = PromptForValue(option, selections);

                if(chosenIndex < selections.Count - 1)
                {
                    var arn = existingProfiles[chosenIndex].Arn;
                    _cachedRequestedValues[option] = arn;
                    return arn;
                }
                else
                {
                    var promptInfo = new RoleHelper.PromptRoleInfo
                    {
                        KnownManagedPolicyDescription = Constants.COMMON_KNOWN_MANAGED_POLICY_DESCRIPTIONS
                    };
                    var managedPolices = RoleHelper.FindManagedPoliciesAsync(this.IAMClient, promptInfo, 20).Result;
                    var profileSelection = new List<string>();
                    foreach (var profile in managedPolices)
                        profileSelection.Add(profile.PolicyName);

                    chosenIndex = PromptForValue("Select managed policy to assign to new instance profile: ", profileSelection);

                    var uniqueRoleName = RoleHelper.GenerateUniqueIAMRoleName(this.IAMClient, newRoleName);

                    this.Logger?.WriteLine("Creating role {0}", uniqueRoleName);
                    RoleHelper.CreateRole(this.IAMClient, uniqueRoleName, Constants.EC2_ASSUME_ROLE_POLICY, managedPolices[chosenIndex].Arn);

                    this.Logger?.WriteLine("Creating instance profile {0}", uniqueRoleName);
                    var response = this.IAMClient.CreateInstanceProfileAsync(new IdentityManagement.Model.CreateInstanceProfileRequest
                    {
                        InstanceProfileName = uniqueRoleName
                    }).Result;

                    this.Logger?.WriteLine("Assigning role to instance profile");
                    this.IAMClient.AddRoleToInstanceProfileAsync(new IdentityManagement.Model.AddRoleToInstanceProfileRequest
                    {
                        InstanceProfileName = uniqueRoleName,
                        RoleName = uniqueRoleName
                    }).Wait();

                    var arn = response.InstanceProfile.Arn;
                    _cachedRequestedValues[option] = arn;
                    return arn;
                }
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;

        }

        protected string GetServiceRoleOrCreateIt(string propertyValue, CommandOption option, string roleName, string assumeRolePolicy, string policy, params string[] managedPolicies)
        {
            var value = GetStringValueOrDefault(propertyValue, option, false);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            Role role = null;
            try
            {
                role = this.IAMClient.GetRoleAsync(new GetRoleRequest
                {
                    RoleName = roleName
                }).Result.Role;
            }
            catch (AggregateException e)
            {
                if (!(e.InnerException is NoSuchEntityException))
                    throw e.InnerException;
            }

            if(role == null)
            {
                this.Logger?.WriteLine("Creating service role " + roleName);
                return RoleHelper.CreateRole(this.IAMClient, roleName, assumeRolePolicy, managedPolicies);
            }


            this.Logger?.WriteLine("Using service role " + role.RoleName);
            return role.Arn;
        }


        // Cache all prompted values so the user is never prompted for the same CommandOption later.
        protected Dictionary<CommandOption, string> _cachedRequestedValues = new Dictionary<CommandOption, string>();
        protected string PromptForValue(CommandOption option)
        {
            if (_cachedRequestedValues.ContainsKey(option))
            {
                var cachedValue = _cachedRequestedValues[option];
                return cachedValue;
            }

            string input = null;


            Console.Out.WriteLine($"Enter {option.Name}: ({option.Description})");
            Console.Out.Flush();
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return null;
            input = input.Trim();

            _cachedRequestedValues[option] = input;
            return input;
        }

        protected int PromptForValue(CommandOption option, IList<string> items)
        {
            return PromptForValue("Select " + option.Name + ":", items);
        }

        protected int PromptForValue(string message, IList<string> items)
        {
            Console.Out.WriteLine(message);
            for (int i = 0; i < items.Count; i++)
            {
                Console.Out.WriteLine($"   {(i + 1).ToString().PadLeft(2)}) {items[i]}");
            }

            Console.Out.Flush();

            int chosenIndex = WaitForIndexResponse(1, items.Count);
            return chosenIndex - 1;
        }


        private int WaitForIndexResponse(int min, int max)
        {
            int chosenIndex = -1;
            while (chosenIndex == -1)
            {
                var indexInput = Console.ReadLine()?.Trim();
                int parsedIndex;
                if (int.TryParse(indexInput, out parsedIndex) && parsedIndex >= min && parsedIndex <= max)
                {
                    chosenIndex = parsedIndex;
                }
                else
                {
                    Console.Out.WriteLine($"Invalid selection, must be a number between {min} and {max}");
                }
            }

            return chosenIndex;
        }


        public IToolLogger Logger { get; protected set; }
        public string WorkingDirectory { get; set; }

        protected void SetUserAgentString()
        {
            string version = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
            Util.Internal.InternalSDKUtils.SetUserAgent(this.ToolName,
                                          version);
        }

        IAmazonIdentityManagementService _iamClient;
        public IAmazonIdentityManagementService IAMClient
        {
            get
            {
                if (this._iamClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonIdentityManagementServiceConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._iamClient = new AmazonIdentityManagementServiceClient(DetermineAWSCredentials(), config);
                }
                return this._iamClient;
            }
            set { this._iamClient = value; }
        }

        IAmazonS3 _s3Client;
        public IAmazonS3 S3Client
        {
            get
            {
                if (this._s3Client == null)
                {
                    SetUserAgentString();

                    var config = new AmazonS3Config();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._s3Client = new AmazonS3Client(DetermineAWSCredentials(), config);
                }
                return this._s3Client;
            }
            set { this._s3Client = value; }
        }

        protected void SaveConfigFile()
        {
            try
            {
                JsonData data;
                if (File.Exists(this.DefaultConfig.SourceFile))
                {
                    data = JsonMapper.ToObject(File.ReadAllText(this.DefaultConfig.SourceFile));
                }
                else
                {
                    data = new JsonData();
                }

                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION.ConfigFileKey, this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.ConfigFileKey, this.GetStringValueOrDefault(this.Profile, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProfileLocation, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION, false));


                SaveConfigFile(data);

                StringBuilder sb = new StringBuilder();
                JsonWriter writer = new JsonWriter(sb);
                writer.PrettyPrint = true;
                JsonMapper.ToJson(data, writer);

                var json = sb.ToString();
                File.WriteAllText(this.DefaultConfig.SourceFile, json);
                this.Logger?.WriteLine($"Config settings saved to {this.DefaultConfig.SourceFile}");
            }
            catch (Exception e)
            {
                throw new ToolsException("Error persisting configuration file: " + e.Message, ToolsException.CommonErrorCode.PersistConfigError);
            }
        }

        protected abstract void SaveConfigFile(JsonData data);

        public bool ConfirmDeletion(string resource)
        {
            if (this.DisableInteractive)
                return true;

            Console.WriteLine("Are you sure you want to delete the {0}? [y/n]", resource);
            char input;
            do
            {
                input = Console.ReadKey().KeyChar;
            } while (input != 'y' && input != 'n');

            return input == 'y';
        }

        protected void EnsureInProjectDirectory()
        {
            var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));

            if (Directory.GetFiles(projectLocation, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1 ||
                Directory.GetFiles(projectLocation, "*.fsproj", SearchOption.TopDirectoryOnly).Length == 1 ||
                Directory.GetFiles(projectLocation, "*.vbproj", SearchOption.TopDirectoryOnly).Length == 1)
            {
                return;
            }

            throw new ToolsException($"No .NET project found in directory {projectLocation} to build.", ToolsException.CommonErrorCode.NoProjectFound);
        }
    }
}
