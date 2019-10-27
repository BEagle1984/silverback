// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

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
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}")
                .MinimumLevel.Warning()
                .MinimumLevel.Override("Silverback", LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithDemystifiedStackTraces()
                .CreateLogger();
        }
    }
}
