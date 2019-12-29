// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;
using Silverback.Examples.Common.Logging;

namespace Silverback.Examples.Common
{
    public static class Configuration
    {
        public const string ConnectionString = @"Data Source=.,1433;Initial Catalog=Silverback.Examples;User ID=sa;Password=mssql2017.;";

        public static void SetupSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj} {Exception} {Properties}{NewLine}")
                .MinimumLevel.Warning()
                .MinimumLevel.Override("Silverback", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.WithActivityId()
                .CreateLogger();
        }
    }
}
