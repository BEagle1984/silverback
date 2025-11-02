---
uid: consumer-health-check
---

# Consumers Health Check

A health check is available to monitor the consumers and alert if they don't appear to be connected or consuming any messages.

```csharp
.AddHealthChecks()
.AddConsumersCheck()
```

A few parameters such as the grace period to wait for the consumers to be connected can be configured. See <xref:Silverback.Messaging.HealthChecks.HealthCheckBuilderExtensions> for details.

## Additional Resources

* [API Reference](xref:Silverback)
