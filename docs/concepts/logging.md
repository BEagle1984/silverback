---
uid: logging
---

# Logging

Silverback logs quite a few events that may be very useful for troubleshooting. Of course `ILogger` is used for logging and will work with any logger you wired in the startup.

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

Each package (that logs some events) has a static class exposing the `EventId` constants. See <xref:Silverback.Diagnostics.CoreEventIds>, <xref:Silverback.Diagnostics.IntegrationEventIds>, <xref:Silverback.Diagnostics.KafkaEventIds> and <xref:Silverback.Diagnostics.RabbitEventIds> for details about the events being logged and their default log level.
