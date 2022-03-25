using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CODE.Framework.Services.Contracts
{
    public static class ServiceHelper
    {
        public static PingResponse GetPopulatedPingResponse(this object referenceObject)
        {
            try
            {
                return new PingResponse
                {
                    ServerDateTime = DateTime.Now, 
                    Version = referenceObject?.GetType().Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                    OperatingSystemDescription = RuntimeInformation.OSDescription,
                    FrameworkDescription = RuntimeInformation.FrameworkDescription,
                    Success = true
                };
            }
            catch
            {
                return new PingResponse
                {
                    Success = false,
                    FailureInformation = "PingService::GetPopulatedPingResponse() - generic error."
                };
            }
        }

        public static bool ShowExtendedFailureInformation { get; set; } = false;

        public static TResponse GetPopulatedFailureResponse<TResponse>(Exception ex) where TResponse: new()
        {
            var response = new TResponse();

            var frame = new StackFrame(1);
            var message = ShowExtendedFailureInformation ? GetExceptionText(ex) : $"Generic error in {frame.GetMethod().DeclaringType.Name}::{frame.GetMethod().Name}";

            if (response is BaseServiceResponse baseResponse)
            {
                baseResponse.Success = false;
                baseResponse.FailureInformation = message;
            }
            else
            {
                var responseType = response.GetType();
                responseType?.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)?.SetValue(response, false);
                responseType?.GetProperty("FailureInformation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)?.SetValue(response, message);
            }

            var loggingMediatorType = Type.GetType("CODE.Framework.Fundamentals.Utilities.LoggingMediator, CODE.Framework.Fundamentals");
            if (loggingMediatorType != null)
            {
                var logMethods = loggingMediatorType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var logMethod in logMethods)
                    if (logMethod.Name == "Log")
                    {
                        var parameters = logMethod.GetParameters();
                        if (parameters.Length == 3 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(Exception) && parameters[2].ParameterType.Name == "LogEventType")
                        {
                            var callParameters = new object[3];
                            callParameters[0] = $"Generic error in {frame.GetMethod().DeclaringType.Name}::{frame.GetMethod().Name} - {ex.GetType().Name}";
                            callParameters[1] = ex;
                            callParameters[2] = 4; // Exception
                            logMethod.Invoke(null, callParameters);
                            break;
                        }
                    }
            }

            return response;
        }

        //private static string _isDebug = string.Empty;
        //public static bool IsDebug()
        //{
        //    if (!string.IsNullOrEmpty(_isDebug)) return _isDebug == "YES";

        //    var assembly = Assembly.GetEntryAssembly();
        //    if (assembly != null)
        //    {
        //        var attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), true);
        //        if (attributes.Length == 0)
        //        {
        //            _isDebug = "NO";
        //            return false;
        //        }

        //        var debuggableAttribute = (DebuggableAttribute)attributes[0];
        //        var debuggableAttributeIsJitTrackingEnabled = debuggableAttribute.IsJITTrackingEnabled;
        //        _isDebug = debuggableAttributeIsJitTrackingEnabled ? "YES" : "NO";
        //        return debuggableAttributeIsJitTrackingEnabled;
        //    }

        //    _isDebug = "NO";
        //    return false;
        //}

        private static string GetExceptionText(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Exception Stack:");
            sb.AppendLine();
            var errorCount = -1;
            while (exception != null)
            {
                errorCount++;
                if (errorCount > 0) sb.AppendLine();
                sb.Append(exception.Message);

                sb.AppendLine("  Exception Attributes:");
                sb.AppendLine($"    Message {exception.Message}");
                sb.AppendLine($"    Exception Type: {exception.GetType().Name}");
                sb.AppendLine($"    Source: {exception.Source}");

                if (exception.TargetSite != null)
                {
                    sb.AppendLine($"    Thrown by Method: {exception.TargetSite.Name}");
                    if (exception.TargetSite.DeclaringType != null)
                        sb.AppendLine($"    Thrown by Class: {exception.TargetSite.DeclaringType.Name}");
                }

                // Stack Trace
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    sb.AppendLine("  Stack Trace:");
                    var stackLines = exception.StackTrace.Split('\r');
                    foreach (var stackLine in stackLines)
                        if (!string.IsNullOrEmpty(stackLine))
                            if (stackLine.IndexOf(" in ", StringComparison.Ordinal) > -1)
                            {
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                var at = detail.IndexOf(" in ", StringComparison.Ordinal);
                                var file = detail.Substring(at + 4);
                                detail = detail.Substring(0, at);
                                at = file.IndexOf(":line", StringComparison.Ordinal);
                                var lineNumber = file.Substring(at + 6);
                                file = file.Substring(0, at);
                                sb.Append($"    Line Number: {lineNumber} -- ");
                                sb.Append($"Method: {detail} -- ");
                                sb.Append($"Source File: {file}\r\n");
                            }
                            else
                            {
                                // We only have generic info
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                sb.Append($"    Method: {detail}");
                            }
                }

                // Next exception
                exception = exception.InnerException;
            }
            return sb.ToString();
        }
    }
}