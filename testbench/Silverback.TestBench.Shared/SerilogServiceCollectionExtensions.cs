// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Silverback.TestBench;

public static class SerilogServiceCollectionExtensions
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} - " +
                                          "EventId: {EventId}, " +
                                          "TraceId: {TraceId}, " +
                                          "SpanId: {SpanId}, " +
                                          "Tags: {ActivityTags}" +
                                          "{NewLine}{Exception}";

    public static IServiceCollection AddSerilog(this IServiceCollection services, string logPath) =>
        services.AddSerilog(
            logger => logger
                .MinimumLevel.Information()
                .MinimumLevel.Override("Silverback", LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .Enrich.WithActivityTags()
                .WriteTo.File(
                    logPath,
                    formatProvider: CultureInfo.InvariantCulture,
                    outputTemplate: OutputTemplate));
}
