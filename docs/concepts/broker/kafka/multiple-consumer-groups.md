---
uid: kafka-consumer-groups
---

# Multiple Consumer Groups (in same process)

In some cases you may want to consume subscribe multiple times the same consumed message, to perform independent tasks. Having multiple subscribers handling the very same message is not a good idea since a failure in one of them will cause the message to be consumed again and thus reprocessed by all subscribers.

A much safer approach is to bind multiple consumers to the same topic, using a different consumer group id. This will cause the message to be consumed multiple times (once per consumer group) and being committed independently. The <xref:Silverback.Messaging.Subscribers.KafkaGroupIdFilterAttribute> can be used to execute a subscribed method according to the group id.

# [EndpointsConfigurator (fluent)](#tab/groups-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("document-events")
                    .Configure(config =>
                        {
                            config.GroupId = "group1";
                        }))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("document-events")
                    .Configure(config =>
                        {
                            config.GroupId = "group2";
                        })));
}
```
# [EndpointsConfigurator (legacy)](#tab/groups-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("document-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "group1"
                    }
                })
            .AddInbound(
                new KafkaConsumerEndpoint("document-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "group2"
                    }
                });;
}
```
# [Subscriber](#tab/groups-subscriber)
```csharp
public class MySubscriber
{
    [KafkaGroupIdFilter("group1")]
    public void PerformTask1(MyEvent @event) => ...

    [KafkaGroupIdFilter("group2")]
    public void PerformTask2(MyEvent @event) => ...
}
```
***

Using the <xref:Silverback.Messaging.Subscribers.KafkaGroupIdFilterAttribute> is the cleanest and easiest approach but alternatively you can always subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and perform different tasks according to the `GroupId` value.

```csharp
public class MySubscriber
{
    public void OnMessageReceived(IInboundEnvelope<MyEvent> envelope)
    {
        switch (((KafkaConsumerEndpoint)envelope.Endpoint).Configuration.GroupId)
        {
            case "group1":
                PerformTask1(envelope.Message);
                break;
            case "group2":
                PerformTask2(envelope.Message);
                break;
        }
    }

    private void PerformTask1(MyEvent @event) => ...

    private void PerformTask2(MyEvent @event) => ...
}
```
