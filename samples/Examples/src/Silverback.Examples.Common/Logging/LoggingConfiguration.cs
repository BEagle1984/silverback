// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

namespace Silverback.Examples.Common.Logging
{
    public static class LoggingConfiguration
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Here for future use")]
        [SuppressMessage("ReSharper", "CA1823", Justification = "Here for future use")]
        private const string VerboseOutputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj} {Exception} {Properties}{NewLine}";

        private const string CompactOutputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Exception} ({ActivityId}){NewLine}";

        public static void Setup()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: CompactOutputTemplate)
                .MinimumLevel.Warning()
                .MinimumLevel.Override("Silverback", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithActivityId()
                .CreateLogger();
        }
    }
}
