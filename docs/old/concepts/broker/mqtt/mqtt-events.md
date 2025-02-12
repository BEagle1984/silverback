---
uid: mqtt-events
---

# MQTT Events

Some lifetime events are fired by the <xref:Silverback.Messaging.Broker.MqttBroker> and can be handled using the following callbacks:
* <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientConnectedCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientDisconnectingCallback>

## Example

In the following example a message is sent as soon as the client is connected.

# [Startup](#tab/example-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMqtt())
            .AddSingletonBrokerCallbacksHandler<ConnectionCallbackHandler>();
    }
}
```
# [ConnectionCallbackHandler](#tab/example-handler)
```csharp
public class ConnectionCallbackHandler
    : IMqttClientConnectedCallback
{
    private readonly IPublisher _publisher;
    
    public ConnectionCallbackHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }
            
    public Task OnClientConnectedAsync(MqttClientConfig config) =>
        _publisher.PublishAsync(new ClientConnectedMessage());
}
```
***

## See also

<xref:broker-callbacks>
