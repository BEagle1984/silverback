---
title: Multiple Consumer Groups (in same process)
permalink: /docs/kafka/multiple-consumer-groups
toc: true
---

In some cases you may want to consume multiple times the same topic, to perform independent tasks. You achieve this by attaching multiple consumers to the same topic, using a different consumer group id.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddInboundConnector()
                .AddOutboundConnector());
    }

    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
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
                    }));
}
{% endhighlight %}
</figure>

By default Silverback would call every matching subscriber method for each message, regardless of the consumer group and you basically have two choices to work around this: using the `KafkaGroupIdFilterAttribute` or manually inspecting the `IInboundEnvelope`.

## Using the attribute

```csharp
public class MySubscriber : ISubscriber
{
    [KafkaGroupIdFilter("group1")]
    private void PerformTask1(MyEvent @event) => ...

    [KafkaGroupIdFilter("group2")]
    private void PerformTask2(MyEvent @event) => ...
}
```

## Subscribing to the IInboundEnvelope

```csharp
public class MySubscriber : ISubscriber
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
