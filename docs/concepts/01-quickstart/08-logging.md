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
                .SetLogLevel(EventIds.SkipMessagePolicyMessageSkipped, LogLevel.Critical)
                .SetLogLevel(EventIds.ProcessingInboundMessageError, LogLevel.Error));
    }
}
```
***

> [!Note]
> The `EventIds` static class contains the constants for all events that are being logged by Silverback.
