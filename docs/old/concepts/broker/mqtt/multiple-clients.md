---
uid: mqtt-client-ids
---

# Multiple Clients (in same process)

In some cases you may want to subscribe multiple times the same consumed message, to perform independent tasks. Having multiple subscribers handling the very same message is not a good idea since a failure in one of them will cause the message to be consumed again and thus reprocessed by all subscribers.

A much safer approach is to bind multiple consumers to the same topic, using a different client id. This will cause the message to be consumed multiple times (once per client) and being committed independently. The <xref:Silverback.Messaging.Subscribers.MqttClientIdFilterAttribute> can be used to execute a subscribed method according to the client id.

# [EndpointsConfigurator (fluent)](#tab/clients-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddMqttEndpoints(endpoints => endpoints
                .Configure(
                    config => config
                        .ConnectViaTcp("localhost"))
                .AddInbound(endpoint => endpoint
                    .Configure(config => config.WithClientId("client1"))
                    .ConsumeFrom("document-events"))
                .AddInbound(endpoint => endpoint
                    .Configure(config => config.WithClientId("client2"))
                    .ConsumeFrom("document-events")));
}
```
# [EndpointsConfigurator (legacy)](#tab/clients-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new MqttConsumerEndpoint("document-events")
                {
                    Configuration =
                    {
                        ClientId = "client1",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "localhost"
                        }
                    }
                })
            .AddInbound(
                new MqttConsumerEndpoint("document-events")
                {
                    Configuration =
                    {
                        ClientId = "client2",
                        ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = "localhost"
                        }
                    }
                });
}
```
# [Subscriber](#tab/clients-subscriber)
```csharp
public class MySubscriber
{
    [MqttClientIdFilter("client1")]
    public void PerformTask1(MyEvent @event) => ...

    [MqttClientIdFilter("client2")]
    public void PerformTask2(MyEvent @event) => ...
}
```
***

> [!Note]
> The filters can be added dynamically using the overloads of `AddSubscriber` accepting a <xref:Silverback.Messaging.Subscribers.Subscriptions.SubscriptionOptions> or <xref:Silverback.Messaging.Subscribers.Subscriptions.TypeSubscriptionOptions> and this allows you to use a variable for the client id.
>
> ```csharp
> .AddSingletonSubscriber<MySubscriber>(
>     new TypeSubscriptionOptions
>     {
>         Filters = new[]
>         {
>             new MqttClientIdFilterAttribute("client1")
>         }
>     })
> ```

Using the <xref:Silverback.Messaging.Subscribers.MqttClientIdFilterAttribute> is the cleanest and easiest approach but alternatively you can always subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and perform different tasks according to the `ClientId` value.

```csharp
public class MySubscriber
{
    public void OnMessageReceived(IInboundEnvelope<MyEvent> envelope)
    {
        switch (((MqttConsumerEndpoint)envelope.Endpoint).Configuration.ClientId)
        {
            case "client1":
                PerformTask1(envelope.Message);
                break;
            case "client2":
                PerformTask2(envelope.Message);
                break;
        }
    }

    private void PerformTask1(MyEvent @event) => ...

    private void PerformTask2(MyEvent @event) => ...
}
```
