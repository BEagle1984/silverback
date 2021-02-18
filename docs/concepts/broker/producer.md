---
uid: producer
---

# Producer

In some cases when high throughput is important you might want to skip the <xref:Silverback.Messaging.Publishing.IPublisher> and take advantage of the several options offered by the <xref:Silverback.Messaging.Broker.IProducer> interface.

## Producing pre serialized messages

A pre-serialized message can be produced via the normal `Produce`/`ProduceAsync` or the `RawProduce`/`RawProduceAsync` methods. The difference is that the latter skip the entire Silverback behaviors pipeline (note that it means that no [chunking](xref:chunking) or other features will kick in).

## Non-blocking overloads

These are especially important for Kafka, since the underlying library is able to batch the outgoing messages for efficiency and that improves the throughput a lot.
They will complete as soon as the message has been enqueued and invoke a callback when it is successfully produced (or when it fails / times out). These overloads exist for `Produce`, `ProduceAsync`, `RawProduce` and `RawProduceAsync`.

```csharp
public class ProducerService
{
    private readonly IProducer _producer;
    private readonly ILogger _logger;
    
    public ProducerService(
        IBroker broker,
        ILogger<ProducerService> logger)
    {
        _producer = broker.GetProducer("some-topic");
        _logger = logger;
    }
    
    public async Task Produce()
    {
        for (int i = 0; i < 100_000: i++)
        {
            await _producer.ProducerAsync(
              new MyMessage(i),
              null,
              () => _logger.LogInformation($"Produced {i}"),
              ex => _logger.LogError(ex, $"Failed to produce {i}");
        }
    }
}
```

> [!Note]
> This is different than calling `ProduceAsync` in a loop without awaiting, because that wouldn't ensure exact message ordering under all circumstances.

