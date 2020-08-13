---
uid: kafka-consumer-groups
---

# Multiple Consumer Groups (in same process)

In some cases you may want to consume multiple times the same topic, to perform independent tasks. You achieve this by attaching multiple consumers to the same topic, using a different consumer group id.

# [Startup](#tab/startup)
```csharp
public class Startup
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddInboundConnector()
                .AddOutboundConnector());
    }
}
```
# [EndpointsConfigurator](#tab/configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
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
                });
    }
}
```
***

By default Silverback would call every matching subscriber method for each message, regardless of the consumer group and you basically have two choices to work around this: using the `KafkaGroupIdFilterAttribute` or manually inspecting the `IInboundEnvelope`.

## Using the attribute

```csharp
public class MySubscriber
{
    [KafkaGroupIdFilter("group1")]
    private void PerformTask1(MyEvent @event) => ...

    [KafkaGroupIdFilter("group2")]
    private void PerformTask2(MyEvent @event) => ...
}
```

## Subscribing to the IInboundEnvelope

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
