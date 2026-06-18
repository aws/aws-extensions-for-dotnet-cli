using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Invoke a function running in Lambda
    /// </summary>
    public class InvokeFunctionCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "invoke-function";
        public const string COMMAND_DESCRIPTION = "Command to invoke a function in Lambda with an optional input";
        public const string COMMAND_ARGUMENTS = "<FUNCTION-NAME> The name of the function to invoke";

        /// <summary>
        /// How the Lambda function should be invoked.
        /// </summary>
        public enum InvokeMode
        {
            /// <summary>
            /// Invoke the function synchronously and wait for the response. This is the default.
            /// </summary>
            RequestResponse,

            /// <summary>
            /// Invoke the function asynchronously.
            /// </summary>
            Event,

            /// <summary>
            /// Invoke the function and stream the response payload to the console as it is produced.
            /// </summary>
            Stream,

            /// <summary>
            /// Invoke a function that is configured for durable execution. The function is invoked
            /// asynchronously (Event) and the durable execution is then monitored by polling the
            /// GetDurableExecution and GetDurableExecutionHistory APIs until the execution completes.
            /// </summary>
            DurableExecution
        }


        public static readonly IList<CommandOption> InvokeCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME,
            LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD,
            LambdaDefinedCommandOptions.ARGUMENT_INVOKE_MODE
        });

        public string FunctionName { get; set; }

        /// <summary>
        /// If the value for Payload points to an existing file then the contents of the file is sent, otherwise
        /// the value of Payload is sent as the input to the function.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// How the function should be invoked. Defaults to <see cref="InvokeMode.RequestResponse"/>.
        /// </summary>
        public InvokeMode? Mode { get; set; }

        public InvokeFunctionCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, InvokeCommandOptions, args)
        {
        }


        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if(values.Arguments.Count > 0)
            {
                this.FunctionName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.Switch)) != null)
                this.FunctionName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD.Switch)) != null)
                this.Payload = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_INVOKE_MODE.Switch)) != null)
                this.Mode = ParseInvokeMode(tuple.Item2.StringValue);
        }

        /// <summary>
        /// Parse the --invoke-mode switch value into an <see cref="InvokeMode"/>.
        /// </summary>
        private static InvokeMode ParseInvokeMode(string value)
        {
            // Only accept the named enum values. Numeric input like "0" or "1" is rejected even though
            // Enum.TryParse would otherwise parse it, since the CLI help only documents the named values.
            if (!string.IsNullOrEmpty(value) &&
                !char.IsDigit(value[0]) && value[0] != '-' && value[0] != '+' &&
                Enum.TryParse<InvokeMode>(value, true, out var mode) && Enum.IsDefined(typeof(InvokeMode), mode))
            {
                return mode;
            }

            throw new LambdaToolsException(
                $"Invalid value \"{value}\" for {LambdaDefinedCommandOptions.ARGUMENT_INVOKE_MODE.Switch}. Valid values are: " +
                $"{InvokeMode.RequestResponse}, {InvokeMode.Event}, {InvokeMode.Stream} or {InvokeMode.DurableExecution}.",
                ToolsException.CommonErrorCode.CommandLineParseError);
        }

        protected override async Task<bool> PerformActionAsync()
        {
            var functionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true);
            var payload = ResolvePayload();
            var mode = this.Mode ?? InvokeMode.RequestResponse;

            await LambdaUtilities.WaitTillFunctionAvailableAsync(this.Logger, this.LambdaClient, functionName);

            switch (mode)
            {
                case InvokeMode.Stream:
                    await InvokeWithResponseStreamAsync(functionName, payload);
                    break;
                case InvokeMode.Event:
                    await InvokeEventAsync(functionName, payload);
                    break;
                case InvokeMode.DurableExecution:
                    await InvokeDurableExecutionAsync(functionName, payload);
                    break;
                default:
                    await InvokeRequestResponseAsync(functionName, payload);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Resolves the payload to send to the function. If <see cref="Payload"/> points to an existing file then
        /// the contents of the file are used, otherwise the raw value is used. If the value is already valid JSON
        /// (an object, array, or scalar such as a number, boolean, string or null) it is sent as-is; otherwise it is
        /// JSON-serialized as a string so the function receives a valid JSON value.
        /// </summary>
        private string ResolvePayload()
        {
            if (string.IsNullOrWhiteSpace(this.Payload))
            {
                return null;
            }

            string payload;
            if (File.Exists(this.Payload))
            {
                Logger.WriteLine($"Reading {Path.GetFullPath(this.Payload)} as input to Lambda function");
                payload = File.ReadAllText(this.Payload);
            }
            else
            {
                payload = this.Payload.Trim();
            }

            // We should still check for empty payload in case it is read from a file.
            if (string.IsNullOrEmpty(payload))
            {
                return payload;
            }

            // If the value is already valid JSON (including scalars like 123, true or null) send it unchanged.
            // Otherwise treat it as a raw string and JSON-serialize it so quotes/backslashes are escaped correctly.
            if (IsValidJson(payload))
            {
                return payload;
            }

            return System.Text.Json.JsonSerializer.Serialize(payload);
        }

        /// <summary>
        /// Returns true if the supplied text is a complete, well-formed JSON value.
        /// </summary>
        private static bool IsValidJson(string value)
        {
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(value))
                {
                    return true;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Invoke the function synchronously (RequestResponse) and print the response payload and log tail.
        /// </summary>
        private async Task InvokeRequestResponseAsync(string functionName, string payload)
        {
            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                LogType = LogType.Tail,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload
            };

            InvokeResponse response;
            try
            {
                response = await this.LambdaClient.InvokeAsync(invokeRequest);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error invoking Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction, e);
            }

            if (!string.IsNullOrEmpty(response.FunctionError))
            {
                this.Logger.WriteLine($"Function error: {response.FunctionError}");
            }

            this.Logger.WriteLine("Payload:");
            PrintPayload(response);

            this.Logger.WriteLine("");
            this.Logger.WriteLine("Log Tail:");
            var log = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
            this.Logger.WriteLine(log);
        }

        /// <summary>
        /// Invoke the function asynchronously (Event). Lambda does not return a payload for asynchronous
        /// invocations so display any function error and the durable execution ARN if one was returned.
        /// </summary>
        private async Task InvokeEventAsync(string functionName, string payload)
        {
            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.Event,
                Payload = payload
            };

            InvokeResponse response;
            try
            {
                response = await this.LambdaClient.InvokeAsync(invokeRequest);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error invoking Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction, e);
            }

            this.Logger.WriteLine($"Request to invoke function accepted (HTTP status code {(int)response.StatusCode}).");

            if (!string.IsNullOrEmpty(response.FunctionError))
            {
                this.Logger.WriteLine($"Function error: {response.FunctionError}");
            }

            if (!string.IsNullOrEmpty(response.DurableExecutionArn))
            {
                this.Logger.WriteLine($"Durable execution ARN: {response.DurableExecutionArn}");
            }
        }

        /// <summary>
        /// Invoke a function configured for durable execution. The function is invoked asynchronously
        /// (InvocationType Event), the durable execution ARN is grabbed from the response and the execution
        /// is then monitored by polling the GetDurableExecution and GetDurableExecutionHistory APIs until
        /// it completes. If the supplied function name is not an ARN the latest published version is resolved
        /// and its ARN is used for the invocation.
        /// </summary>
        private async Task InvokeDurableExecutionAsync(string functionName, string payload)
        {
            var resolvedFunctionArn = await ResolveDurableFunctionArnAsync(functionName);

            var invokeRequest = new InvokeRequest
            {
                FunctionName = resolvedFunctionArn,
                InvocationType = InvocationType.Event,
                Payload = payload
            };

            InvokeResponse response;
            try
            {
                response = await this.LambdaClient.InvokeAsync(invokeRequest);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error invoking Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction, e);
            }

            if (!string.IsNullOrEmpty(response.FunctionError))
            {
                this.Logger.WriteLine($"Function error: {response.FunctionError}");
            }

            if (string.IsNullOrEmpty(response.DurableExecutionArn))
            {
                throw new LambdaToolsException(
                    "The function invocation did not return a durable execution ARN. Confirm the function is configured for durable execution.",
                    LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction);
            }

            this.Logger.WriteLine($"Durable execution ARN: {response.DurableExecutionArn}");

            await MonitorDurableExecutionAsync(response.DurableExecutionArn);
        }

        /// <summary>
        /// Resolves the function ARN to use for a durable execution invocation. If the supplied value is already
        /// an ARN it is returned unchanged. Otherwise the latest published version is looked up and its ARN is used.
        /// If the function has no published versions an error is reported.
        /// </summary>
        private async Task<string> ResolveDurableFunctionArnAsync(string functionName)
        {
            if (functionName.StartsWith("arn:", StringComparison.OrdinalIgnoreCase))
            {
                return functionName;
            }

            FunctionConfiguration latestVersion = null;
            try
            {
                string marker = null;
                do
                {
                    var listResponse = await this.LambdaClient.ListVersionsByFunctionAsync(new ListVersionsByFunctionRequest
                    {
                        FunctionName = functionName,
                        Marker = marker
                    });

                    if (listResponse.Versions != null)
                    {
                        foreach (var version in listResponse.Versions)
                        {
                            // Skip the $LATEST pseudo-version, only published numeric versions are considered.
                            if (!long.TryParse(version.Version, out var versionNumber))
                            {
                                continue;
                            }

                            if (latestVersion == null || versionNumber > long.Parse(latestVersion.Version))
                            {
                                latestVersion = version;
                            }
                        }
                    }

                    marker = listResponse.NextMarker;
                } while (!string.IsNullOrEmpty(marker));
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error looking up versions for Lambda function {functionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaListVersions, e);
            }

            if (latestVersion == null)
            {
                throw new LambdaToolsException(
                    $"No published versions were found for Lambda function {functionName}. A published version is required to invoke a durable execution.",
                    LambdaToolsException.LambdaErrorCode.NoFunctionVersionsFound);
            }

            this.Logger.WriteLine($"Resolved latest version {latestVersion.Version} for function {functionName} to ARN: {latestVersion.FunctionArn}");
            return latestVersion.FunctionArn;
        }

        /// <summary>
        /// The delay between polls of the durable execution status while monitoring its progress.
        /// </summary>
        protected virtual TimeSpan PollInterval => TimeSpan.FromSeconds(3);

        /// <summary>
        /// Waits <see cref="PollInterval"/> between durable execution status polls. Exposed as a virtual method so
        /// tests can override it to avoid slowing down the suite.
        /// </summary>
        protected virtual Task PollDelayAsync() => Task.Delay(PollInterval);

        /// <summary>
        /// Polls the GetDurableExecution and GetDurableExecutionHistory APIs to monitor a durable execution,
        /// writing new history events and status changes to the console until the execution reaches a terminal state.
        /// </summary>
        private async Task MonitorDurableExecutionAsync(string durableExecutionArn)
        {
            this.Logger.WriteLine("");
            this.Logger.WriteLine("Monitoring durable execution progress:");

            var seenEventIds = new HashSet<int>();
            GetDurableExecutionResponse execution = null;

            while (true)
            {
                try
                {
                    execution = await this.LambdaClient.GetDurableExecutionAsync(new GetDurableExecutionRequest
                    {
                        DurableExecutionArn = durableExecutionArn
                    });

                    await WriteNewHistoryEventsAsync(durableExecutionArn, seenEventIds);
                }
                catch (ResourceNotFoundException)
                {
                    // It is possible for the durable execution to not be immediately visible after the invocation returns, so treat a not found error as an empty execution history and keep polling until it appears.
                    await PollDelayAsync();
                    continue;
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException($"Error monitoring durable execution: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaGetDurableExecution, e);
                }

                if (execution.Status != ExecutionStatus.RUNNING)
                {
                    break;
                }

                await PollDelayAsync();
            }

            this.Logger.WriteLine("");
            this.Logger.WriteLine($"Durable execution finished with status: {execution.Status}");

            if (!string.IsNullOrEmpty(execution.Result))
            {
                this.Logger.WriteLine("Result:");
                this.Logger.WriteLine(execution.Result);
            }

            if (execution.Error != null)
            {
                this.Logger.WriteLine("Error:");
                if (!string.IsNullOrEmpty(execution.Error.ErrorType))
                {
                    this.Logger.WriteLine($"   Type: {execution.Error.ErrorType}");
                }
                if (!string.IsNullOrEmpty(execution.Error.ErrorMessage))
                {
                    this.Logger.WriteLine($"   Message: {execution.Error.ErrorMessage}");
                }
                if (execution.Error.StackTrace != null && execution.Error.StackTrace.Count > 0)
                {
                    this.Logger.WriteLine("   Stack Trace:");
                    foreach (var frame in execution.Error.StackTrace)
                    {
                        this.Logger.WriteLine($"      {frame}");
                    }
                }
            }
        }

        /// <summary>
        /// Fetches the durable execution history and writes any events that have not already been written to the console.
        /// </summary>
        private async Task WriteNewHistoryEventsAsync(string durableExecutionArn, HashSet<int> seenEventIds)
        {
            string marker = null;
            do
            {
                var historyResponse = await this.LambdaClient.GetDurableExecutionHistoryAsync(new GetDurableExecutionHistoryRequest
                {
                    DurableExecutionArn = durableExecutionArn,
                    IncludeExecutionData = true,
                    Marker = marker
                });

                if (historyResponse.Events != null)
                {
                    foreach (var historyEvent in historyResponse.Events)
                    {
                        if (historyEvent.EventId.HasValue && !seenEventIds.Add(historyEvent.EventId.Value))
                        {
                            continue;
                        }

                        var timestamp = historyEvent.EventTimestamp.HasValue
                            ? historyEvent.EventTimestamp.Value.ToString("u")
                            : string.Empty;
                        this.Logger.WriteLine($"   [{timestamp}] {historyEvent.Name}: {historyEvent.EventType}".TrimEnd());

                        WriteEventDetails(historyEvent);
                    }
                }

                marker = historyResponse.NextMarker;
            } while (!string.IsNullOrEmpty(marker));
        }

        /// <summary>
        /// The maximum number of characters of a string field that will be written to the console. Larger string
        /// payload fields in the durable execution details are truncated to this length.
        /// </summary>
        private const int MAX_PAYLOAD_DISPLAY_LENGTH = 1000;

        // Indentation used when writing the details of a durable execution history event.
        private const string DETAILS_INDENT = "      ";
        private const string DETAILS_INDENT2 = "         ";

        /// <summary>
        /// Each durable execution history <see cref="Event"/> carries at most one populated "*Details" object
        /// describing what happened (e.g. ExecutionStartedDetails, StepSucceededDetails). Inspect each known detail
        /// type and write out its contents.
        /// </summary>
        private void WriteEventDetails(Event historyEvent)
        {
            if (historyEvent.ExecutionStartedDetails != null)
            {
                var d = historyEvent.ExecutionStartedDetails;
                WriteValue("Execution Timeout", d.ExecutionTimeout);
                WriteEventInput(d.Input);
            }
            else if (historyEvent.ExecutionSucceededDetails != null)
            {
                WriteEventResult(historyEvent.ExecutionSucceededDetails.Result);
            }
            else if (historyEvent.ExecutionFailedDetails != null)
            {
                WriteEventError(historyEvent.ExecutionFailedDetails.Error);
            }
            else if (historyEvent.ExecutionStoppedDetails != null)
            {
                WriteEventError(historyEvent.ExecutionStoppedDetails.Error);
            }
            else if (historyEvent.ExecutionTimedOutDetails != null)
            {
                WriteEventError(historyEvent.ExecutionTimedOutDetails.Error);
            }
            else if (historyEvent.StepStartedDetails != null)
            {
                // StepStartedDetails has no fields.
            }
            else if (historyEvent.StepSucceededDetails != null)
            {
                var d = historyEvent.StepSucceededDetails;
                WriteEventResult(d.Result);
                WriteRetryDetails(d.RetryDetails);
            }
            else if (historyEvent.StepFailedDetails != null)
            {
                var d = historyEvent.StepFailedDetails;
                WriteEventError(d.Error);
                WriteRetryDetails(d.RetryDetails);
            }
            else if (historyEvent.WaitStartedDetails != null)
            {
                var d = historyEvent.WaitStartedDetails;
                WriteValue("Duration", d.Duration);
                WriteValue("Scheduled End Timestamp", d.ScheduledEndTimestamp);
            }
            else if (historyEvent.WaitSucceededDetails != null)
            {
                WriteValue("Duration", historyEvent.WaitSucceededDetails.Duration);
            }
            else if (historyEvent.WaitCancelledDetails != null)
            {
                WriteEventError(historyEvent.WaitCancelledDetails.Error);
            }
            else if (historyEvent.CallbackStartedDetails != null)
            {
                var d = historyEvent.CallbackStartedDetails;
                WriteValue("Callback Id", d.CallbackId);
                WriteValue("Heartbeat Timeout", d.HeartbeatTimeout);
                WriteValue("Timeout", d.Timeout);
            }
            else if (historyEvent.CallbackSucceededDetails != null)
            {
                WriteEventResult(historyEvent.CallbackSucceededDetails.Result);
            }
            else if (historyEvent.CallbackFailedDetails != null)
            {
                WriteEventError(historyEvent.CallbackFailedDetails.Error);
            }
            else if (historyEvent.CallbackTimedOutDetails != null)
            {
                WriteEventError(historyEvent.CallbackTimedOutDetails.Error);
            }
            else if (historyEvent.ChainedInvokeStartedDetails != null)
            {
                var d = historyEvent.ChainedInvokeStartedDetails;
                WriteValue("Function Name", d.FunctionName);
                WriteValue("Executed Version", d.ExecutedVersion);
                WriteValue("Durable Execution Arn", d.DurableExecutionArn);
                WriteValue("Tenant Id", d.TenantId);
                WriteEventInput(d.Input);
            }
            else if (historyEvent.ChainedInvokeSucceededDetails != null)
            {
                WriteEventResult(historyEvent.ChainedInvokeSucceededDetails.Result);
            }
            else if (historyEvent.ChainedInvokeFailedDetails != null)
            {
                WriteEventError(historyEvent.ChainedInvokeFailedDetails.Error);
            }
            else if (historyEvent.ChainedInvokeStoppedDetails != null)
            {
                WriteEventError(historyEvent.ChainedInvokeStoppedDetails.Error);
            }
            else if (historyEvent.ChainedInvokeTimedOutDetails != null)
            {
                WriteEventError(historyEvent.ChainedInvokeTimedOutDetails.Error);
            }
            else if (historyEvent.ContextStartedDetails != null)
            {
                // ContextStartedDetails has no fields.
            }
            else if (historyEvent.ContextSucceededDetails != null)
            {
                WriteEventResult(historyEvent.ContextSucceededDetails.Result);
            }
            else if (historyEvent.ContextFailedDetails != null)
            {
                WriteEventError(historyEvent.ContextFailedDetails.Error);
            }
            else if (historyEvent.InvocationCompletedDetails != null)
            {
                var d = historyEvent.InvocationCompletedDetails;
                WriteValue("Request Id", d.RequestId);
                WriteValue("Start Timestamp", d.StartTimestamp);
                WriteValue("End Timestamp", d.EndTimestamp);
                WriteEventError(d.Error);
            }
        }

        /// <summary>
        /// Writes the payload of an <see cref="EventInput"/>. If the payload is null nothing is written so the
        /// Truncated flag is never reported on its own.
        /// </summary>
        private void WriteEventInput(EventInput input)
        {
            if (input?.Payload == null)
            {
                return;
            }

            WritePayload("Input", input.Payload, input.Truncated);
        }

        /// <summary>
        /// Writes the payload of an <see cref="EventResult"/>. If the payload is null nothing is written so the
        /// Truncated flag is never reported on its own.
        /// </summary>
        private void WriteEventResult(EventResult result)
        {
            if (result?.Payload == null)
            {
                return;
            }

            WritePayload("Result", result.Payload, result.Truncated);
        }

        /// <summary>
        /// Writes the contents of an <see cref="EventError"/>. If the error payload is null nothing is written so the
        /// Truncated flag is never reported on its own.
        /// </summary>
        private void WriteEventError(EventError error)
        {
            var errorObject = error?.Payload;
            if (errorObject == null)
            {
                return;
            }

            this.Logger.WriteLine($"{DETAILS_INDENT}Error:");
            if (!string.IsNullOrEmpty(errorObject.ErrorType))
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}Type: {errorObject.ErrorType}");
            }
            if (!string.IsNullOrEmpty(errorObject.ErrorMessage))
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}Message: {Truncate(errorObject.ErrorMessage)}");
            }
            if (!string.IsNullOrEmpty(errorObject.ErrorData))
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}Data: {Truncate(errorObject.ErrorData)}");
            }
            if (errorObject.StackTrace != null && errorObject.StackTrace.Count > 0)
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}Stack Trace:");
                foreach (var frame in errorObject.StackTrace)
                {
                    this.Logger.WriteLine($"{DETAILS_INDENT2}   {frame}");
                }
            }

            // Only note truncation when there was actually a payload, which is guaranteed here.
            if (error.Truncated == true)
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}(error payload was truncated by the service)");
            }
        }

        /// <summary>
        /// Writes the details of a <see cref="RetryDetails"/> if present.
        /// </summary>
        private void WriteRetryDetails(RetryDetails retryDetails)
        {
            if (retryDetails == null)
            {
                return;
            }

            WriteValue("Current Attempt", retryDetails.CurrentAttempt);
            WriteValue("Next Attempt Delay Seconds", retryDetails.NextAttemptDelaySeconds);
        }

        /// <summary>
        /// Writes a named payload string, truncating it for display, and noting when the service marked the payload
        /// as truncated. The caller must only invoke this when the payload is non-null.
        /// </summary>
        private void WritePayload(string label, string payload, bool? truncatedByService)
        {
            this.Logger.WriteLine($"{DETAILS_INDENT}{label}: {Truncate(payload)}");

            // Only report the service truncation flag here where we know the payload was present.
            if (truncatedByService == true)
            {
                this.Logger.WriteLine($"{DETAILS_INDENT2}({label.ToLowerInvariant()} payload was truncated by the service)");
            }
        }

        /// <summary>
        /// Writes a labelled value if it is not null or empty.
        /// </summary>
        private void WriteValue(string label, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                this.Logger.WriteLine($"{DETAILS_INDENT}{label}: {value}");
            }
        }

        /// <summary>
        /// Writes a labelled nullable value if it has a value.
        /// </summary>
        private void WriteValue<T>(string label, T? value) where T : struct
        {
            if (value.HasValue)
            {
                this.Logger.WriteLine($"{DETAILS_INDENT}{label}: {value.Value}");
            }
        }

        /// <summary>
        /// Truncates a string to <see cref="MAX_PAYLOAD_DISPLAY_LENGTH"/> characters, appending a note when the value
        /// was truncated so the user knows the payload was not shown in full.
        /// </summary>
        private static string Truncate(string value)
        {
            if (value != null && value.Length > MAX_PAYLOAD_DISPLAY_LENGTH)
            {
                return value.Substring(0, MAX_PAYLOAD_DISPLAY_LENGTH) +
                    $"... [payload truncated, showing first {MAX_PAYLOAD_DISPLAY_LENGTH} of {value.Length} characters]";
            }

            return value;
        }

        /// <summary>
        /// Invoke the function and stream the response payload to the console as it is produced by the function.
        /// </summary>
        private async Task InvokeWithResponseStreamAsync(string functionName, string payload)
        {
            var invokeRequest = new InvokeWithResponseStreamRequest
            {
                FunctionName = functionName,
                LogType = LogType.Tail
            };

            if (!string.IsNullOrEmpty(payload))
            {
                invokeRequest.Payload = new MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes(payload));
            }

            this.Logger.WriteLine("Payload:");

            string logResult = null;
            string errorCode = null;
            string errorDetails = null;
            try
            {
                // Use a Decoder to preserve state across chunks so multi-byte UTF-8 characters that are split
                // across chunk boundaries are decoded correctly instead of producing replacement characters.
                var decoder = System.Text.UTF8Encoding.UTF8.GetDecoder();
                using (var response = await this.LambdaClient.InvokeWithResponseStreamAsync(invokeRequest))
                {
                    foreach (var streamEvent in response.EventStream)
                    {
                        if (streamEvent is InvokeResponseStreamUpdate update && update.Payload != null)
                        {
                            var bytes = update.Payload.ToArray();
                            var chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length, false)];
                            var charCount = decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
                            if (charCount > 0)
                            {
                                this.Logger.Write(new string(chars, 0, charCount));
                            }
                        }
                        else if (streamEvent is InvokeWithResponseStreamCompleteEvent complete)
                        {
                            logResult = complete.LogResult;
                            errorCode = complete.ErrorCode;
                            errorDetails = complete.ErrorDetails;
                        }
                    }
                }

                // Flush any remaining buffered bytes from the decoder.
                var tail = new char[decoder.GetCharCount(Array.Empty<byte>(), 0, 0, true)];
                var tailCount = decoder.GetChars(Array.Empty<byte>(), 0, 0, tail, 0, true);
                if (tailCount > 0)
                {
                    this.Logger.Write(new string(tail, 0, tailCount));
                }
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error invoking Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction, e);
            }

            // Terminate the streamed payload with a newline so following output is on its own line.
            this.Logger.WriteLine("");

            if (!string.IsNullOrEmpty(errorCode))
            {
                this.Logger.WriteLine($"Function error: {errorCode}");
                if (!string.IsNullOrEmpty(errorDetails))
                {
                    this.Logger.WriteLine(errorDetails);
                }
            }

            if (!string.IsNullOrEmpty(logResult))
            {
                this.Logger.WriteLine("");
                this.Logger.WriteLine("Log Tail:");
                var log = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(logResult));
                this.Logger.WriteLine(log);
            }
        }


        private void PrintPayload(InvokeResponse response)
        {
            try
            {
                var payload = new StreamReader(response.Payload).ReadToEnd();
                this.Logger.WriteLine(payload);
            }
            catch (Exception)
            {
                this.Logger.WriteLine("<unparseable data>");
            }
        }

        protected override void SaveConfigFile(Dictionary<string, object> data)
        {
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD.ConfigFileKey, this.GetStringValueOrDefault(this.Payload, LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_INVOKE_MODE.ConfigFileKey, this.Mode?.ToString());
        }
    }
}
