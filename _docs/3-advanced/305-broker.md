---
title: Using IBroker
permalink: /docs/advanced/broker
---

It may be useful to use the lower level `IBroker` and related `IConsumer` and `IProducer` to implement some more advanced use cases.

Using `IBroker` directly you get full control over message acknowledgement and you can implement your fully customized logics, taking advantage of Rx.NET if you wish.

## Configuration

The only required service is the broker.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddSilverback()
        .WithConnectionToMessageBroker(options => options
            .AddKafka());

}
```

## Consumer

Retrieving an `IConsumer` and start consuming messages is just a matter of a few lines of code.

```c#
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
        // Deserialize
        var message = args.Endpoint.Serializer.Deserialize(args.Message);

        // Process
        ...

        // Commit
        _consumer.Acknowledge(args.Offset);
    }
}
```

**Note:** A single call to the `Connect` method is required for each broker and `GetConsumer` must be called before `Connect`.
{: .notice--info}

### Wrapping with an Observable

You can of course very easily wrap the `Received` event with an `Observable` and enjoy the uncountable possibilities of [Rx.net](https://github.com/dotnet/reactive).

```c#
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

**Note:** The `Achknowledge` method can be used to commit one or more messages at once.
{: .notice--info}

## Producer

The producer works exactly the same as the consumer.

```c#
var producer = _broker.GetProducer(new KafkaProducerEndpoint("topic-name")
{
    Configuration = new KafkaProducerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092"
    }
});

producer.Produce(myMessage);
```

**Note:** `GetProducer` (unlike `GetConsumer`) can be called at any time, even after `Connect` as been called.
{: .notice--info}