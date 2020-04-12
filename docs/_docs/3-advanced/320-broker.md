---
title: Using IBroker
permalink: /docs/advanced/broker
---

It may be useful to use the lower level `IBroker` and related `IConsumer` and `IProducer` to implement some more advanced use cases.

Using `IBroker` directly you get full control over message acknowledgement and you can implement your fully customized logics, taking advantage of Rx.NET if you wish.

## Configuration

The only required service is the broker.

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
                .AddKafka());
    }
}
{% endhighlight %}
</figure>

## Consumer

Retrieving an `IConsumer` and start consuming messages is just a matter of a few lines of code.

```csharp
public class MyCustomMessageProcessor
{
    private readonly IConsumer _consumer;

    public MyCustomMessageProcessor(KafkaBroker broker)
    {
        _consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("topic-name")
        {
            Configuration = new KafkaConsumerConfig
            {
                BootstrapServers = "PLAINTEXT://kafka:9092"
            }
        });

        _consumer.Received += OnMessageReceived;

        _broker.Connect();
    }

    private static void OnMessageReceived(object sender, ReceivedMessageEventArgs args)
    {
        foreach (var envelope in args.Envelopes)
        {
            // ...your processing logic...
        }
    }
}
```

A single call to the `Connect` method is required for each broker and `GetConsumer` must be called before `Connect`.
{: .notice--note}

The messages are acknowledged automatically if the `Received` event handlers return without exceptions. To disable this behavior remove the `InboundProcessorConsumerBehavior` from the `IServiceCollection` (you will need to manually handle the DI scope as well).
{: .notice--important}

### Wrapping with an Observable

You can of course very easily wrap the `Received` event with an `Observable` and enjoy the uncountable possibilities of [Rx.net](https://github.com/dotnet/reactive).

```csharp
var consumer = _broker.GetConsumer(...);

Observable.FromEventPattern<MessageReceivedEventArgs>(
    h => consumer.Received += h,
    h => consumer.Received -= h)
.Subscribe(args => 
{
    Process(args.Message);
    consumer.Acknowledge(args.Offset);
});
```

The `Achknowledge` method can be used to commit one or more messages at once.
{: .notice--note}

## Producer

The producer works exactly the same as the consumer.

```csharp
var producer = _broker.GetProducer(new KafkaProducerEndpoint("topic-name")
{
    Configuration = new KafkaProducerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092"
    }
});

await producer.ProduceAsync(message, headers);
```

`GetProducer` (unlike `GetConsumer`) can be called at any time, even after `Connect` as been called.
{: .notice--note}