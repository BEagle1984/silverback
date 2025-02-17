---
uid: broker-callbacks
---

# Broker Callbacks

The callbacks are used to notify some events happening during the lifecycle of a message broker client. 

An interface has to be implemented by the callback handler that is then registered via the `Add*BrokerCallbacksHandler` methods.

## Generic

The only generic callback, invoked for any of the actual broker implementation is:
* <xref:Silverback.Messaging.Broker.Callbacks.IEndpointsConfiguredCallback>

Some broker specific callbacks may be added by the specific broker implementation (see <xref:broker-callbacks> and <xref:mqtt-events>).

### Example

In the following example an handler for the <xref:Silverback.Messaging.Broker.Callbacks.IEndpointsConfiguredCallback> is being registered.

# [Startup](#tab/example-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddSingletonBrokerCallbacksHandler<EndpointsConfiguredCallbackHandler>();
    }
}
```
# [EndpointsConfiguredCallbackHandler](#tab/example-handler)
```csharp
public class EndpointsConfiguredCallbackHandler
    : IKafkaPartitionsAssignedCallback
{
    public Task OnEndpointsConfiguredAsync()
    {
        // Perform some initialization logic, 
        // e.g. create the missing topics
    } 
}
```
***
