---
uid: logging
---

# Logging

Silverback logs quite a few events that may be very useful for troubleshooting. Of course [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger) is used for logging and will work with any logger you wired in the startup.

The `WithLogLevels` configuration method can be used to tweak the log levels of each particular event.

# [Startup](#tab/ibehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithLogLevels(configurator => configurator
                .SetLogLevel(IntegrationEventIds.MessageSkipped, LogLevel.Critical)
                .SetLogLevel(IntegrationEventIds.ErrorProcessingInboundMessage, LogLevel.Error));
    }
}
```
***

Each package (that writes any log) has a static class declaring each log event: default [LogLevel](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel), [EventId](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.eventid) and message. Refer to those classes for details about the events being logged and their default log level.

See:
* <xref:Silverback.Diagnostics.CoreLogEvents>
* <xref:Silverback.Diagnostics.IntegrationLogEvents>
* <xref:Silverback.Diagnostics.KafkaEvents>
* <xref:Silverback.Diagnostics.RabbitEvents>
