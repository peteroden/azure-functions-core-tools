using Azure.Functions.Cli.Common;
using Fclp.Internals;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Azure.Functions.Cli.Helpers
{
    internal static class TelemetryHelpers
    {
        internal class ConsoleAppLogEvent
        {
            public string CommandName { get; set; }
            public string IActionName { get; set; }
            public IEnumerable<string> Parameters { get; set; }
            public bool PrefixOrScriptRoot { get; set; }
            public bool IsSuccessful { get; set; }
            public bool ParseError { get; set; }
            public long TimeTaken { get; set; }
        }

        public static IEnumerable<string> GetCommandsFromCommandLineOptions(IEnumerable<ICommandLineOption> options)
        {
            return options.Select(option => option.HasLongName ? option.LongName : option.ShortName);
        }

        public static string GetTelemetryUserID()
        {
            MD5 md5 = MD5.Create();
            byte[] userIDBytes = Encoding.Unicode.GetBytes($"{Environment.MachineName}:{Environment.UserName}");
            byte[] hashedID = md5.ComputeHash(userIDBytes);

            StringBuilder sb = new StringBuilder();
            // Append the bytes in hexadecimal form to the string builder
            hashedID.ToList().ForEach(bt => sb.Append(bt.ToString("x2")));
            return sb.ToString();
        }

        public static void LogEventIfAllowed(TelemetryClient tc, ConsoleAppLogEvent consoleEvent)
        {
            var telemetryOptOut = Environment.GetEnvironmentVariable(Constants.TelemtryOptOutVariable);
            if (string.IsNullOrEmpty(telemetryOptOut) || (!telemetryOptOut.Equals("true") && !telemetryOptOut.Equals("1")))
            {
                try
                {
                    LogEvent(tc, consoleEvent);
                }
                catch
                {
                    // If we can't log this event for some reason, it's ok.
                }
            }
        }

        private static void LogEvent(TelemetryClient tc, ConsoleAppLogEvent consoleEvent)
        {
            // If we didn't set the parameters, make it an empty list to avoid failure
            consoleEvent.Parameters = consoleEvent.Parameters ?? new List<string>();
            var properties = new Dictionary<string, string>
            {
                { "commandName" , consoleEvent.CommandName },
                { "iActionName" , consoleEvent.IActionName },
                { "parameters" , string.Join(",", consoleEvent.Parameters) },
                { "prefixOrScriptRoot" , consoleEvent.PrefixOrScriptRoot.ToString() },
                { "parseError" , consoleEvent.ParseError.ToString() },
                { "isSuccessful" , consoleEvent.IsSuccessful.ToString() }
            };

            var metrics = new Dictionary<string, double>
            {
                { "timeTaken" , consoleEvent.TimeTaken }
            };

            tc.TrackEvent(consoleEvent.CommandName, properties, metrics);
            tc.Flush();
        }
    }
}
